using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using TrainingAPI.Models;

namespace TrainingAPI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly CompanyContext _context;

        public ChatHub(CompanyContext context)
        {
            _context = context;
        }

        public async Task SendMessageToUser(int receiverId, string message)
        {
            var senderId = int.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var senderName = Context.User!.FindFirst("DisplayName")?.Value
                          ?? Context.User!.FindFirst(ClaimTypes.Name)?.Value
                          ?? "Unknown";

            var chatMsg = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                MessageContent = message,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.ChatMessages.Add(chatMsg);
            await _context.SaveChangesAsync();

            await Clients.Users(receiverId.ToString(), senderId.ToString())
                .SendAsync("ReceiveMessage", senderId, senderName, message, chatMsg.SentAt);
        }
    }
}