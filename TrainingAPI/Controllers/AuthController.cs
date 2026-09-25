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
            var (success, error, token, userId, username, displayName, avatarUrl) = await _userService.LoginAsync(request, cancellationToken);

            if (!success) return BadRequest(new { success = false, message = error });

            return Ok(new
            {
                success = true,
                token = token,
                userId = userId,
                username = username,
                displayName = displayName,
                avatarUrl = avatarUrl
            });
        }
    }
}