using System;

namespace UpsaMe_API.DTOs.Posts
{
    public class PostListItemDto
    {
        public Guid Id { get; set; }
        public int Role { get; set; }
        public int Status { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        public int? Capacity { get; set; }
        public int? MaxCapacity { get; set; }
        public string? CalendlyUrl { get; set; }

        public int CapacityUsed { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        public Guid AuthorId { get; set; }
        public string Author { get; set; } = string.Empty;

        public Guid? SubjectId { get; set; }
        public string? Subject { get; set; }

        public string? ImageUrl { get; set; }

        // 🆕 avatar/foto del autor
        public string? AuthorAvatarId { get; set; }
        public string? AuthorProfilePhotoUrl { get; set; }
    }
}