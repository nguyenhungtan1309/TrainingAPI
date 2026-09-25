using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TrainingAPI.DTOs;
using TrainingAPI.Services.Common;
using TrainingAPI.Services.Messaging;

namespace TrainingAPI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatMessageService _messageService;

        public ChatHub(IChatMessageService messageService)
        {
            _messageService = messageService;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public async Task JoinThread(long threadId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, threadId.ToString());
        }

        public async Task LeaveThread(long threadId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, threadId.ToString());
        }

        public async Task SendMessage(long threadId, string content, string messageType = "Text", long? parentMessageId = null)
        {
            var senderId = Context.User.GetUserId();
            var senderName = Context.User.Identity?.Name ?? "User";

            var request = new SendMessageRequestDTO
            {
                ThreadId = threadId,
                Content = content,
                MessageType = messageType,
                ParentMessageId = parentMessageId
            };

            var (success, error, messageId) = await _messageService.SendMessageAsync(senderId, request);

            if (success)
            {
                var payload = new
                {
                    messageId = messageId,
                    threadId = threadId,
                    senderId = senderId,
                    senderName = senderName,
                    content = content,
                    messageType = messageType,
                    sentAtUTC = DateTime.UtcNow,
                    parentMessageId = parentMessageId
                };

                await Clients.Group(threadId.ToString()).SendAsync("ReceiveMessage", payload);
            }
        }

        public async Task SendTyping(long threadId, bool isTyping)
        {
            var senderName = Context.User.Identity?.Name ?? "User";
            await Clients.Group(threadId.ToString()).SendAsync("UserTyping", new { threadId, isTyping, displayName = senderName });
        }

        public async Task SendReaction(long threadId, long messageId, string reactionType)
        {
            var userId = Context.User.GetUserId();
            var displayName = Context.User.Identity?.Name ?? "User";

            var request = new ToggleReactionRequestDTO { MessageId = messageId, ReactionType = reactionType };
            await _messageService.ToggleReactionAsync(userId, request);

            await Clients.Group(threadId.ToString()).SendAsync("ReactionUpdated", new { threadId, messageId, userId, displayName, reactionType });
        }

        public async Task RevokeMessage(long threadId, long messageId)
        {
            var userId = Context.User.GetUserId();
            await _messageService.RevokeMessageAsync(userId, messageId);
            await Clients.Group(threadId.ToString()).SendAsync("MessageRevoked", new { threadId, messageId });
        }
    }
}