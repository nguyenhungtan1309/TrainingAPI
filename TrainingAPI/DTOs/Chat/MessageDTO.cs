namespace TrainingAPI.DTOs
{
    public class MessageAttachmentDTO
    {
        public long Id { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public int FileSize { get; set; }
    }

    public class MessageReactionDTO
    {
        public string ReactionType { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string ReactedByName { get; set; } = string.Empty;
    }

    public class MessageResponseDTO
    {
        public long MessageId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string? SenderAvatarUrl { get; set; }
        public string MessageType { get; set; } = "Text";
        public string Content { get; set; } = string.Empty;
        public bool IsRevoked { get; set; }
        public bool IsEdited { get; set; }
        public DateTime SentAtUTC { get; set; }

        public long? ParentMessageId { get; set; }
        public string? ParentSenderName { get; set; }
        public string? ParentContent { get; set; }

        public bool IsPinned { get; set; }

        public List<MessageAttachmentDTO> Attachments { get; set; } = new();
        public List<MessageReactionDTO> Reactions { get; set; } = new();
    }
}