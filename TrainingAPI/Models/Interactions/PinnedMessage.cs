using System.ComponentModel.DataAnnotations.Schema;
using TrainingAPI.Models.Chat;
using TrainingAPI.Models.Users;

namespace TrainingAPI.Models.Interactions
{
    [Table("PinnedMessage")]
    public class PinnedMessage
    {
        public long ThreadId { get; set; }

        public long MessageId { get; set; }

        public int PinnedBy { get; set; }

        public DateTime PinnedAtUTC { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(ThreadId))]
        public virtual ChatThread? Thread { get; set; }

        [ForeignKey(nameof(MessageId))]
        public virtual Message? Message { get; set; }

        [ForeignKey(nameof(PinnedBy))]
        public virtual AppUser? Pinner { get; set; }
    }
}