using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UpsaMe_API.Models
{
    public class PostReply
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("Post")]
        public Guid PostId { get; set; }
        public Post? Post { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }
        public User? User { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;
        [MaxLength(500)]
        public string? ImageUrl { get; set; }   // 👈 NUEVO

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}