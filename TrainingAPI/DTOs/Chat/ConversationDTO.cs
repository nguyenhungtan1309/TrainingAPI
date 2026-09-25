namespace TrainingAPI.DTOs
{
    public class ConversationDTO
    {
        public int ViewerId { get; set; }
        public long ThreadId { get; set; }
        public bool IsGroup { get; set; }
        public string ThreadName { get; set; } = string.Empty;
        public string? ThreadAvatarUrl { get; set; }
        public long? LastMessageId { get; set; }
        public string? LastMessageType { get; set; }
        public string? LastMessageContent { get; set; }
        public int? LastSenderId { get; set; }
        public DateTime? LastMessageTimeUTC { get; set; }
        public long? LastDeliveredMessageId { get; set; }
        public long? LastReadMessageId { get; set; }
        public bool IsMuted { get; set; }
        public int UnreadCount { get; set; }
    }
}