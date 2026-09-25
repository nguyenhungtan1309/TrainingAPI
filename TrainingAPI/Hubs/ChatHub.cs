using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrainingAPI.DTOs;
using TrainingAPI.Models;
using TrainingAPI.Services.Common;
using TrainingAPI.Services.Messaging;

namespace TrainingAPI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly CompanyContext _context;
        private readonly ISqlDataAccess _sql;

        // [Hạ tầng chung] Không còn tự xây _connectionString trong Hub nữa.
        public ChatHub(CompanyContext context, ISqlDataAccess sql)
        {
            _context = context;
            _sql = sql;
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

            var newMessageId = await _sql.ExecuteScalarAsync<long>("dbo.sp_SendMessage", p =>
            {
                p.AddWithValue("@ThreadId", request.ThreadId);
                p.AddWithValue("@SenderId", senderId);
                p.AddWithValue("@MessageType", request.MessageType);
                p.AddWithValue("@Content", request.Content);
                p.AddWithValue("@ParentMessageId", (object?)request.ParentMessageId ?? DBNull.Value);
                p.AddWithValue("@ForwardedFromMessageId", (object?)request.ForwardedFromMessageId ?? DBNull.Value);
            });

            var formatter = MessageContentFormatterFactory.Create(request.MessageType);
            var previewContent = formatter.BuildPreview(request.Content, isRevoked: false);

            await Clients.Group($"thread_{request.ThreadId}").SendAsync("ReceiveMessage", new
            {
                MessageId = newMessageId,
                ThreadId = request.ThreadId,
                SenderId = senderId,
                SenderName = CurrentUserName,
                MessageType = request.MessageType,
                Content = request.Content,
                PreviewContent = previewContent,
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

            await _sql.ExecuteNonQueryAsync("dbo.sp_ToggleReaction", p =>
            {
                p.AddWithValue("@MessageId", messageId);
                p.AddWithValue("@UserId", userId);
                p.AddWithValue("@ReactionType", reactionType);
            });

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

            await _sql.ExecuteNonQueryAsync("dbo.sp_RevokeMessage", p =>
            {
                p.AddWithValue("@MessageId", messageId);
                p.AddWithValue("@UserId", userId);
            });

            // [2.1][Abstract class] Nhánh isRevoked = true của BuildPreview():
            // dù MessageType gốc là gì (Text/Image/File), formatter tương ứng
            // đều trả về đúng một câu "Tin nhắn đã bị thu hồi" nhờ phần khung
            // dùng chung trong lớp cha MessageContentFormatter - lớp con
            // không cần (và không thể) ghi đè hành vi này.
            var formatter = MessageContentFormatterFactory.Create("Text");
            var previewContent = formatter.BuildPreview(rawContent: string.Empty, isRevoked: true);

            await Clients.Group($"thread_{threadId}").SendAsync("MessageRevoked", new
            {
                ThreadId = threadId,
                MessageId = messageId,
                PreviewContent = previewContent
            });
        }

        public async Task MarkAsRead(long threadId, long messageId)
        {
            var userId = CurrentUserId;
            if (userId == 0) return;

            await _sql.ExecuteNonQueryAsync("dbo.sp_MarkAsRead", p =>
            {
                p.AddWithValue("@ThreadId", threadId);
                p.AddWithValue("@UserId", userId);
                p.AddWithValue("@MessageId", messageId);
            });

            await Clients.OthersInGroup($"thread_{threadId}").SendAsync("MessageRead", new
            {
                ThreadId = threadId,
                UserId = userId,
                MessageId = messageId
            });
        }
    }
}
