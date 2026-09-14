namespace TrainingAPI.Models
{
    public class ConversationItem
    {
        public long LastMessageId { get; set; }
        public int UserOneId { get; set; }
        public string UserOneName { get; set; } = string.Empty;
        public int UserTwoId { get; set; }
        public string UserTwoName { get; set; } = string.Empty;
        public int LastSenderId { get; set; }
        public string LastMessageContent { get; set; } = string.Empty;
        public DateTime LastSentAt { get; set; }
    }
}