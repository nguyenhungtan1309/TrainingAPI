namespace TrainingAPI.DTOs
{
    public class AttachmentUploadResult
    {
        public bool Success { get; set; }
        public string? FileUrl { get; set; }
        public string? FileType { get; set; }
        public int FileSize { get; set; }
        public string? ErrorMessage { get; set; }

        public static AttachmentUploadResult Succeeded(string url, string type, int size) =>
            new() { Success = true, FileUrl = url, FileType = type, FileSize = size };

        public static AttachmentUploadResult Failed(string error) =>
            new() { Success = false, ErrorMessage = error };
    }
}