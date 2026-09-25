using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingAPI.DTOs;
using TrainingAPI.Services.Common;

namespace TrainingAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/users")]
    [Route("api/v1/chat/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserAccountService _userService;

        public UserController(IUserAccountService userService)
        {
            _userService = userService;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string keyword, CancellationToken cancellationToken)
        {
            var currentUserId = User.GetUserId();
            var (success, error, users) = await _userService.SearchUsersAsync(currentUserId, keyword ?? string.Empty, cancellationToken);
            if (!success) return BadRequest(new { success = false, message = error });
            return Ok(users);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDTO request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var (success, error) = await _userService.UpdateProfileAsync(userId, request, cancellationToken);
            if (!success) return BadRequest(new { success = false, message = error });
            return Ok(new { success = true });
        }

        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDTO request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var (success, error) = await _userService.ChangePasswordAsync(userId, request, cancellationToken);
            if (!success) return BadRequest(new { success = false, message = error });
            return Ok(new { success = true });
        }

        [HttpPost("block")]
        public async Task<IActionResult> ToggleBlock([FromBody] ToggleBlockRequestDTO request, CancellationToken cancellationToken)
        {
            var currentUserId = User.GetUserId();
            var (success, error) = await _userService.ToggleBlockUserAsync(currentUserId, request.BlockedUserId, cancellationToken);
            if (!success) return BadRequest(new { success = false, message = error });
            return Ok(new { success = true });
        }

        [HttpPost("device")]
        public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequestDTO request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var (success, error) = await _userService.RegisterDeviceAsync(userId, request, cancellationToken);
            if (!success) return BadRequest(new { success = false, message = error });
            return Ok(new { success = true });
        }
    }
}