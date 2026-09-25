using System.Security.Claims;

namespace TrainingAPI.Services.Common
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Người dùng chưa được xác thực hoặc token không hợp lệ.");
            }
            return userId;
        }
    }
}