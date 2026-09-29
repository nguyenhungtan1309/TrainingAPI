using System.Security.Cryptography;
using System.Text;

namespace TrainingAPI.Services.Common
{
    public static class RefreshTokenHasher
    {
        public static string Hash(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                throw new ArgumentException("Refresh token không được để trống.", nameof(refreshToken));

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
