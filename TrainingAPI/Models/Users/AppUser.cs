using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;

namespace TrainingAPI.Models.Users
{
    public static class SystemRoles
    {
        public const string User = "User";
        public const string Admin = "Admin";

        public static bool IsValid(string? role) => role == User || role == Admin;
    }

    [Table("AppUser")]
    public class AppUser
    {
        [Key]
        public int Id { get; private set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; private set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string DisplayName { get; private set; } = string.Empty;

        [MaxLength(1000)]
        public string? AvatarUrl { get; private set; }

        [Required]
        [MaxLength(128)]
        public string PasswordHash { get; private set; } = string.Empty;

        [Required]
        [MaxLength(64)]
        public string PasswordSalt { get; private set; } = string.Empty;

        public bool IsActive { get; private set; } = true;

        public DateTime? LastActiveAtUTC { get; private set; }

        public DateTime CreatedAtUTC { get; private set; } = DateTime.UtcNow;

        [MaxLength(20)]
        public string SystemRole { get; private set; } = SystemRoles.User;

        public string? RefreshToken { get; private set; }
        public DateTime? RefreshTokenExpiryTime { get; private set; }
        private AppUser()
        {
        }

        public AppUser(string username, string displayName, string passwordHash, string passwordSalt)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username không được để trống.", nameof(username));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("DisplayName không được để trống.", nameof(displayName));
            if (string.IsNullOrWhiteSpace(passwordHash) || string.IsNullOrWhiteSpace(passwordSalt))
                throw new ArgumentException("Tài khoản phải có mật khẩu đã được băm trước khi tạo.");

            Username = username.Trim();
            DisplayName = displayName.Trim();
            PasswordHash = passwordHash;
            PasswordSalt = passwordSalt;
            IsActive = true;
            CreatedAtUTC = DateTime.UtcNow;
        }

        public void UpdateProfile(string displayName, string? avatarUrl)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("DisplayName không được để trống.", nameof(displayName));

            DisplayName = displayName.Trim();

            if (!string.IsNullOrWhiteSpace(avatarUrl))
            {
                AvatarUrl = avatarUrl.Trim();
            }
        }

        public void ChangePassword(string newPasswordHash, string newPasswordSalt)
        {
            if (string.IsNullOrWhiteSpace(newPasswordHash) || string.IsNullOrWhiteSpace(newPasswordSalt))
                throw new ArgumentException("Thiếu hash hoặc salt của mật khẩu mới.");

            PasswordHash = newPasswordHash;
            PasswordSalt = newPasswordSalt;
        }

        public void Deactivate() => IsActive = false;

        public void Activate() => IsActive = true;

        public void RecordActivity() => LastActiveAtUTC = DateTime.UtcNow;

        public void SetSystemRole(string role)
        {
            if (!SystemRoles.IsValid(role))
                throw new ArgumentException("Vai trò hệ thống không hợp lệ (chỉ nhận User hoặc Admin).", nameof(role));

            SystemRole = role;
        }

        /// </summary>
        public void SetRefreshToken(string tokenHash, DateTime expiresAtUTC)
        {
            if (string.IsNullOrWhiteSpace(tokenHash))
                throw new ArgumentException("Refresh token (đã băm) không được để trống.", nameof(tokenHash));

            RefreshToken = tokenHash;
            RefreshTokenExpiryTime = expiresAtUTC;
        }

        public void ClearRefreshToken()
        {
            RefreshToken = null;
            RefreshTokenExpiryTime = null;
        }

        public bool IsRefreshTokenValid(string providedTokenHash)
        {
            if (string.IsNullOrEmpty(RefreshToken) || string.IsNullOrEmpty(providedTokenHash)) return false;
            if (!RefreshTokenExpiryTime.HasValue || RefreshTokenExpiryTime.Value <= DateTime.UtcNow) return false;

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(RefreshToken),
                Encoding.UTF8.GetBytes(providedTokenHash));
        }
    }
}
