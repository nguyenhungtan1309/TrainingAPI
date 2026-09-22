using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;

namespace TrainingAPI.Controllers
{
    [ApiController]
    [Route("api/v1/attachments")]
    public class AttachmentController : ControllerBase
    {
        private readonly Cloudinary _cloudinary;

        public AttachmentController(IConfiguration configuration)
        {
            var acc = new Account(
                configuration["Cloudinary:CloudName"],
                configuration["Cloudinary:ApiKey"],
                configuration["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(acc);
            _cloudinary.Api.Secure = true;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Không tìm thấy file." });

            var uploadResult = new RawUploadResult();
            var isImage = file.ContentType.StartsWith("image/");

            using (var stream = file.OpenReadStream())
            {
                if (isImage)
                {
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(file.FileName, stream),
                        Folder = "chat_system/images"
                    };
                    uploadResult = await _cloudinary.UploadAsync(uploadParams);
                }
                else
                {
                    var uploadParams = new RawUploadParams
                    {
                        File = new FileDescription(file.FileName, stream),
                        Folder = "chat_system/files"
                    };
                    uploadResult = await _cloudinary.UploadAsync(uploadParams);
                }
            }

            if (uploadResult.Error != null)
                return StatusCode(500, new { message = uploadResult.Error.Message });

            return Ok(new
            {
                url = uploadResult.SecureUrl.ToString(),
                fileType = file.ContentType,
                fileSize = (int)file.Length,
                isImage = isImage
            });
        }
    }
}