using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TrainingAPI.Models.Users;

namespace TrainingAPI.Models.Chat
{
    [Table("ThreadParticipant")]
    public class ThreadParticipant
    {
        public long ThreadId { get; set; }

        public int UserId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "Member"; // "Admin", "Deputy", "Member"

        public long? LastDeliveredMessageId { get; set; }

        public long? LastReadMessageId { get; set; }

        public bool IsMuted { get; set; } = false;

        public DateTime? MutedUntilUTC { get; set; }

        public DateTime JoinedAtUTC { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(ThreadId))]
        public virtual ChatThread? Thread { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual AppUser? User { get; set; }
    }
}