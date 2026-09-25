using System.ComponentModel.DataAnnotations;

namespace TrainingAPI.DTOs
{
    public class CreateThreadRequestDTO
    {
        public bool IsGroup { get; set; } = false;

        [MaxLength(100)]
        public string? Title { get; set; }

        [Required]
        public List<int> ParticipantIds { get; set; } = new();
    }

    public class SendMessageRequestDTO
    {
        public long ThreadId { get; set; }

        [Required]
        [MaxLength(20)]
        public string MessageType { get; set; } = "Text";

        [MaxLength(4000)]
        public string Content { get; set; } = string.Empty;

        public long? ParentMessageId { get; set; }
        public long? ForwardedFromMessageId { get; set; }

        public List<SaveAttachmentRequestDTO>? Attachments { get; set; }
    }
    public class ManageParticipantRequestDTO
    {
        public long ThreadId { get; set; }
        public int TargetUserId { get; set; }

        [Required]
        public string ActionType { get; set; } = "ADD";
    }
    public class UpdateRoleRequestDTO
    {
        public int TargetUserId { get; set; }
        public string NewRole { get; set; } = string.Empty;
    }
    public class MuteThreadRequestDTO
    {
        public bool IsMuted { get; set; }
        public DateTime? MutedUntilUTC { get; set; }
    }
    public class ToggleReactionRequestDTO
    {
        public long MessageId { get; set; }

        [Required]
        [MaxLength(20)]
        public string ReactionType { get; set; } = "like";
    }

    public class EditMessageRequestDTO
    {
        public long MessageId { get; set; }

        [Required]
        [MaxLength(4000)]
        public string NewContent { get; set; } = string.Empty;
    }

    public class UpdateTitleRequestDTO
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(100)]
        public string NewTitle { get; set; } = string.Empty;
    }

    public class SaveAttachmentRequestDTO
    {
        public string FileUrl { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public int FileSize { get; set; }
    }
}