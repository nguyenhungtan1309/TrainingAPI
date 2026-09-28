using System.Security.Cryptography;
using System.Text;

namespace TrainingAPI.Services.Common
{
    /// <summary>
    /// [2.4][JWT] Băm refresh token trước khi lưu DB.
    ///
    /// Vì sao dùng SHA-256 thuần, không salt (khác mật khẩu): refresh token là chuỗi ngẫu nhiên 64 byte
    /// (entropy rất cao) nên không thể dò từ điển/rainbow table như mật khẩu do người dùng đặt.
    /// Mục đích chỉ là: đọc được DB cũng không lấy được token dùng ngay.
    /// </summary>
    public static class RefreshTokenHasher
    {
        public static string Hash(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                throw new ArgumentException("Refresh token không được để trống.", nameof(refreshToken));

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
            return Convert.ToHexString(bytes).ToLowerInvariant(); // 64 ký tự, vừa cột NVARCHAR(200)
        }
    }
}
