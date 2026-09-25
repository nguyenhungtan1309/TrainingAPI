using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingAPI.DTOs;
using TrainingAPI.Services.Common;
using TrainingAPI.Services.Messaging;

namespace TrainingAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/chat")]
    public class ChatController : ControllerBase
    {
        private readonly IChatThreadService _threadService;
        private readonly IChatMessageService _messageService;

        public ChatController(IChatThreadService threadService, IChatMessageService messageService)
        {
            _threadService = threadService;
            _messageService = messageService;
        }

        [HttpPost("threads")]
        public async Task<IActionResult> CreateThread([FromBody] CreateThreadRequestDTO request, CancellationToken cancellationToken)
        {
            var creatorId = User.GetUserId();
            var (success, error, threadId) = await _threadService.CreateThreadAsync(creatorId, request, cancellationToken);
            if (!success) return BadRequest(new { success = false, message = error });
            return Ok(new { success = true, threadId });
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations([FromQuery] int top = 30, [FromQuery] DateTime? beforeTimeUTC = null, CancellationToken cancellationToken = default)
        {
            var viewerId = User.GetUserId();
            var (success, error, list) = await _threadService.GetConversationsAsync(viewerId, top, beforeTimeUTC, cancellationToken);
            if (!success) return BadRequest(new { success = false, message = error });
            return Ok(list);
        }

        [HttpGet("threads/{threadId}/participants")]
        public async Task<IActionResult> GetThreadParticipants([FromRoute] long threadId, CancellationToken cancellationToken)
        {
            var viewerId = User.GetUserId();
            var (success, error, list) = await _threadService.GetThreadParticipantsAsync(viewerId, threadId, cancellationToken);
            if (!success) return BadRequest(new { success = false, message = error });
            return Ok(list);
        }

        [HttpGet("threads/{threadId}/details")]
        public async Task<IActionResult> GetThreadDetails([FromRoute] long threadId, [FromQuery] int limit = 20, CancellationToken cancellationToken = default)
        {
            var viewerId = User.GetUserId();
            var (success, error, data) = await _threadService.GetThreadDetailsAsync(viewerId, threadId, limit, cancellationToken);
            if (!success) return BadRequest(new { success = false, message = error });
            return Ok(data);
        }

        // Bổ sung alias "threads/manage-participant" khớp với fetch ở chat.html
        [HttpPost("threads/participants/manage")]
        [HttpPost("threads/manage-participant")]
        public async Task<IActionResult> ManageParticipant([FromBody] ManageParticipantRequestDTO request, CancellationToken cancellationToken)
        {
            var actionUserId = User.GetUserId();
            var (success, error) = await _threadService.ManageParticipantAsync(actionUserId, request, cancellationToken);
            if (!success) return BadRequest(new { success = false, message = error });
            return Ok(new { success = true });
        }

        // Bổ sung alias "threads/{threadId}/roles" khớp với fetch ở chat.html
        [HttpPut("threads/{threadId}/participants/role")]
        [HttpPut("threads/{threadId}/roles")]
        public async Task<IActionResult> UpdateParticipantRole([FromRoute] long threadId, [FromBody] UpdateRoleRequestDTO request, CancellationToken cancellationToken)
        {
            var actionUserId = User.GetUserId();
            var (success, error) = await _threadService.UpdateParticipantRoleAsync(actionUserId, threadId, request, cancellationToken);
            if (!success) return BadRequest(new { success = false, message = error });
            return Ok(new { success = true });
        }

        [HttpPut("threads/{threadId}/title")]
        public async Task<IActionResult> UpdateThreadTitle([FromRoute] long threadId, [FromBody] UpdateTitleRequestDTO request, CancellationToken cancellationToken)
        {
            var actionUserId = User.GetUserId();
            var (success, error) = await _threadService.UpdateThreadTitleAsync(actionUserId, threadId, request, cancellationToken);
            if (!success) return BadRequest(new { success = false, message = error });
            return Ok(new { success = true });
        }

        [HttpPost("threads/{threadId}/hide")]
        [HttpPost("threads/{threadId}/toggle-hide")]
        public async Task<IActionResult> ToggleHideThread([FromRoute] long threadId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var (success, error) = await _threadService.ToggleHideThreadAsync(userId, threadId, cancellationToken);
            if (!success) return BadRequest(new { success = false, message = error });
            return Ok(new { success = true });
        }

        [HttpPost("threads/{threadId}/mute")]
        public async Task<IActionResult> MuteThread([FromRoute] long threadId, [FromBody] MuteThreadRequestDTO request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var (success, error) = await _threadService.MuteThreadAsync(userId, threadId, request, cancellationToken);
            if (!success) return BadRequest(new { success = false, message = error });
            return Ok(new { success = true });
        }

        // ================== MESSAGE ACTIONS ==================

        [HttpPost("messages")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequestDTO request, CancellationToken cancellationToken)
        {
            var senderId = User.GetUserId();
            var (success, error, messageId) = await _messageService.SendMessageAsync(senderId, request, cancellationToken);
            if (!success) return BadRequest(new { success = false, message = error });
            return Ok(new { success = true, messageId });
        }

        [HttpGet("threads/{threadId}/messages")]
        [HttpGet("messages")]
        public async Task<IActionResult> GetThreadMessages(
            [FromRoute] long threadId,
            [FromQuery(Name = "threadId")] long? queryThreadId,
            [FromQuery] long? cursorMessageId = null,
            [FromQuery] int limit = 20,
            CancellationToken cancellationToken = default)
        {
            var viewerId = User.GetUserId();
            var effectiveThreadId = threadId > 0 ? threadId : (queryThreadId ?? 0);
            var (success, error, messages) = await _messageService.GetThreadMessagesAsync(viewerId, effectiveThreadId, cursorMessageId, limit, cancellationToken);
            if (!success) return BadRequest(new { success = false, message = error });
            return Ok(messages);
        }

        [HttpPost("messages/{messageId}/revoke")]
        public async Task<IActionResult> RevokeMessage([FromRoute] long messageId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var (success, error) = await _messageService.RevokeMessageAsync(userId, messageId, cancellationToken);
            if (!success) return BadRequest(new { success = false, message = error });
            return Ok(new { success = true });
        }

        [HttpPut("messages/{messageId}")]
        public async Task<IActionResult> EditMessage([FromRoute] long messageId, [FromBody] EditMessageRequestDTO request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            request.MessageId = messageId;
            var (success, error) = await _messageService.EditMessageAsync(userId, request, cancellationToken);
            if (!success) return BadRequest(new { success = false, message = error });
            return Ok(new { success = true });
        }

        [HttpPost("threads/{threadId}/messages/{messageId}/pin")]
        public async Task<IActionResult> TogglePinMessage([FromRoute] long threadId, [FromRoute] long messageId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var (pinSuccess, pinError) = await _messageService.TogglePinMessageAsync(userId, threadId, messageId, cancellationToken);
            if (!pinSuccess) return BadRequest(new { success = false, message = pinError });
            return Ok(new { success = true });
        }

        [HttpDelete("messages/{messageId}/for-me")]
        public async Task<IActionResult> DeleteMessageForMe([FromRoute] long messageId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var (success, error) = await _messageService.DeleteMessageForMeAsync(userId, messageId, cancellationToken);
            if (!success) return BadRequest(new { success = false, message = error });
            return Ok(new { success = true });
        }

        [HttpPost("reactions")]
        public async Task<IActionResult> ToggleReaction([FromBody] ToggleReactionRequestDTO request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var (success, error) = await _messageService.ToggleReactionAsync(userId, request, cancellationToken);
            if (!success) return BadRequest(new { success = false, message = error });
            return Ok(new { success = true });
        }
    }
}