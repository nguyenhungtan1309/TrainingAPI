namespace TrainingAPI.Services.Messaging
{
    /// <summary>
    /// [2.1][Abstract class] Vì sao dùng abstract class ở đây thay vì
    /// interface: 4 loại tin nhắn (Text/Image/File/System) có quan hệ
    /// "is-a" thật sự ổn định ("ImageMessageFormatter LÀ MỘT loại
    /// MessageContentFormatter"), và chúng chia sẻ chung MỘT hành vi
    /// khung không đổi (BuildPreview) - chỉ khác nhau ở đúng một bước
    /// nhỏ bên trong (FormatContent). Đây đúng tiêu chí đã nêu ở phần lý
    /// thuyết: "abstract class khi các lớp con cùng huyết thống và chia
    /// sẻ code/field chung, chỉ khác một phần hành vi". Nếu dùng
    /// interface, method BuildPreview (xử lý tin đã thu hồi) sẽ phải
    /// viết lặp lại ở cả 4 class con - đúng phần "nhược điểm interface"
    /// đã phân tích trước đó.
    ///
    /// Đây cũng KHÔNG phải trừu tượng hóa "phòng hờ": có sẵn 4 hiện thực
    /// thật, được dùng thật trong ChatHub (xem SendMessage/RevokeMessage),
    /// không phải tạo ra để "cho đủ bài tập" rồi không ai gọi tới.
    /// </summary>
    public abstract class MessageContentFormatter
    {
        /// <summary>
        /// Phần khung DÙNG CHUNG cho mọi loại tin nhắn: nếu tin đã bị
        /// thu hồi, luôn hiện "Tin nhắn đã bị thu hồi" bất kể loại gì.
        /// Đây là ví dụ khuôn mẫu (template method): lớp cha quyết định
        /// TRÌNH TỰ xử lý, lớp con chỉ quyết định một bước nhỏ bên trong.
        /// </summary>
        public string BuildPreview(string rawContent, bool isRevoked)
        {
            if (isRevoked) return "Tin nhắn đã bị thu hồi";
            return FormatContent(rawContent);
        }

        /// <summary>Chỉ phần NÀY khác nhau giữa các loại tin nhắn - lớp con bắt buộc tự hiện thực.</summary>
        protected abstract string FormatContent(string rawContent);
    }

    public sealed class TextMessageFormatter : MessageContentFormatter
    {
        protected override string FormatContent(string rawContent) => rawContent;
    }

    public sealed class ImageMessageFormatter : MessageContentFormatter
    {
        protected override string FormatContent(string rawContent) => "[Hình ảnh]";
    }

    public sealed class FileMessageFormatter : MessageContentFormatter
    {
        protected override string FormatContent(string rawContent) => "[Tệp đính kèm]";
    }

    public sealed class SystemMessageFormatter : MessageContentFormatter
    {
        // Tin loại System do chính server sinh ra (vd "X đã thêm Y vào
        // nhóm") đã là văn bản hiển thị được ngay, không cần định dạng lại.
        protected override string FormatContent(string rawContent) => rawContent;
    }

    /// <summary>
    /// Factory chọn đúng formatter theo MessageType. Đợt học Polymorphism
    /// (2.1 OOP #4) ở batch sau sẽ mở rộng cách chọn/dùng đa hình này kỹ
    /// hơn; ở đợt hạ tầng này, factory chỉ cần đủ dùng để tránh if/else
    /// rải rác tại nơi gọi (ChatHub).
    /// </summary>
    public static class MessageContentFormatterFactory
    {
        public static MessageContentFormatter Create(string messageType) => messageType switch
        {
            "Image" => new ImageMessageFormatter(),
            "File" => new FileMessageFormatter(),
            "System" => new SystemMessageFormatter(),
            _ => new TextMessageFormatter()
        };
    }
}
