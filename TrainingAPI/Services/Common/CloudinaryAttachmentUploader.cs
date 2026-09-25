using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TrainingAPI.DTOs;

namespace TrainingAPI.Services.Common
{
    // Composition: CloudinaryAttachmentUploader "chứa" (has-a) một Cloudinary client được inject từ ngoài
    public class CloudinaryAttachmentUploader : IAttachmentUploader
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryAttachmentUploader> _logger;

        public CloudinaryAttachmentUploader(Cloudinary cloudinary, ILogger<CloudinaryAttachmentUploader> logger)
        {
            _cloudinary = cloudinary;
            _logger = logger;
        }

        public async Task<AttachmentUploadResult> UploadAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                return AttachmentUploadResult.Failed("Tệp tải lên không hợp lệ.");
            }

            if (file.Length > 20 * 1024 * 1024)
            {
                return AttachmentUploadResult.Failed("Dung lượng tệp không được vượt quá 20MB.");
            }

            try
            {
                await using var stream = file.OpenReadStream();
                var isImage = file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

                if (isImage)
                {
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(file.FileName, stream),
                        Folder = "chat_uploads",
                        UseFilename = true,
                        UniqueFilename = true,
                        Overwrite = false
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
                    if (uploadResult.Error != null)
                    {
                        return AttachmentUploadResult.Failed(uploadResult.Error.Message);
                    }

                    return AttachmentUploadResult.Succeeded(
                        uploadResult.SecureUrl?.ToString() ?? uploadResult.Url.ToString(),
                        file.ContentType,
                        (int)file.Length);
                }
                else
                {
                    var rawParams = new RawUploadParams
                    {
                        File = new FileDescription(file.FileName, stream),
                        Folder = "chat_uploads",
                        UseFilename = true,
                        UniqueFilename = true,
                        Overwrite = false
                    };

                    var uploadResult = await _cloudinary.UploadLargeAsync(rawParams, cancellationToken: cancellationToken);
                    if (uploadResult.Error != null)
                    {
                        return AttachmentUploadResult.Failed(uploadResult.Error.Message);
                    }

                    return AttachmentUploadResult.Succeeded(
                        uploadResult.SecureUrl?.ToString() ?? uploadResult.Url.ToString(),
                        file.ContentType,
                        (int)file.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình upload tệp {FileName}", file.FileName);
                return AttachmentUploadResult.Failed("Đã xảy ra lỗi khi tải tệp lên máy chủ lưu trữ.");
            }
        }
    }
}