using System.Security.Cryptography;
using System.Text;

namespace TrainingAPI.Services.Common
{
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
