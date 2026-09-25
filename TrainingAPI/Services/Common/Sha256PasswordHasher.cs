using System.Security.Cryptography;
using System.Text;

namespace TrainingAPI.Services.Common
{
    /// <summary>
    /// [2.1][Encapsulation] Toàn bộ chi tiết "băm bằng SHA-256, nối salt
    /// trước mật khẩu, in ra hex thường" được giấu kín bên trong class
    /// này. AuthController/UserController gọi Hash()/Verify() mà không
    /// biết (và không cần biết) đây là SHA-256 hay thuật toán gì khác.
    ///
    /// [2.2][DI - sẽ đăng ký ở đợt sau] Class này không giữ trạng thái
    /// (stateless) và rẻ để tạo mới, nên phù hợp đăng ký Transient.
    /// </summary>
    public class Sha256PasswordHasher : IPasswordHasher
    {
        public string GenerateSalt() => Guid.NewGuid().ToString("N").Substring(0, 16);

        public string Hash(string rawPassword, string salt)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(salt + rawPassword));

            var builder = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }

        public bool Verify(string rawPassword, string salt, string expectedHash)
            => string.Equals(Hash(rawPassword, salt), expectedHash, StringComparison.OrdinalIgnoreCase);
    }
}
