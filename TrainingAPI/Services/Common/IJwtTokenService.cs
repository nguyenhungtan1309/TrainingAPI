using System.Security.Claims;
using TrainingAPI.Models.Users;

namespace TrainingAPI.Services.Common
{
    public interface IJwtTokenService
    {
        string GenerateAccessToken(AppUser user);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}