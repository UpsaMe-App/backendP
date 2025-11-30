using System;

namespace UpsaMe_API.DTOs.Posts
{
    // DTO que usa el frontend para enviar una reply
    // En el backend solo usamos Content (y opcionalmente podrías usar otros después)
    public class CreateReplyDto
    {
        public Guid UserId { get; set; }          // lo ignoras, porque sacas el user del token
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }   // lo puedes ignorar y usar DateTime.UtcNow

        // Estos campos los puede mandar el front, pero el backend no los necesita para crear
        public Guid Id { get; set; }
        public string Author { get; set; } = string.Empty;
        public string? AvatarId { get; set; }
        public string? ProfilePhotoUrl { get; set; }
        public string? ImageUrl { get; set; }
    }
}