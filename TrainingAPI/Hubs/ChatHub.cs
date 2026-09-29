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
        private readonly IChatMessageService _messageService;
        private readonly CompanyContext _context;

        public ChatHub(IChatMessageService messageService, CompanyContext context)
        {
            _messageService = messageService;
            _context = context;
        }

        private int CurrentUserId => Context.User!.GetUserId();

        private string CurrentUserName =>
            Context.User!.FindFirst("DisplayName")?.Value
            ?? Context.User!.FindFirst(ClaimTypes.Name)?.Value
            ?? "Anonymous";

        private static string GroupName(long threadId) => $"thread_{threadId}";

        public override async Task OnConnectedAsync()
        {
            var userId = CurrentUserId;
            var threadIds = await _context.ThreadParticipants
                .Where(tp => tp.UserId == userId)
                .Select(tp => tp.ThreadId)
                .ToListAsync();

            foreach (var threadId in threadIds)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(threadId));
            }

            await base.OnConnectedAsync();
        }

        public async Task JoinThread(long threadId)
        {
            var isMember = await _context.ThreadParticipants
                .AnyAsync(tp => tp.ThreadId == threadId && tp.UserId == CurrentUserId);
            if (!isMember)
            {
                throw new HubException("Người dùng không thuộc cuộc trò chuyện này.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(threadId));
        }

        public async Task LeaveThread(long threadId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(threadId));
        }

        public async Task SendMessage(SendMessageRequestDTO request)
        {
            var senderId = CurrentUserId;

            var (success, error, messageId) = await _messageService.SendMessageAsync(senderId, request);
            if (!success)
            {
                throw new HubException(error ?? "Không thể gửi tin nhắn.");
            }

            var formatter = MessageContentFormatterFactory.Create(request.MessageType);
            var previewContent = formatter.BuildPreview(request.Content, isRevoked: false);

            await Clients.Group(GroupName(request.ThreadId)).SendAsync("ReceiveMessage", new
            {
                MessageId = messageId,
                ThreadId = request.ThreadId,
                SenderId = senderId,
                SenderName = CurrentUserName,
                MessageType = request.MessageType,
                Content = request.Content,
                PreviewContent = previewContent,
                ParentMessageId = request.ParentMessageId,
                Attachments = request.Attachments ?? new List<SaveAttachmentRequestDTO>(),
                ClientMessageId = request.ClientMessageId,
                SentAtUTC = DateTime.UtcNow
            });
        }

        public async Task SendTyping(long threadId, bool isTyping)
        {
            await Clients.OthersInGroup(GroupName(threadId)).SendAsync("UserTyping", new
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
            var request = new ToggleReactionRequestDTO { MessageId = messageId, ReactionType = reactionType };

            var (success, error) = await _messageService.ToggleReactionAsync(userId, request);
            if (!success) throw new HubException(error ?? "Không thể thả biểu cảm.");

            await Clients.Group(GroupName(threadId)).SendAsync("ReactionUpdated", new
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
            var (success, error) = await _messageService.RevokeMessageAsync(CurrentUserId, messageId);
            if (!success) throw new HubException(error ?? "Không thể thu hồi tin nhắn.");

            var formatter = MessageContentFormatterFactory.Create("Text");
            var previewContent = formatter.BuildPreview(rawContent: string.Empty, isRevoked: true);

            await Clients.Group(GroupName(threadId)).SendAsync("MessageRevoked", new
            {
                ThreadId = threadId,
                MessageId = messageId,
                PreviewContent = previewContent
            });
        }

        public async Task MarkAsRead(long threadId, long messageId)
        {
            var userId = CurrentUserId;

            var (success, error) = await _messageService.MarkAsReadAsync(userId, threadId, messageId);
            if (!success) throw new HubException(error ?? "Không thể đánh dấu đã đọc.");

            await Clients.OthersInGroup(GroupName(threadId)).SendAsync("MessageRead", new
            {
                ThreadId = threadId,
                UserId = userId,
                MessageId = messageId
            });
        }
    }
}
