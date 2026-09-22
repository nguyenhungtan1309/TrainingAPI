using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using TrainingAPI.DTOs;
using TrainingAPI.Models;

namespace TrainingAPI.Controllers
{
    [ApiController]
    [Route("api/v1/users")]
    public class UserController : ControllerBase
    {
        private readonly CompanyContext _context;
        private readonly string _connectionString;

        public UserController(CompanyContext context, IConfiguration configuration)
        {
            _context = context;
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? _context.Database.GetConnectionString()!;
        }

        [HttpPost("block")]
        public async Task<IActionResult> ToggleBlockUser([FromQuery] int userId, [FromBody] ToggleBlockRequestDTO request)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("dbo.sp_ToggleBlockUser", conn) { CommandType = CommandType.StoredProcedure };

                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@BlockedUserId", request.BlockedUserId);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                return Ok(new { success = true });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("device-token")]
        public async Task<IActionResult> RegisterDevice([FromQuery] int userId, [FromBody] RegisterDeviceRequestDTO request)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("dbo.sp_RegisterDevice", conn) { CommandType = CommandType.StoredProcedure };

                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@DeviceToken", request.DeviceToken);
                cmd.Parameters.AddWithValue("@Platform", request.Platform);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                return Ok(new { success = true });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromQuery] int userId, [FromBody] UpdateProfileRequestDTO request)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.IsActive == false)
                return NotFound(new { message = "Người dùng không tồn tại hoặc đã bị khóa." });

            user.DisplayName = request.DisplayName.Trim();

            if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
            {
                user.AvatarUrl = request.AvatarUrl.Trim();
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                displayName = user.DisplayName,
                avatarUrl = user.AvatarUrl
            });
        }

        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromQuery] int userId, [FromBody] ChangePasswordRequestDTO request)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.IsActive == false)
                return NotFound(new { message = "Người dùng không tồn tại hoặc đã bị khóa." });

            string oldHash = ComputeSha256Hash(user.PasswordSalt + request.OldPassword);
            if (!string.Equals(user.PasswordHash, oldHash, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Mật khẩu cũ không chính xác." });
            }

            string newSalt = Guid.NewGuid().ToString("N").Substring(0, 16);
            string newHash = ComputeSha256Hash(newSalt + request.NewPassword);

            user.PasswordSalt = newSalt;
            user.PasswordHash = newHash;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đổi mật khẩu thành công." });
        }

        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}