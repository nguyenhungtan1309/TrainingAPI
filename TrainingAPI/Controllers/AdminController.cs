using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingAPI.Services.Common;

namespace TrainingAPI.Controllers
{

    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/v1/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IUserAccountService _userService;

        public AdminController(IUserAccountService userService)
        {
            _userService = userService;
        }

        [HttpPost("users/{userId:int}/deactivate")]
        public async Task<IActionResult> DeactivateUser([FromRoute] int userId, CancellationToken cancellationToken)
        {
            var adminId = User.GetUserId();
            var (success, error) = await _userService.SetUserActiveStatusAsync(adminId, userId, false, cancellationToken);
            if (!success) return this.ErrorResult(error);
            return Ok(new { success = true });
        }

        [HttpPost("users/{userId:int}/activate")]
        public async Task<IActionResult> ActivateUser([FromRoute] int userId, CancellationToken cancellationToken)
        {
            var adminId = User.GetUserId();
            var (success, error) = await _userService.SetUserActiveStatusAsync(adminId, userId, true, cancellationToken);
            if (!success) return this.ErrorResult(error);
            return Ok(new { success = true });
        }
    }
}
