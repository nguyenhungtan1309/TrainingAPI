using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingAPI.DTOs;
using TrainingAPI.Services.Common;

namespace TrainingAPI.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserAccountService _userService;

        public AuthController(IUserAccountService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO request, CancellationToken cancellationToken)
        {
            var (success, error, userId) = await _userService.RegisterAsync(request, cancellationToken);
            if (!success) return BadRequest(new { success = false, message = error });
            return Ok(new { success = true, userId });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request, CancellationToken cancellationToken)
        {
            var (success, error, token, refreshToken, userId, username, displayName, avatarUrl) = await _userService.LoginAsync(request, cancellationToken);
            if (!success) return BadRequest(new { success = false, message = error });

            return Ok(new
            {
                success = true,
                token,
                refreshToken,
                userId,
                username,
                displayName,
                avatarUrl
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] TokenRequestDTO request, CancellationToken cancellationToken)
        {
            var (success, error, newAccessToken, newRefreshToken) = await _userService.RefreshTokenAsync(request, cancellationToken);
            if (!success) return Unauthorized(new { success = false, message = error });

            return Ok(new
            {
                success = true,
                token = newAccessToken,
                refreshToken = newRefreshToken
            });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            await _userService.LogoutAsync(userId, cancellationToken);
            return Ok(new { success = true });
        }
    }
}