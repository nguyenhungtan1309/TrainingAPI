using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;
using TrainingAPI.DTOs;
using TrainingAPI.Models;
using TrainingAPI.Services.Common;

namespace TrainingAPI.Services.Messaging
{
    public class ChatThreadService : IChatThreadService
    {
        private readonly ISqlDataAccess _db;
        private readonly CompanyContext _context;
        private readonly IChatMessageService _messageService;
        private readonly ILogger<ChatThreadService> _logger;

        public ChatThreadService(
            ISqlDataAccess db,
            CompanyContext context,
            IChatMessageService messageService,
            ILogger<ChatThreadService> logger)
        {
            _db = db;
            _context = context;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<(bool Success, string? Error, long ThreadId)> CreateThreadAsync(
            int creatorId,
            CreateThreadRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            var participantsJson = JsonSerializer.Serialize(request.ParticipantIds);

            try
            {
                await using var result = await _db.ExecuteReaderAsync(
                    "dbo.sp_CreateThread",
                    parameters =>
                    {
                        parameters.Add(new SqlParameter("@CreatorId", SqlDbType.Int) { Value = creatorId });
                        parameters.Add(new SqlParameter("@IsGroup", SqlDbType.Bit) { Value = request.IsGroup });
                        parameters.Add(new SqlParameter("@Title", SqlDbType.NVarChar, 100) { Value = (object?)request.Title ?? DBNull.Value });
                        parameters.Add(new SqlParameter("@ParticipantIdsJson", SqlDbType.NVarChar, -1) { Value = participantsJson });
                    },
                    cancellationToken);

                long newThreadId = 0;
                if (await result.Reader.ReadAsync(cancellationToken))
                {
                    newThreadId = Convert.ToInt64(result.Reader["NewThreadId"]);
                }

                _logger.LogInformation("User {CreatorId} đã tạo Thread mới {ThreadId} (IsGroup={IsGroup}, Title={Title})",
                    creatorId, newThreadId, request.IsGroup, request.Title);

                return (true, null, newThreadId);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi khi User {CreatorId} tạo nhóm thoại", creatorId);
                return (false, ex.Message, 0);
            }
        }

        public async Task<(bool Success, string? Error, ThreadDetailDTO? Data)> GetThreadDetailsAsync(
            int viewerId,
            long threadId,
            int limit = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var participantsTask = GetThreadParticipantsAsync(viewerId, threadId, cancellationToken);
                var messagesTask = _messageService.GetThreadMessagesAsync(viewerId, threadId, null, limit, cancellationToken);

                await Task.WhenAll(participantsTask, messagesTask);

                var (partSuccess, partError, participants) = await participantsTask;
                var (msgSuccess, msgError, messages) = await messagesTask;

                if (!partSuccess) return (false, partError, null);
                if (!msgSuccess) return (false, msgError, null);

                var result = new ThreadDetailDTO
                {
                    ThreadId = threadId,
                    Participants = participants,
                    Messages = messages
                };

                return (true, null, result);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Người dùng {ViewerId} đã hủy yêu cầu tải chi tiết Thread {ThreadId}", viewerId, threadId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải chi tiết phòng chat Thread {ThreadId} cho Viewer {ViewerId}", threadId, viewerId);
                return (false, "Lỗi không xác định khi tải dữ liệu phòng chat.", null);
            }
        }

