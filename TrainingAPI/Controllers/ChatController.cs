using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;
using TrainingAPI.DTOs;
using TrainingAPI.Models;

namespace TrainingAPI.Controllers
{
    [ApiController]
    [Route("api/v1/chat")]
    public class ChatController : ControllerBase
    {
        private readonly CompanyContext _context;
        private readonly string _connectionString;

        public ChatController(CompanyContext context, IConfiguration configuration)
        {
            _context = context;
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? _context.Database.GetConnectionString()!;
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations([FromQuery] int viewerId)
        {
            var conversations = new List<ConversationDTO>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.sp_GetConversationList", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@ViewerId", viewerId);
            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                conversations.Add(new ConversationDTO
                {
                    ViewerId = reader.GetInt32("ViewerId"),
                    ThreadId = reader.GetInt64("ThreadId"),
                    IsGroup = reader.GetBoolean("IsGroup"),
                    ThreadName = reader.IsDBNull("ThreadName") ? "Cuộc hội thoại" : reader.GetString("ThreadName"),
                    ThreadAvatarUrl = reader.IsDBNull("ThreadAvatarUrl") ? null : reader.GetString("ThreadAvatarUrl"),
                    LastMessageId = reader.IsDBNull("LastMessageId") ? null : reader.GetInt64("LastMessageId"),
                    LastMessageContent = reader.IsDBNull("LastMessageContent") ? null : reader.GetString("LastMessageContent"),
                    LastSenderId = reader.IsDBNull("LastSenderId") ? null : reader.GetInt32("LastSenderId"),
                    LastMessageTimeUTC = reader.IsDBNull("LastMessageTimeUTC") ? null : reader.GetDateTime("LastMessageTimeUTC"),
                    LastDeliveredMessageId = reader.IsDBNull("LastDeliveredMessageId") ? null : reader.GetInt64("LastDeliveredMessageId"),
                    LastReadMessageId = reader.IsDBNull("LastReadMessageId") ? null : reader.GetInt64("LastReadMessageId"),
                    IsMuted = reader.GetBoolean("IsMuted")
                });
            }

            return Ok(conversations);
        }

