using System;

namespace UpsaMe_API.DTOs.Posts
{
    public class PostReplyDto
    {
        public Guid Id { get; set; }
        public Guid PostId { get; set; }

        // 🔹 Este es el ID del autor de la reply (antes lo llamabas AuthorId en el select)
        public Guid AuthorId { get; set; }
        public string Author { get; set; } = string.Empty;

        public string? AvatarId { get; set; }
        public string? ProfilePhotoUrl { get; set; }

        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}