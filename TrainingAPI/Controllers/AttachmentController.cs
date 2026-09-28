using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingAPI.Services.Common;

namespace TrainingAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/attachments")]
    public class AttachmentController : ControllerBase
    {
        private readonly IAttachmentUploader _uploader;

        public AttachmentController(IAttachmentUploader uploader)
        {
            _uploader = uploader;
        }

        [HttpPost("upload")]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> UploadAttachment([FromForm] IFormFile file, CancellationToken cancellationToken)
        {
            var result = await _uploader.UploadAsync(file, cancellationToken);

            if (!result.Success)
            {
                var body = new { success = false, message = result.ErrorMessage };
                var msg = (result.ErrorMessage ?? string.Empty).ToLowerInvariant();

                if (msg.Contains("không hợp lệ")) return BadRequest(body);
                if (msg.Contains("vượt quá")) return StatusCode(StatusCodes.Status413PayloadTooLarge, body);
                return StatusCode(StatusCodes.Status502BadGateway, body);
            }

            return Ok(new
            {
                success = true,
                url = result.FileUrl,
                fileUrl = result.FileUrl,
                fileType = result.FileType,
                fileSize = result.FileSize,
                isImage = result.FileType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true
            });
        }
    }
}