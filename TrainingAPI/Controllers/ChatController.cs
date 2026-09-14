using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrainingAPI.DTOs;
using TrainingAPI.Models;

namespace TrainingAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly CompanyContext _context;

        public ChatController(CompanyContext context)
        {
            _context = context;
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var conversations = await _context.ConversationItems
                .Where(c => c.UserOneId == currentUserId || c.UserTwoId == currentUserId)
                .OrderByDescending(c => c.LastSentAt)
                .Select(c => new
                {
                    PartnerId = c.UserOneId == currentUserId ? c.UserTwoId : c.UserOneId,
                    PartnerName = c.UserOneId == currentUserId ? c.UserTwoName : c.UserOneName,
                    LastMessage = c.LastMessageContent,
                    LastSentAt = c.LastSentAt,
                    IsMyMessage = c.LastSenderId == currentUserId
                })
                .ToListAsync();

            return Ok(conversations);
        }

        [HttpGet("history/{partnerId:int}")]
        public async Task<IActionResult> GetChatHistory(int partnerId)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var paramUserA = new SqlParameter("@UserAId", currentUserId);
            var paramUserB = new SqlParameter("@UserBId", partnerId);

            var history = await _context.ChatHistoryItems
                .FromSqlRaw("EXEC dbo.sp_GetChatHistory @UserAId, @UserBId", paramUserA, paramUserB)
                .ToListAsync();

            return Ok(history);
        }

        [HttpGet("contacts")]
        public async Task<IActionResult> GetContacts()
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var users = await _context.AppUsers
                .Where(u => u.Id != currentUserId && u.IsActive)
                .Select(u => new { u.Id, u.DisplayName, u.Username })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("search-users")]
        public async Task<IActionResult> SearchUsers([FromQuery] string? keyword)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            if (string.IsNullOrWhiteSpace(keyword))
            {
                return Ok(new List<UserSearchDTO>());
            }

            var paramCurrentUserId = new SqlParameter("@CurrentUserId", currentUserId);
            var paramKeyword = new SqlParameter("@Keyword", keyword.Trim());

            var users = await _context.Database
                .SqlQueryRaw<UserSearchDTO>("EXEC dbo.sp_SearchUsersToChat @CurrentUserId, @Keyword", paramCurrentUserId, paramKeyword)
                .ToListAsync();

            return Ok(users);
        }
    }
}