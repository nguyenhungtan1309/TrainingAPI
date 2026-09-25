using TrainingAPI.DTOs;

namespace TrainingAPI.Services.Messaging.Validation
{
    public class TextMessageValidator : MessageValidator
    {
        public override ValidationResult Validate(SendMessageRequestDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return ValidationResult.Fail("Nội dung tin nhắn văn bản không được để trống.");
            }

            if (request.Content.Length > 4000)
            {
                return ValidationResult.Fail("Nội dung tin nhắn không được vượt quá 4000 ký tự.");
            }

            return ValidationResult.Success();
        }
    }

    public class ImageMessageValidator : MessageValidator
    {
        public override ValidationResult Validate(SendMessageRequestDTO request)
        {
            if (request.Attachments == null || request.Attachments.Count == 0)
            {
                return ValidationResult.Fail("Tin nhắn hình ảnh bắt buộc phải có tệp hình đính kèm.");
            }

            return ValidationResult.Success();
        }
    }

    public class FileMessageValidator : MessageValidator
    {
        public override ValidationResult Validate(SendMessageRequestDTO request)
        {
            if (request.Attachments == null || request.Attachments.Count == 0)
            {
                return ValidationResult.Fail("Tin nhắn tệp bắt buộc phải có tệp đính kèm.");
            }

            return ValidationResult.Success();
        }
    }

    public class SystemMessageValidator : MessageValidator
    {
        public override ValidationResult Validate(SendMessageRequestDTO request)
        {
            return ValidationResult.Fail("Người dùng không thể tự tạo tin nhắn hệ thống.");
        }
    }
}