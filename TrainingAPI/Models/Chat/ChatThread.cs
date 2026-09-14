using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TrainingAPI.Models.Interactions;

namespace TrainingAPI.Models.Chat
{
    [Table("ChatThread")]
    public class ChatThread
    {
        [Key]
        public long Id { get; set; }

        public bool IsGroup { get; set; } = false;

        [MaxLength(100)]
        public string? Title { get; set; }

        [MaxLength(1000)]
        public string? AvatarUrl { get; set; }

        public long? LastMessageId { get; set; }

        public DateTime? LastMessageTimeUTC { get; set; }

        public DateTime CreatedAtUTC { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<ThreadParticipant> Participants { get; set; } = new List<ThreadParticipant>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
        public virtual ICollection<PinnedMessage> PinnedMessages { get; set; } = new List<PinnedMessage>();
    }
}