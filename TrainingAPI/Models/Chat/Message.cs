using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TrainingAPI.Models.Interactions;
using TrainingAPI.Models.Users;

namespace TrainingAPI.Models.Chat
{
    [Table("Message")]
    public class Message
    {
        [Key]
        public long Id { get; set; }

        public long ThreadId { get; set; }

        public int SenderId { get; set; }

        [Required]
        [MaxLength(20)]
        public string MessageType { get; set; } = "Text"; // "Text", "Image", "File", "System"

        [Required]
        [MaxLength(4000)]
        public string Content { get; set; } = string.Empty;

        public long? ParentMessageId { get; set; }

        public long? ForwardedFromMessageId { get; set; }

        public bool IsEdited { get; set; } = false;

        public DateTime? EditedAtUTC { get; set; }

        public bool IsRevoked { get; set; } = false;

        public DateTime SentAtUTC { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(ThreadId))]
        public virtual ChatThread? Thread { get; set; }

        [ForeignKey(nameof(SenderId))]
        public virtual AppUser? Sender { get; set; }

        [ForeignKey(nameof(ParentMessageId))]
        public virtual Message? ParentMessage { get; set; }

        [ForeignKey(nameof(ForwardedFromMessageId))]
        public virtual Message? ForwardedFromMessage { get; set; }

        public virtual ICollection<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
        public virtual ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
    }
}