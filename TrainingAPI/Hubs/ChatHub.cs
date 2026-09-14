using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;
using TrainingAPI.DTOs;
using TrainingAPI.Models;

namespace TrainingAPI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly CompanyContext _context;
        private readonly string _connectionString;

        public ChatHub(CompanyContext context, IConfiguration configuration)
        {
            _context = context;
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? _context.Database.GetConnectionString()!;
        }

        private int CurrentUserId =>
            int.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? Context.User!.FindFirst("sub")?.Value
                   ?? "0");

        private string CurrentUserName =>
            Context.User!.FindFirst("DisplayName")?.Value
            ?? Context.User!.FindFirst(ClaimTypes.Name)?.Value
            ?? "Anonymous";

        public override async Task OnConnectedAsync()
        {
            var userId = CurrentUserId;
            if (userId > 0)
            {
                var userThreadIds = await _context.ThreadParticipants
                    .Where(tp => tp.UserId == userId)
                    .Select(tp => tp.ThreadId)
                    .ToListAsync();

                foreach (var threadId in userThreadIds)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"thread_{threadId}");
                }
            }

            await base.OnConnectedAsync();
        }

        public async Task SendMessage(SendMessageRequestDTO request)
        {
            var senderId = CurrentUserId;
            if (senderId == 0) return;

            long newMessageId = 0;

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.sp_SendMessage", conn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("@ThreadId", request.ThreadId);
                cmd.Parameters.AddWithValue("@SenderId", senderId);
                cmd.Parameters.AddWithValue("@MessageType", request.MessageType);
                cmd.Parameters.AddWithValue("@Content", request.Content);
                cmd.Parameters.AddWithValue("@ParentMessageId", (object?)request.ParentMessageId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ForwardedFromMessageId", (object?)request.ForwardedFromMessageId ?? DBNull.Value);

                await conn.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();
                newMessageId = Convert.ToInt64(result);
            }

            await Clients.Group($"thread_{request.ThreadId}").SendAsync("ReceiveMessage", new
            {
                MessageId = newMessageId,
                ThreadId = request.ThreadId,
                SenderId = senderId,
                SenderName = CurrentUserName,
                MessageType = request.MessageType,
                Content = request.Content,
                ParentMessageId = request.ParentMessageId,
                SentAtUTC = DateTime.UtcNow
            });
        }

        public async Task JoinThread(long threadId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"thread_{threadId}");
        }

        public async Task LeaveThread(long threadId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"thread_{threadId}");
        }

        public async Task SendTyping(long threadId, bool isTyping)
        {
            await Clients.OthersInGroup($"thread_{threadId}").SendAsync("UserTyping", new
            {
                ThreadId = threadId,
                UserId = CurrentUserId,
                DisplayName = CurrentUserName,
                IsTyping = isTyping
            });
        }

        public async Task SendReaction(long threadId, long messageId, string reactionType)
        {
            var userId = CurrentUserId;
            if (userId == 0) return;

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.sp_ToggleReaction", conn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("@MessageId", messageId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@ReactionType", reactionType);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }

            await Clients.Group($"thread_{threadId}").SendAsync("ReactionUpdated", new
            {
                ThreadId = threadId,
                MessageId = messageId,
                UserId = userId,
                DisplayName = CurrentUserName,
                ReactionType = reactionType
            });
        }
        public async Task RevokeMessage(long threadId, long messageId)
        {
            var userId = CurrentUserId;
            if (userId == 0) return;

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.sp_RevokeMessage", conn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("@MessageId", messageId);
                cmd.Parameters.AddWithValue("@UserId", userId);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }

            await Clients.Group($"thread_{threadId}").SendAsync("MessageRevoked", new
            {
                ThreadId = threadId,
                MessageId = messageId
            });
        }

        public async Task MarkAsRead(long threadId, long messageId)
        {
            var userId = CurrentUserId;
            if (userId == 0) return;

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("dbo.sp_MarkAsRead", conn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("@ThreadId", threadId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@MessageId", messageId);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }

            await Clients.OthersInGroup($"thread_{threadId}").SendAsync("MessageRead", new
            {
                ThreadId = threadId,
                UserId = userId,
                MessageId = messageId
            });
        }
    }
}