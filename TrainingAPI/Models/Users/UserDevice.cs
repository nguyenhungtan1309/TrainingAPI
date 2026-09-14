using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrainingAPI.Models.Users
{
    [Table("UserDevice")]
    public class UserDevice
    {
        [Key]
        public long Id { get; set; }

        public int UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string DeviceToken { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Platform { get; set; } = string.Empty; // "iOS", "Android", "Web"

        public DateTime UpdatedAtUTC { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public virtual AppUser? User { get; set; }
    }
}