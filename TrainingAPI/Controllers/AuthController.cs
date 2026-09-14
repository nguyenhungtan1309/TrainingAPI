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

        private static string ComputeHash(string salt, string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(salt + password));
            return Convert.ToHexString(bytes).ToLower();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

            if (user == null)
            {
                return Unauthorized(new { message = "Tài khoản hoặc mật khẩu không chính xác." });
            }

            var hash = ComputeHash(user.PasswordSalt, request.Password);
            if (hash != user.PasswordHash)
            {
                return Unauthorized(new { message = "Tài khoản hoặc mật khẩu không chính xác." });
            }

            user.LastActiveAtUTC = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var jwtKey = _configuration["Jwt:Key"] ?? "Chuoi_Khoa_Bi_Mat_Cuc_Ky_Dai_Va_An_Toan_Tren_32_Ky_Tu_Cho_TrainingAPI";
            var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("DisplayName", user.DisplayName)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = _configuration["Jwt:Issuer"] ?? "TrainingAPI",
                Audience = _configuration["Jwt:Audience"] ?? "TrainingAPIClient",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(securityToken);

            return Ok(new LoginResponseDTO(
                Token: tokenString,
                UserId: user.Id,
                Username: user.Username,
                DisplayName: user.DisplayName,
                AvatarUrl: user.AvatarUrl
            ));
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO request)
        {
            if (await _context.AppUsers.AnyAsync(u => u.Username == request.Username))
            {
                return BadRequest(new { message = "Tên đăng nhập đã tồn tại." });
            }

            var salt = Guid.NewGuid().ToString("N")[..16];
            var hash = ComputeHash(salt, request.Password);

            var newUser = new AppUser
            {
                Username = request.Username,
                DisplayName = request.DisplayName,
                AvatarUrl = request.AvatarUrl,
                PasswordSalt = salt,
                PasswordHash = hash,
                IsActive = true,
                CreatedAtUTC = DateTime.UtcNow
            };

            _context.AppUsers.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đăng ký tài khoản thành công!", userId = newUser.Id });
        }
    }
}