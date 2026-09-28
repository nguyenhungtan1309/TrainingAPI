using Microsoft.AspNetCore.Mvc;

namespace TrainingAPI.Controllers
{
    /// <summary>
    /// [Mục 6/11 - API Status Result] Phân loại thông điệp lỗi do Service/SP trả về
    /// thành đúng mã HTTP, thay vì dồn hết vào 400.
    ///
    /// Vì sao phân loại theo nội dung thông điệp: các SP dùng RAISERROR (mọi lỗi tùy biến
    /// đều có Number = 50000) và Service chỉ trả về chuỗi lỗi, nên chưa có "mã lỗi" để
    /// phân biệt. Toàn bộ quy tắc gom về MỘT nơi này để dễ sửa/kiểm tra.
    /// Thông điệp không nhận diện được sẽ rơi về 400 (giữ hành vi cũ, an toàn).
    /// Cách bền hơn về lâu dài: SP dùng THROW với số lỗi riêng (vd 50403, 50404, 50409).
    ///
    /// LƯU Ý QUAN TRỌNG: tuyệt đối KHÔNG trả 401 cho lỗi nghiệp vụ trong các API của chat.html,
    /// vì hàm fetch bọc sẵn ở đó coi mọi 401 là "token hết hạn" -> tự refresh rồi đá về /login.
    /// </summary>
    public static class ApiErrorMapper
    {
        private static readonly string[] Forbidden =
        {
            "không thuộc cuộc trò chuyện", "lỗi quyền", "bị chặn", "không có quyền truy cập",
            "chỉ trưởng nhóm"
        };

        private static readonly string[] NotFound =
        {
            "không tìm thấy", "không tồn tại", "user not found", "tin nhắn không thuộc"
        };

        private static readonly string[] Conflict =
        {
            "đã tồn tại", "trưởng nhóm duy nhất", "đang bận xử lý", "violation of unique",
            "duplicate key", "deadlock"
        };

        public static IActionResult ErrorResult(this ControllerBase controller, string? error)
        {
            var message = error ?? "Yêu cầu không hợp lệ.";
            var body = new { success = false, message };
            var text = message.ToLowerInvariant();

            if (Contains(text, Forbidden)) return controller.StatusCode(StatusCodes.Status403Forbidden, body);
            if (Contains(text, NotFound)) return controller.NotFound(body);
            if (Contains(text, Conflict)) return controller.Conflict(body);
            return controller.BadRequest(body);
        }

        private static bool Contains(string text, string[] keywords)
            => keywords.Any(k => text.Contains(k, StringComparison.Ordinal));
    }
}
