using System.ComponentModel.DataAnnotations.Schema;
using TrainingAPI.Models.Chat;
using TrainingAPI.Models.Users;

namespace TrainingAPI.Models.Interactions
{
    [Table("HiddenThread")]
    public class HiddenThread
    {
        public int UserId { get; set; }

        public long ThreadId { get; set; }

        public DateTime HiddenAtUTC { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public virtual AppUser? User { get; set; }

        [ForeignKey(nameof(ThreadId))]
        public virtual ChatThread? Thread { get; set; }
    }
}