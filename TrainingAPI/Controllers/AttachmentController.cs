using Microsoft.AspNetCore.Mvc;
using TrainingAPI.Services.Common;

namespace TrainingAPI.Controllers
{
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
                return BadRequest(new { success = false, message = result.ErrorMessage });
            }

            return Ok(new
            {
                success = true,
                fileUrl = result.FileUrl,
                fileType = result.FileType,
                fileSize = result.FileSize
            });
        }
    }
}