        public async Task<(bool Success, string? Error, List<ConversationDTO> Conversations)> GetConversationsAsync(
            int viewerId,
            int top = 30,
            DateTime? beforeTimeUTC = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await using var result = await _db.ExecuteReaderAsync(
                    "dbo.sp_GetConversationListV2",
                    parameters =>
                    {
                        parameters.Add(new SqlParameter("@ViewerId", SqlDbType.Int) { Value = viewerId });
                        parameters.Add(new SqlParameter("@Top", SqlDbType.Int) { Value = top });
                        parameters.Add(new SqlParameter("@BeforeTimeUTC", SqlDbType.DateTime2) { Value = (object?)beforeTimeUTC ?? DBNull.Value });
                    },
                    cancellationToken);

                var list = new List<ConversationDTO>();
                while (await result.Reader.ReadAsync(cancellationToken))
                {
                    list.Add(new ConversationDTO
                    {
                        ViewerId = Convert.ToInt32(result.Reader["ViewerId"]),
                        ThreadId = Convert.ToInt64(result.Reader["ThreadId"]),
                        IsGroup = Convert.ToBoolean(result.Reader["IsGroup"]),
                        ThreadName = result.Reader["ThreadName"].ToString(),
                        ThreadAvatarUrl = result.Reader["ThreadAvatarUrl"].ToString(),
                        LastMessageId = result.Reader["LastMessageId"] as long?,
                        LastMessageType = result.Reader["LastMessageType"].ToString(),
                        LastMessageContent = result.Reader["LastMessageContent"].ToString(),
                        LastSenderId = result.Reader["LastSenderId"] as int?,
                        LastMessageTimeUTC = Convert.ToDateTime(result.Reader["LastMessageTimeUTC"]),
                        LastDeliveredMessageId = result.Reader["LastDeliveredMessageId"] as long?,
                        LastReadMessageId = result.Reader["LastReadMessageId"] as long?,
                        IsMuted = Convert.ToBoolean(result.Reader["IsMuted"]),
                        UnreadCount = Convert.ToInt32(result.Reader["UnreadCount"])
                    });
                }

                return (true, null, list);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách hội thoại cho User {ViewerId}", viewerId);
                return (false, ex.Message, new List<ConversationDTO>());
            }
        }

        public async Task<(bool Success, string? Error, List<ParticipantDTO> Participants)> GetThreadParticipantsAsync(
            int viewerId,
            long threadId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await using var result = await _db.ExecuteReaderAsync(
                    "dbo.sp_GetThreadParticipants",
                    parameters =>
                    {
                        parameters.Add(new SqlParameter("@ViewerId", SqlDbType.Int) { Value = viewerId });
                        parameters.Add(new SqlParameter("@ThreadId", SqlDbType.BigInt) { Value = threadId });
                    },
                    cancellationToken);

                var list = new List<ParticipantDTO>();
                while (await result.Reader.ReadAsync(cancellationToken))
                {
                    list.Add(new ParticipantDTO
                    {
                        UserId = Convert.ToInt32(result.Reader["UserId"]),
                        Username = result.Reader["Username"].ToString(),
                        DisplayName = result.Reader["DisplayName"].ToString(),
                        AvatarUrl = result.Reader["AvatarUrl"].ToString(),
                        Role = result.Reader["Role"].ToString(),
                        JoinedAtUTC = Convert.ToDateTime(result.Reader["JoinedAtUTC"]),
                        LastDeliveredMessageId = result.Reader["LastDeliveredMessageId"] as long?,
                        LastReadMessageId = result.Reader["LastReadMessageId"] as long?
                    });
                }

                return (true, null, list);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách thành viên Thread {ThreadId} cho User {ViewerId}", threadId, viewerId);
                return (false, ex.Message, new List<ParticipantDTO>());
            }
        }

        public async Task<(bool Success, string? Error)> ManageParticipantAsync(
            int actionUserId,
            ManageParticipantRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _db.ExecuteNonQueryAsync(
                    "dbo.sp_ManageParticipant",
                    parameters =>
                    {
                        parameters.Add(new SqlParameter("@ThreadId", SqlDbType.BigInt) { Value = request.ThreadId });
                        parameters.Add(new SqlParameter("@ActionUserId", SqlDbType.Int) { Value = actionUserId });
                        parameters.Add(new SqlParameter("@TargetUserId", SqlDbType.Int) { Value = request.TargetUserId });
                        parameters.Add(new SqlParameter("@ActionType", SqlDbType.VarChar, 10) { Value = request.ActionType });
                    },
                    cancellationToken);

                _logger.LogInformation("User {ActionUserId} đã thực hiện thao tác '{ActionType}' đối với TargetUser {TargetUserId} trong Thread {ThreadId}",
                    actionUserId, request.ActionType, request.TargetUserId, request.ThreadId);

                return (true, null);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi khi User {ActionUserId} thao tác thành viên Thread {ThreadId}", actionUserId, request.ThreadId);
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> UpdateParticipantRoleAsync(
            int actionUserId,
            long threadId,
            UpdateRoleRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _db.ExecuteNonQueryAsync(
                    "dbo.sp_UpdateParticipantRole",
                    parameters =>
                    {
                        parameters.Add(new SqlParameter("@ThreadId", SqlDbType.BigInt) { Value = threadId });
                        parameters.Add(new SqlParameter("@ActionUserId", SqlDbType.Int) { Value = actionUserId });
                        parameters.Add(new SqlParameter("@TargetUserId", SqlDbType.Int) { Value = request.TargetUserId });
                        parameters.Add(new SqlParameter("@NewRole", SqlDbType.VarChar, 20) { Value = request.NewRole });
                    },
                    cancellationToken);

                _logger.LogInformation("User {ActionUserId} đã đổi vai trò của TargetUser {TargetUserId} thành '{NewRole}' trong Thread {ThreadId}",
                    actionUserId, request.TargetUserId, request.NewRole, threadId);

                return (true, null);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi khi phân quyền cho User {TargetUserId} bởi User {ActionUserId} trong Thread {ThreadId}", request.TargetUserId, actionUserId, threadId);
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> UpdateThreadTitleAsync(
            int actionUserId,
            long threadId,
            UpdateTitleRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            var participant = await _context.ThreadParticipants
                .FirstOrDefaultAsync(p => p.ThreadId == threadId && p.UserId == actionUserId, cancellationToken);

            if (participant == null)
            {
                return (false, "Người dùng không thuộc cuộc trò chuyện này.");
            }

            var thread = await _context.ChatThreads.FirstOrDefaultAsync(t => t.Id == threadId, cancellationToken);
            if (thread == null)
            {
                return (false, "Không tìm thấy cuộc hội thoại.");
            }

            if (!thread.IsGroup)
            {
                return (false, "Không thể đổi tên nhóm 1-1.");
            }

            var oldTitle = thread.Title;
            thread.Title = request.NewTitle.Trim();

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("User {ActionUserId} đã đổi tên Thread {ThreadId} từ '{OldTitle}' thành '{NewTitle}'",
                    actionUserId, threadId, oldTitle, thread.Title);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đổi tên Thread {ThreadId}", threadId);
                return (false, "Có lỗi xảy ra khi cập nhật tên nhóm.");
            }
        }

        public async Task<(bool Success, string? Error)> ToggleHideThreadAsync(
            int userId,
            long threadId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _db.ExecuteNonQueryAsync(
                    "dbo.sp_ToggleHideThread",
                    parameters =>
                    {
                        parameters.Add(new SqlParameter("@ThreadId", SqlDbType.BigInt) { Value = threadId });
                        parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
                    },
                    cancellationToken);

                _logger.LogInformation("User {UserId} đã chuyển trạng thái Ẩn/Hiện Thread {ThreadId}", userId, threadId);
                return (true, null);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi khi ẩn/hiện nhóm {ThreadId} bởi User {UserId}", threadId, userId);
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> MuteThreadAsync(
            int userId,
            long threadId,
            MuteThreadRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _db.ExecuteNonQueryAsync(
                    "dbo.sp_MuteThread",
                    parameters =>
                    {
                        parameters.Add(new SqlParameter("@ThreadId", SqlDbType.BigInt) { Value = threadId });
                        parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
                        parameters.Add(new SqlParameter("@IsMuted", SqlDbType.Bit) { Value = request.IsMuted });
                        parameters.Add(new SqlParameter("@MutedUntilUTC", SqlDbType.DateTime2) { Value = (object?)request.MutedUntilUTC ?? DBNull.Value });
                    },
                    cancellationToken);

                _logger.LogInformation("User {UserId} đã đặt trạng thái thông báo Thread {ThreadId} thành IsMuted={IsMuted}", userId, threadId, request.IsMuted);
                return (true, null);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi khi tắt/bật thông báo Thread {ThreadId} bởi User {UserId}", threadId, userId);
                return (false, ex.Message);
            }
        }
    }
}