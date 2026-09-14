using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TrainingAPI.Models.Chat;
using TrainingAPI.Models.Users;

namespace TrainingAPI.Models.Interactions
{
    [Table("MessageReaction")]
    public class MessageReaction
    {
        public long MessageId { get; set; }

        public int UserId { get; set; }

        [Required]
        [MaxLength(20)]
        public string ReactionType { get; set; } = string.Empty; // "like", "heart", "haha", "wow", "sad", "angry"

        public DateTime ReactedAtUTC { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(MessageId))]
        public virtual Message? Message { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual AppUser? User { get; set; }
    }
}