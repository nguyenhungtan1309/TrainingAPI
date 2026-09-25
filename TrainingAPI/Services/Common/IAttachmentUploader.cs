using Microsoft.AspNetCore.Http;
using TrainingAPI.DTOs;

namespace TrainingAPI.Services.Common
{
    public interface IAttachmentUploader
    {
        Task<AttachmentUploadResult> UploadAsync(IFormFile file, CancellationToken cancellationToken = default);
    }
}