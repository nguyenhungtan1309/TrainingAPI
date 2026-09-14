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

        [Required]
        [MaxLength(4000)]
        public string Content { get; set; } = string.Empty;

        public long? ParentMessageId { get; set; }
        public long? ForwardedFromMessageId { get; set; }
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
        public long ThreadId { get; set; }
        public int TargetUserId { get; set; }

        [Required]
        [MaxLength(20)]
        public string NewRole { get; set; } = "Member";
    }
    public class MuteThreadRequestDTO
    {
        public long ThreadId { get; set; }
        public bool IsMuted { get; set; }
        public DateTime? MutedUntilUTC { get; set; }
    }
    public class RegisterDeviceRequestDTO
    {
        [Required]
        [MaxLength(255)]
        public string DeviceToken { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Platform { get; set; } = "Web";
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
}