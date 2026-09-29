using Microsoft.AspNetCore.Mvc;

namespace TrainingAPI.Controllers
{

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
