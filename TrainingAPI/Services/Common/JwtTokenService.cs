using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TrainingAPI.Models.Users;

namespace TrainingAPI.Services.Common
{
    /// <summary>
    /// [2.1][Encapsulation] Hiện thực duy nhất của IJwtTokenService.
    /// Nhận JwtSettings qua IOptions&lt;JwtSettings&gt; - CÙNG một nguồn
    /// cấu hình mà Program.cs dùng để cấu hình AddJwtBearer, nên token
    /// được ký ở đây LUÔN verify được ở middleware xác thực.
    /// </summary>
    public class JwtTokenService : IJwtTokenService
    {
        private readonly JwtSettings _settings;

        public JwtTokenService(IOptions<JwtSettings> options)
        {
            _settings = options.Value;
        }

        public string GenerateAccessToken(AppUser user)
        {
            var keyBytes = Encoding.UTF8.GetBytes(_settings.Key);
            var securityKey = new SymmetricSecurityKey(keyBytes);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Claim tối thiểu cho đợt hạ tầng này. Đợt học JWT/Authorization
            // sau sẽ bổ sung thêm claim role/permission khi cần role-based
            // hoặc claim-based authorization.
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("displayName", user.DisplayName)
            };

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiresMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
