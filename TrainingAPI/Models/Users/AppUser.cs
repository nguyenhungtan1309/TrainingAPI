using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrainingAPI.Models.Users
{
    [Table("AppUser")]
    public class AppUser
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? AvatarUrl { get; set; }

        [Required]
        [MaxLength(128)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(64)]
        public string PasswordSalt { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime? LastActiveAtUTC { get; set; }

        public DateTime CreatedAtUTC { get; set; } = DateTime.UtcNow;
    }
}