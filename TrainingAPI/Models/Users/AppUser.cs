using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrainingAPI.Models.Users
{
    /// <summary>
    /// [2.1][Encapsulation] TRƯỚC KHI SỬA: mọi property đều "public get; set;",
    /// nghĩa là bất kỳ đâu trong code cũng có thể viết:
    ///     user.PasswordHash = "";              // xóa sạch mật khẩu
    ///     user.IsActive = true;                // "mở khóa" tài khoản mà không qua kiểm tra gì
    /// mà không đi qua bất kỳ điều kiện nghiệp vụ nào, và AppUser có thể
    /// được tạo ra ở trạng thái thiếu dữ liệu bắt buộc (new AppUser()
    /// rồi gán property tùy ý, có thể quên gán PasswordHash).
    ///
    /// SAU KHI SỬA: constructor validate ngay khi tạo (fail fast), và
    /// mọi thay đổi trạng thái chỉ được thực hiện qua các method có
    /// kiểm soát bên dưới - đúng nguyên tắc "AppUser không bao giờ tồn
    /// tại ở trạng thái không hợp lệ" đã nêu ở lý thuyết Encapsulation.
    /// </summary>
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

        /// <summary>
        /// Constructor rỗng DÀNH RIÊNG cho EF Core dùng khi tải dữ liệu
        /// từ DB lên (materialize) - EF Core gán property qua private
        /// setter bằng reflection, không đi qua constructor có tham số
        /// bên dưới. Tầng ứng dụng KHÔNG được gọi constructor này.
        /// </summary>
        private AppUser()
        {
        }

        /// <summary>
        /// Constructor validate: một AppUser KHÔNG THỂ được tạo ra nếu
        /// thiếu username/displayName, hoặc chưa có mật khẩu đã băm sẵn
        /// (hash/salt phải được tính TRƯỚC bằng IPasswordHasher rồi mới
        /// truyền vào đây - AppUser không tự băm mật khẩu, tách đúng
        /// trách nhiệm giữa Model và Service).
        /// </summary>
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

        /// <summary>Cập nhật hồ sơ hiển thị - chỉ qua đây, không gán trực tiếp property từ Controller.</summary>
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

        /// <summary>
        /// Đổi mật khẩu - LUÔN nhận hash/salt ĐÃ được băm sẵn bởi
        /// IPasswordHasher ở tầng Controller/Service; class này không tự
        /// băm để tránh AppUser phụ thuộc ngược vào chi tiết thuật toán.
        /// </summary>
        public void ChangePassword(string newPasswordHash, string newPasswordSalt)
        {
            if (string.IsNullOrWhiteSpace(newPasswordHash) || string.IsNullOrWhiteSpace(newPasswordSalt))
                throw new ArgumentException("Thiếu hash hoặc salt của mật khẩu mới.");

            PasswordHash = newPasswordHash;
            PasswordSalt = newPasswordSalt;
        }

        /// <summary>Khóa tài khoản - thay cho "user.IsActive = false" gán trực tiếp.</summary>
        public void Deactivate() => IsActive = false;

        /// <summary>Mở khóa tài khoản.</summary>
        public void Activate() => IsActive = true;

        /// <summary>Ghi nhận mốc hoạt động gần nhất (chưa có nơi gọi ở đợt này, chuẩn bị sẵn cho sau).</summary>
        public void RecordActivity() => LastActiveAtUTC = DateTime.UtcNow;
    }
}
