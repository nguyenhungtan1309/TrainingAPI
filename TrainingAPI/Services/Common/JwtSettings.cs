namespace TrainingAPI.Services.Common
{
    /// <summary>
    /// Nơi DUY NHẤT khai báo cấu hình JWT trong toàn bộ ứng dụng.
    ///
    /// LỖI THẬT ĐÃ TỒN TẠI TRƯỚC KHI SỬA: Program.cs (lúc cấu hình
    /// AddJwtBearer) và AuthController (lúc ký token) mỗi nơi tự đọc
    /// configuration["Jwt:Key"] với một giá trị default fallback KHÁC
    /// NHAU khi thiếu cấu hình:
    ///   - Program.cs:        "Chuoi_Khoa_Bi_Mat_Cuc_Ky_Dai_Va_An_Toan_Tren_32_Ky_Tu_Cho_TrainingAPI"
    ///   - AuthController:    "ChuoiBiMatMacDinhRatDaiChoChatSystem123456789!"
    /// Nếu appsettings.json mất mục "Jwt:Key", hai nơi sẽ ký và verify
    /// token bằng 2 khóa khác nhau -> MỌI token luôn bị từ chối (401)
    /// mà không có thông báo lỗi rõ ràng nào, rất khó chẩn đoán.
    /// Gộp về đúng MỘT class cấu hình, dùng chung ở cả 2 nơi, loại bỏ
    /// hẳn khả năng lệch giá trị này.
    /// </summary>
    public class JwtSettings
    {
        public const string SectionName = "Jwt";

        public string Key { get; set; } = "Chuoi_Khoa_Bi_Mat_Cuc_Ky_Dai_Va_An_Toan_Tren_32_Ky_Tu_Cho_TrainingAPI";
        public string Issuer { get; set; } = "TrainingAPI";
        public string Audience { get; set; } = "TrainingAPIClient";

        /// <summary>Giữ đúng hành vi cũ: access token hết hạn sau 7 ngày.</summary>
        public int AccessTokenExpiresMinutes { get; set; } = 60 * 24 * 7;
    }
}
