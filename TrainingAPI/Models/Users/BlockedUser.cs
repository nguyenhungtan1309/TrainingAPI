using System.ComponentModel.DataAnnotations.Schema;

namespace TrainingAPI.Models.Users
{
    [Table("BlockedUser")]
    public class BlockedUser
    {
        public int UserId { get; set; }

        public int BlockedUserId { get; set; }

        public DateTime CreatedAtUTC { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public virtual AppUser? User { get; set; }

        [ForeignKey(nameof(BlockedUserId))]
        public virtual AppUser? BlockedAppUser { get; set; }
    }
}