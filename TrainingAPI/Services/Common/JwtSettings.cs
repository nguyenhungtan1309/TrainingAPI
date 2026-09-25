namespace TrainingAPI.Services.Common
{
    public class JwtSettings
    {
        public const string SectionName = "JwtSettings";
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;

        // Bổ sung thuộc tính cấu hình thời gian sống của Access Token (mặc định 60 phút)
        public int DurationInMinutes { get; set; } = 60;
    }
}