        [HttpGet("messages")]
        public async Task<IActionResult> GetThreadMessages(
        [FromQuery] int viewerId,
        [FromQuery] long threadId,
        [FromQuery] long? cursorMessageId = null,
        [FromQuery] int limit = 20)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("dbo.sp_GetThreadMessages", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@ViewerId", viewerId);
                cmd.Parameters.AddWithValue("@ThreadId", threadId);
                cmd.Parameters.AddWithValue("@CursorMessageId", (object?)cursorMessageId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Limit", limit);

                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                var messagesMap = new Dictionary<long, MessageResponseDTO>();
                var messagesList = new List<MessageResponseDTO>();

                while (await reader.ReadAsync())
                {
                    var msg = new MessageResponseDTO
                    {
                        MessageId = reader.GetInt64("MessageId"),
                        SenderId = reader.GetInt32("SenderId"),
                        SenderName = reader.GetString("SenderName"),
                        SenderAvatarUrl = reader.IsDBNull("SenderAvatarUrl") ? null : reader.GetString("SenderAvatarUrl"),
                        MessageType = reader.GetString("MessageType"),
                        Content = reader.GetString("Content"),
                        IsRevoked = reader.GetBoolean("IsRevoked"),
                        IsEdited = reader.GetBoolean("IsEdited"),
                        SentAtUTC = reader.GetDateTime("SentAtUTC"),
                        ParentMessageId = reader.IsDBNull("ParentMessageId") ? null : reader.GetInt64("ParentMessageId"),
                        ParentSenderName = reader.IsDBNull("ParentSenderName") ? null : reader.GetString("ParentSenderName"),
                        ParentContent = reader.IsDBNull("ParentContent") ? null : reader.GetString("ParentContent"),
                        IsPinned = reader.GetBoolean("IsPinned"),
                        Attachments = new List<MessageAttachmentDTO>(),
                        Reactions = new List<MessageReactionDTO>()
                    };

                    messagesMap[msg.MessageId] = msg;
                    messagesList.Add(msg);
                }

                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        long msgId = reader.GetInt64("MessageId");
                        if (messagesMap.TryGetValue(msgId, out var msg))
                        {
                            msg.Attachments.Add(new MessageAttachmentDTO
                            {
                                Id = reader.GetInt64("Id"),
                                FileUrl = reader.GetString("FileUrl"),
                                FileType = reader.GetString("FileType"),
                                FileSize = reader.GetInt32("FileSize")
                            });
                        }
                    }
                }

                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        long msgId = reader.GetInt64("MessageId");
                        if (messagesMap.TryGetValue(msgId, out var msg))
                        {
                            msg.Reactions.Add(new MessageReactionDTO
                            {
                                ReactionType = reader.GetString("ReactionType"),
                                UserId = reader.GetInt32("UserId"),
                                ReactedByName = reader.GetString("ReactedByName")
                            });
                        }
                    }
                }

                return Ok(messagesList);
            }
            catch (SqlException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("users/search")]
        public async Task<IActionResult> SearchUsers([FromQuery] int currentUserId, [FromQuery] string keyword)
        {
            var result = new List<UserSearchDTO>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.sp_SearchUsersToChat", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@CurrentUserId", currentUserId);
            cmd.Parameters.AddWithValue("@Keyword", keyword ?? string.Empty);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new UserSearchDTO(
                    reader.GetInt32("Id"),
                    reader.GetString("Username"),
                    reader.GetString("DisplayName"),
                    reader.IsDBNull("AvatarUrl") ? null : reader.GetString("AvatarUrl")
                ));
            }

            return Ok(result);
        }

        [HttpGet("threads/{threadId}/participants")]
        public async Task<IActionResult> GetParticipants([FromRoute] long threadId, [FromQuery] int viewerId)
        {
            var participants = new List<ParticipantDTO>();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("dbo.sp_GetThreadParticipants", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@ViewerId", viewerId);
                cmd.Parameters.AddWithValue("@ThreadId", threadId);

                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    participants.Add(new ParticipantDTO
                    {
                        UserId = reader.GetInt32("UserId"),
                        Username = reader.GetString("Username"),
                        DisplayName = reader.GetString("DisplayName"),
                        AvatarUrl = reader.IsDBNull("AvatarUrl") ? null : reader.GetString("AvatarUrl"),
                        Role = reader.GetString("Role"),
                        JoinedAtUTC = reader.GetDateTime("JoinedAtUTC"),
                        LastDeliveredMessageId = reader.IsDBNull("LastDeliveredMessageId") ? null : reader.GetInt64("LastDeliveredMessageId"),
                        LastReadMessageId = reader.IsDBNull("LastReadMessageId") ? null : reader.GetInt64("LastReadMessageId")
                    });
                }

                return Ok(participants);
            }
            catch (SqlException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("threads")]
        public async Task<IActionResult> CreateThread([FromQuery] int creatorId, [FromBody] CreateThreadRequestDTO request)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("dbo.sp_CreateThread", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@CreatorId", creatorId);
                cmd.Parameters.AddWithValue("@IsGroup", request.IsGroup);
                cmd.Parameters.AddWithValue("@Title", (object?)request.Title ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ParticipantIdsJson", JsonSerializer.Serialize(request.ParticipantIds));

                await conn.OpenAsync();
                var newThreadId = await cmd.ExecuteScalarAsync();

                return Ok(new { threadId = Convert.ToInt64(newThreadId) });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("messages")]
        public async Task<IActionResult> SendMessage([FromQuery] int senderId, [FromBody] SendMessageRequestDTO request)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("dbo.sp_SendMessage", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@ThreadId", request.ThreadId);
                cmd.Parameters.AddWithValue("@SenderId", senderId);
                cmd.Parameters.AddWithValue("@MessageType", request.MessageType);
                cmd.Parameters.AddWithValue("@Content", request.Content);
                cmd.Parameters.AddWithValue("@ParentMessageId", (object?)request.ParentMessageId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ForwardedFromMessageId", (object?)request.ForwardedFromMessageId ?? DBNull.Value);

                await conn.OpenAsync();
                var newMsgId = await cmd.ExecuteScalarAsync();

                return Ok(new { messageId = Convert.ToInt64(newMsgId) });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("threads/{threadId}/read")]
        public async Task<IActionResult> MarkAsRead([FromRoute] long threadId, [FromQuery] int userId, [FromQuery] long messageId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.sp_MarkAsRead", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@ThreadId", threadId);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@MessageId", messageId);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return Ok(new { success = true });
        }

        [HttpPost("messages/reaction")]
        public async Task<IActionResult> ToggleReaction([FromQuery] int userId, [FromBody] ToggleReactionRequestDTO request)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("dbo.sp_ToggleReaction", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@MessageId", request.MessageId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@ReactionType", request.ReactionType);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                return Ok(new { success = true });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("messages/{messageId}/revoke")]
        public async Task<IActionResult> RevokeMessage([FromRoute] long messageId, [FromQuery] int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.sp_RevokeMessage", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@MessageId", messageId);
            cmd.Parameters.AddWithValue("@UserId", userId);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return Ok(new { success = true });
        }

        [HttpPut("messages/{messageId}")]
        public async Task<IActionResult> EditMessage([FromRoute] long messageId, [FromQuery] int userId, [FromBody] EditMessageRequestDTO request)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.sp_EditMessage", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@MessageId", messageId);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@NewContent", request.NewContent);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return Ok(new { success = true });
        }

        [HttpPost("threads/{threadId}/pin/{messageId}")]
        public async Task<IActionResult> TogglePin([FromRoute] long threadId, [FromRoute] long messageId, [FromQuery] int userId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("dbo.sp_TogglePinMessage", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@ThreadId", threadId);
                cmd.Parameters.AddWithValue("@MessageId", messageId);
                cmd.Parameters.AddWithValue("@UserId", userId);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                return Ok(new { success = true });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("messages/{messageId}/for-me")]
        public async Task<IActionResult> DeleteForMe([FromRoute] long messageId, [FromQuery] int userId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("dbo.sp_DeleteMessageForMe", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@MessageId", messageId);
                cmd.Parameters.AddWithValue("@UserId", userId);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                return Ok(new { success = true });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("threads/{threadId}/title")]
        public async Task<IActionResult> UpdateThreadTitle([FromRoute] long threadId, [FromQuery] int userId, [FromBody] UpdateTitleRequestDTO request)
        {
            var isMember = await _context.ThreadParticipants.AnyAsync(tp => tp.ThreadId == threadId && tp.UserId == userId);
            if (!isMember) return Forbid();

            var thread = await _context.ChatThreads.FirstOrDefaultAsync(t => t.Id == threadId);
            if (thread == null) return NotFound();

            thread.Title = request.NewTitle;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, newTitle = thread.Title });
        }

        [HttpPost("threads/{threadId}/toggle-hide")]
        public async Task<IActionResult> ToggleHideThread([FromRoute] long threadId, [FromQuery] int userId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("dbo.sp_ToggleHideThread", conn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@ThreadId", threadId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                return Ok(new { success = true });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("threads/manage-participant")]
        public async Task<IActionResult> ManageParticipant([FromBody] ManageParticipantRequestDTO request, [FromQuery] int actionUserId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("dbo.sp_ManageParticipant", conn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@ThreadId", request.ThreadId);
                cmd.Parameters.AddWithValue("@ActionUserId", actionUserId);
                cmd.Parameters.AddWithValue("@TargetUserId", request.TargetUserId);
                cmd.Parameters.AddWithValue("@ActionType", request.ActionType);
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                return Ok(new { success = true });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

         [HttpPut("threads/{threadId}/roles")]
        public async Task<IActionResult> UpdateParticipantRole([FromRoute] long threadId, [FromQuery] int actionUserId, [FromBody] UpdateRoleRequestDTO request)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("dbo.sp_UpdateParticipantRole", conn) { CommandType = CommandType.StoredProcedure };

                cmd.Parameters.AddWithValue("@ThreadId", threadId);
                cmd.Parameters.AddWithValue("@ActionUserId", actionUserId);
                cmd.Parameters.AddWithValue("@TargetUserId", request.TargetUserId);
                cmd.Parameters.AddWithValue("@NewRole", request.NewRole);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                return Ok(new { success = true });
            }
            catch (SqlException ex)
            {
                 return BadRequest(new { message = ex.Message });
            }
        }

         [HttpPost("threads/{threadId}/mute")]
        public async Task<IActionResult> MuteThread([FromRoute] long threadId, [FromQuery] int userId, [FromBody] MuteThreadRequestDTO request)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("dbo.sp_MuteThread", conn) { CommandType = CommandType.StoredProcedure };

                cmd.Parameters.AddWithValue("@ThreadId", threadId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@IsMuted", request.IsMuted);
                cmd.Parameters.AddWithValue("@MutedUntilUTC", (object?)request.MutedUntilUTC ?? DBNull.Value);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                return Ok(new { success = true });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("messages/{messageId}/attachments")]
        public async Task<IActionResult> SaveAttachment([FromRoute] long messageId, [FromBody] SaveAttachmentRequestDTO request)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var sql = @"INSERT INTO dbo.MessageAttachment (MessageId, FileUrl, FileType, FileSize) 
                            VALUES (@MessageId, @FileUrl, @FileType, @FileSize);";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@MessageId", messageId);
                cmd.Parameters.AddWithValue("@FileUrl", request.FileUrl);
                cmd.Parameters.AddWithValue("@FileType", request.FileType);
                cmd.Parameters.AddWithValue("@FileSize", request.FileSize);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                return Ok(new { success = true });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}