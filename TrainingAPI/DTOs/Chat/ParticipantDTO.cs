namespace TrainingAPI.DTOs
{
    public class ParticipantDTO
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Role { get; set; } = "Member";
        public DateTime JoinedAtUTC { get; set; }
        public long? LastDeliveredMessageId { get; set; }
        public long? LastReadMessageId { get; set; }
    }
}