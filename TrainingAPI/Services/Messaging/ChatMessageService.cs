using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using TrainingAPI.DTOs;
using TrainingAPI.Services.Common;
using TrainingAPI.Services.Messaging.Validation;

namespace TrainingAPI.Services.Messaging
{
    public class ChatMessageService : IChatMessageService
    {
        private readonly ISqlDataAccess _db;
        private readonly IConversationCacheService _conversationCache;
        private readonly ILogger<ChatMessageService> _logger;

        public ChatMessageService(ISqlDataAccess db, IConversationCacheService conversationCache, ILogger<ChatMessageService> logger)
        {
            _db = db;
            _conversationCache = conversationCache;
            _logger = logger;
        }

        public async Task<(bool Success, string? Error, long MessageId)> SendMessageAsync(
            int senderId,
            SendMessageRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            var validator = MessageValidatorFactory.GetValidator(request.MessageType);
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation tin nhắn thất bại từ User {SenderId}: {Error}", senderId, validationResult.ErrorMessage);
                return (false, validationResult.ErrorMessage, 0);
            }

            string? attachmentsJson = null;
            if (request.Attachments != null && request.Attachments.Count > 0)
            {
                attachmentsJson = JsonSerializer.Serialize(request.Attachments);
            }

            try
            {
                await using var result = await _db.ExecuteReaderAsync(
                    "dbo.sp_SendMessage",
                    parameters =>
                    {
                        parameters.Add(new SqlParameter("@ThreadId", SqlDbType.BigInt) { Value = request.ThreadId });
                        parameters.Add(new SqlParameter("@SenderId", SqlDbType.Int) { Value = senderId });
                        parameters.Add(new SqlParameter("@MessageType", SqlDbType.VarChar, 20) { Value = request.MessageType });
                        parameters.Add(new SqlParameter("@Content", SqlDbType.NVarChar, 4000) { Value = (object?)request.Content ?? string.Empty });
                        parameters.Add(new SqlParameter("@ParentMessageId", SqlDbType.BigInt) { Value = (object?)request.ParentMessageId ?? DBNull.Value });
                        parameters.Add(new SqlParameter("@ForwardedFromMessageId", SqlDbType.BigInt) { Value = (object?)request.ForwardedFromMessageId ?? DBNull.Value });
                        parameters.Add(new SqlParameter("@ClientMessageId", SqlDbType.UniqueIdentifier) { Value = (object?)request.ClientMessageId ?? DBNull.Value });
                        parameters.Add(new SqlParameter("@AttachmentsJson", SqlDbType.NVarChar, -1) { Value = (object?)attachmentsJson ?? DBNull.Value });
                    },
                    cancellationToken);

                long newMsgId = 0;
                if (await result.Reader.ReadAsync(cancellationToken))
                {
                    newMsgId = Convert.ToInt64(result.Reader["NewMessageId"]);
                }

                _logger.LogInformation("User {SenderId} đã gửi tin nhắn {MessageId} loại {MessageType} vào Thread {ThreadId}",
                    senderId, newMsgId, request.MessageType, request.ThreadId);

                await InvalidateConversationCacheForThreadAsync(senderId, request.ThreadId, cancellationToken);

                return (true, null, newMsgId);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi SQL khi gửi tin nhắn từ User {SenderId} vào Thread {ThreadId}", senderId, request.ThreadId);
                return (false, ex.Message, 0);
            }
        }

