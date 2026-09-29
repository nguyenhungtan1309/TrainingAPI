namespace TrainingAPI.Services.Messaging
{
    public abstract class MessageContentFormatter
    {
        public string BuildPreview(string rawContent, bool isRevoked)
        {
            if (isRevoked) return "Tin nhắn đã bị thu hồi";
            return FormatContent(rawContent);
        }

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
        protected override string FormatContent(string rawContent) => rawContent;
    }

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
