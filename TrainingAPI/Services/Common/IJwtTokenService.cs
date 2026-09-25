using TrainingAPI.Models.Users;

namespace TrainingAPI.Services.Common
{
    /// <summary>
    /// [2.1][Interface] Đóng gói toàn bộ việc TẠO token JWT.
    /// AuthController trước đây tự làm điều này trong một private method
    /// (GenerateJwtToken) ngay trong chính nó - vừa lặp việc đọc cấu hình
    /// (xem JwtSettings.cs), vừa khiến AuthController không thể tái sử
    /// dụng logic này ở nơi khác (ví dụ endpoint refresh token sẽ thêm
    /// ở đợt học JWT sau) mà không copy-paste.
    ///
    /// Interface này được thiết kế để MỞ RỘNG được ở đợt sau (thêm
    /// GenerateRefreshToken, ValidateRefreshToken...) mà không phá vỡ
    /// nơi đang dùng GenerateAccessToken hiện tại - đúng tinh thần
    /// Abstraction: nơi gọi chỉ phụ thuộc vào "khả năng", không phụ
    /// thuộc chi tiết hiện thực.
    /// </summary>
    public interface IJwtTokenService
    {
        string GenerateAccessToken(AppUser user);
    }
}
