namespace TrainingAPI.Services.Common
{
    /// <summary>
    /// [2.1][Interface][Abstraction] Hợp đồng cho việc băm và kiểm tra mật khẩu.
    /// Trước đây AuthController và UserController mỗi nơi tự viết một bản
    /// ComputeSha256Hash giống hệt nhau (copy-paste). Nơi gọi (Controller)
    /// giờ chỉ cần biết "băm được, kiểm tra được" qua interface này,
    /// không cần biết thuật toán cụ thể bên trong là gì - đúng bản chất
    /// của Abstraction: tách "làm được gì" khỏi "làm như thế nào".
    ///
    /// Lợi ích thực tế: nếu sau này muốn đổi sang BCrypt/Argon2 (an toàn
    /// hơn SHA-256 thuần cho mật khẩu), chỉ cần viết thêm một class mới
    /// hiện thực interface này và đổi 1 dòng đăng ký DI - không phải sửa
    /// AuthController hay UserController.
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>Sinh salt ngẫu nhiên mới, dùng khi tạo tài khoản hoặc đổi mật khẩu.</summary>
        string GenerateSalt();

        /// <summary>Băm mật khẩu thô kèm salt để lưu vào DB.</summary>
        string Hash(string rawPassword, string salt);

        /// <summary>So khớp mật khẩu người dùng nhập lúc đăng nhập với hash đã lưu.</summary>
        bool Verify(string rawPassword, string salt, string expectedHash);
    }
}
