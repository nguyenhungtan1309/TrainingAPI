using System.ComponentModel.DataAnnotations.Schema;
using TrainingAPI.Models.Chat;
using TrainingAPI.Models.Users;

namespace TrainingAPI.Models.Interactions
{
    [Table("DeletedThreadMessage")]
    public class DeletedThreadMessage
    {
        public int UserId { get; set; }

        public long MessageId { get; set; }

        public DateTime DeletedAtUTC { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public virtual AppUser? User { get; set; }

        [ForeignKey(nameof(MessageId))]
        public virtual Message? Message { get; set; }
    }
}