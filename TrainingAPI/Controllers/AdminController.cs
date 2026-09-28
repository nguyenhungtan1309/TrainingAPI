using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingAPI.Services.Common;

namespace TrainingAPI.Controllers
{
    /// <summary>
    /// [2.4][Role-based Authorization] Các API quản trị hệ thống - chỉ tài khoản có SystemRole = "Admin".
    ///
    /// Phân biệt với quyền trong nhóm chat: ThreadParticipant.Role (Admin/Deputy/Member) chỉ có nghĩa trong
    /// MỘT hội thoại cụ thể và được kiểm tra trong SP theo ThreadId; còn Roles = "Admin" ở đây là vai trò
    /// TOÀN HỆ THỐNG, nằm sẵn trong JWT nên [Authorize(Roles = ...)] kiểm tra được ngay.
    ///
    /// Kết quả mong đợi: chưa đăng nhập -> 401; đã đăng nhập nhưng không phải Admin -> 403.
    /// </summary>
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
