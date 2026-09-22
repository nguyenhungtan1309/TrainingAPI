using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TrainingAPI.DTOs;
using TrainingAPI.Models;
using TrainingAPI.Models.Users;

namespace TrainingAPI.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly CompanyContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(CompanyContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO request)
        {
            var userExists = await _context.AppUsers.AnyAsync(u => u.Username.ToLower() == request.Username.ToLower());
            if (userExists)
            {
                return BadRequest(new { message = "Tên đăng nhập này đã được sử dụng." });
            }

            string newSalt = Guid.NewGuid().ToString("N").Substring(0, 16);
            string hashedPassword = ComputeSha256Hash(newSalt + request.Password);

            var newUser = new AppUser
            {
                Username = request.Username,
                DisplayName = request.DisplayName.Trim(),
                PasswordSalt = newSalt,
                PasswordHash = hashedPassword,
                IsActive = true
            };

            _context.AppUsers.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đăng ký tài khoản thành công!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());

            if (user == null || user.IsActive == false)
            {
                return BadRequest(new { message = "Tài khoản hoặc mật khẩu không chính xác." });
            }

            string computedHash = ComputeSha256Hash(user.PasswordSalt + request.Password);
            if (!string.Equals(user.PasswordHash, computedHash, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Tài khoản hoặc mật khẩu không chính xác." });
            }

            string token = GenerateJwtToken(user);

            return Ok(new
            {
                token,
                userId = user.Id,
                username = user.Username,
                displayName = user.DisplayName,
                avatarUrl = user.AvatarUrl
            });
        }

        private string GenerateJwtToken(AppUser user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "ChuoiBiMatMacDinhRatDaiChoChatSystem123456789!";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "TrainingAPI";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "TrainingClient";

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("displayName", user.DisplayName)
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
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