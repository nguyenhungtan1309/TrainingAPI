using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrainingAPI.Models
{
    [Table("ChatMessage")]
    public class ChatMessage
    {
        [Key]
        public long Id { get; set; }

        public int SenderId { get; set; }

        public int ReceiverId { get; set; }

        [Required]
        public string MessageContent { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        [ForeignKey(nameof(SenderId))]
        public AppUser? Sender { get; set; }

        [ForeignKey(nameof(ReceiverId))]
        public AppUser? Receiver { get; set; }
    }
}