        private async Task InvalidateConversationCacheForThreadAsync(int senderId, long threadId, CancellationToken cancellationToken)
        {
            try
            {
                var participantIds = new List<int>();

                await using (var result = await _db.ExecuteReaderAsync(
                    "dbo.sp_GetThreadParticipants",
                    parameters =>
                    {
                        parameters.Add(new SqlParameter("@ViewerId", SqlDbType.Int) { Value = senderId });
                        parameters.Add(new SqlParameter("@ThreadId", SqlDbType.BigInt) { Value = threadId });
                    },
                    cancellationToken))
                {
                    while (await result.Reader.ReadAsync(cancellationToken))
                    {
                        participantIds.Add(Convert.ToInt32(result.Reader["UserId"]));
                    }
                }

                foreach (var userId in participantIds)
                {
                    await _conversationCache.InvalidateAsync(userId, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể xóa cache danh sách hội thoại cho Thread {ThreadId} sau khi User {SenderId} gửi tin (không ảnh hưởng việc gửi tin đã thành công).", threadId, senderId);
            }
        }

        public async Task<(bool Success, string? Error, List<MessageResponseDTO> Messages)> GetThreadMessagesAsync(
            int viewerId,
            long threadId,
            long? cursorMessageId = null,
            int limit = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await using var result = await _db.ExecuteReaderAsync(
                    "dbo.sp_GetThreadMessages",
                    parameters =>
                    {
                        parameters.Add(new SqlParameter("@ViewerId", SqlDbType.Int) { Value = viewerId });
                        parameters.Add(new SqlParameter("@ThreadId", SqlDbType.BigInt) { Value = threadId });
                        parameters.Add(new SqlParameter("@CursorMessageId", SqlDbType.BigInt) { Value = (object?)cursorMessageId ?? DBNull.Value });
                        parameters.Add(new SqlParameter("@Limit", SqlDbType.Int) { Value = limit });
                    },
                    cancellationToken);

                var reader = result.Reader;
                var msgList = new List<MessageResponseDTO>();
                var msgDict = new Dictionary<long, MessageResponseDTO>();

                while (await reader.ReadAsync(cancellationToken))
                {
                    var msg = new MessageResponseDTO
                    {
                        MessageId = Convert.ToInt64(reader["MessageId"]),
                        SenderId = Convert.ToInt32(reader["SenderId"]),
                        SenderName = reader["SenderName"].ToString() ?? string.Empty,
                        SenderAvatarUrl = reader["SenderAvatarUrl"] as string,
                        MessageType = reader["MessageType"].ToString() ?? "Text",
                        Content = reader["Content"].ToString() ?? string.Empty,
                        IsRevoked = Convert.ToBoolean(reader["IsRevoked"]),
                        IsEdited = Convert.ToBoolean(reader["IsEdited"]),
                        SentAtUTC = Convert.ToDateTime(reader["SentAtUTC"]),
                        ParentMessageId = reader["ParentMessageId"] as long?,
                        ParentSenderName = reader["ParentSenderName"] as string,
                        ParentContent = reader["ParentContent"] as string,
                        IsPinned = Convert.ToBoolean(reader["IsPinned"])
                    };
                    msgList.Add(msg);
                    msgDict[msg.MessageId] = msg;
                }

                if (await reader.NextResultAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var msgId = Convert.ToInt64(reader["MessageId"]);
                        if (msgDict.TryGetValue(msgId, out var targetMsg))
                        {
                            targetMsg.Attachments.Add(new MessageAttachmentDTO
                            {
                                Id = Convert.ToInt64(reader["Id"]),
                                FileUrl = reader["FileUrl"].ToString() ?? string.Empty,
                                FileType = reader["FileType"].ToString() ?? string.Empty,
                                FileSize = Convert.ToInt32(reader["FileSize"])
                            });
                        }
                    }
                }

                if (await reader.NextResultAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var msgId = Convert.ToInt64(reader["MessageId"]);
                        if (msgDict.TryGetValue(msgId, out var targetMsg))
                        {
                            targetMsg.Reactions.Add(new MessageReactionDTO
                            {
                                ReactionType = reader["ReactionType"].ToString() ?? string.Empty,
                                UserId = Convert.ToInt32(reader["UserId"]),
                                ReactedByName = reader["ReactedByName"].ToString() ?? string.Empty
                            });
                        }
                    }
                }

                return (true, null, msgList);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi SQL khi lấy tin nhắn Thread {ThreadId} cho User {ViewerId}", threadId, viewerId);
                return (false, ex.Message, new List<MessageResponseDTO>());
            }
        }

        public async Task<(bool Success, string? Error)> RevokeMessageAsync(
            int userId,
            long messageId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var affected = await _db.ExecuteNonQueryAsync(
                    "dbo.sp_RevokeMessage",
                    parameters =>
                    {
                        parameters.Add(new SqlParameter("@MessageId", SqlDbType.BigInt) { Value = messageId });
                        parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
                    },
                    cancellationToken);

                if (affected > 0)
                {
                    _logger.LogInformation("User {UserId} đã thu hồi tin nhắn {MessageId}", userId, messageId);
                }

                return (affected > 0, affected > 0 ? null : "Không tìm thấy tin nhắn hoặc không có quyền thu hồi.");
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi khi thu hồi tin nhắn {MessageId} bởi User {UserId}", messageId, userId);
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> EditMessageAsync(
            int userId,
            EditMessageRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.NewContent))
                return (false, "Nội dung chỉnh sửa không được để trống.");

            try
            {
                var affected = await _db.ExecuteScalarAsync<int>(
                    "dbo.sp_EditMessage",
                    parameters =>
                    {
                        parameters.Add(new SqlParameter("@MessageId", SqlDbType.BigInt) { Value = request.MessageId });
                        parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
                        parameters.Add(new SqlParameter("@NewContent", SqlDbType.NVarChar, 4000) { Value = request.NewContent.Trim() });
                    },
                    cancellationToken);

                if (affected > 0)
                {
                    _logger.LogInformation("User {UserId} đã sửa nội dung tin nhắn {MessageId}", userId, request.MessageId);
                    return (true, null);
                }

                return (false, "Không tìm thấy tin nhắn, tin đã bị thu hồi hoặc không có quyền chỉnh sửa.");
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi khi sửa tin nhắn {MessageId} bởi User {UserId}", request.MessageId, userId);
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> TogglePinMessageAsync(
            int userId,
            long threadId,
            long messageId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _db.ExecuteNonQueryAsync(
                    "dbo.sp_TogglePinMessage",
                    parameters =>
                    {
                        parameters.Add(new SqlParameter("@ThreadId", SqlDbType.BigInt) { Value = threadId });
                        parameters.Add(new SqlParameter("@MessageId", SqlDbType.BigInt) { Value = messageId });
                        parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
                    },
                    cancellationToken);

                _logger.LogInformation("User {UserId} đã thao tác Ghim/Bỏ ghim tin nhắn {MessageId} trong Thread {ThreadId}", userId, messageId, threadId);
                return (true, null);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi khi ghim/bỏ ghim tin nhắn {MessageId} trong Thread {ThreadId}", messageId, threadId);
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> DeleteMessageForMeAsync(
            int userId,
            long messageId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _db.ExecuteNonQueryAsync(
                    "dbo.sp_DeleteMessageForMe",
                    parameters =>
                    {
                        parameters.Add(new SqlParameter("@MessageId", SqlDbType.BigInt) { Value = messageId });
                        parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
                    },
                    cancellationToken);

                _logger.LogInformation("User {UserId} đã xóa tin nhắn {MessageId} ở phía mình", userId, messageId);
                return (true, null);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa tin nhắn {MessageId} phía tôi cho User {UserId}", messageId, userId);
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> MarkAsReadAsync(
            int userId,
            long threadId,
            long messageId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _db.ExecuteNonQueryAsync(
                    "dbo.sp_MarkAsRead",
                    parameters =>
                    {
                        parameters.Add(new SqlParameter("@ThreadId", SqlDbType.BigInt) { Value = threadId });
                        parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
                        parameters.Add(new SqlParameter("@MessageId", SqlDbType.BigInt) { Value = messageId });
                    },
                    cancellationToken);

                await _conversationCache.InvalidateAsync(userId, cancellationToken);
                return (true, null);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi SQL khi đánh dấu đã đọc: User {UserId}, Thread {ThreadId}, Message {MessageId}", userId, threadId, messageId);
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> ToggleReactionAsync(
            int userId,
            ToggleReactionRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _db.ExecuteNonQueryAsync(
                    "dbo.sp_ToggleReaction",
                    parameters =>
                    {
                        parameters.Add(new SqlParameter("@MessageId", SqlDbType.BigInt) { Value = request.MessageId });
                        parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
                        parameters.Add(new SqlParameter("@ReactionType", SqlDbType.VarChar, 20) { Value = request.ReactionType });
                    },
                    cancellationToken);

                return (true, null);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi khi thả biểu cảm tin nhắn {MessageId} bởi User {UserId}", request.MessageId, userId);
                return (false, ex.Message);
            }
        }
    }
}