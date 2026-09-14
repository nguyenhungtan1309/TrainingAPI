using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrainingAPI.Models.Chat
{
    [Table("MessageAttachment")]
    public class MessageAttachment
    {
        [Key]
        public long Id { get; set; }

        public long MessageId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string FileUrl { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string FileType { get; set; } = string.Empty;

        public int FileSize { get; set; }

        public DateTime UploadedAtUTC { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(MessageId))]
        public virtual Message? Message { get; set; }
    }
}