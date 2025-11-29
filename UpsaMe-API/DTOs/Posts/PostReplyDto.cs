namespace UpsaMe_API.DTOs.Posts
{
    public class CreateReplyDto
    {
        public Guid UserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }

        public Guid Id { get; set; }
        public string Author { get; set; } = string.Empty;
        public string? AvatarId { get; set; }
        public string? ProfilePhotoUrl { get; set; }
        public string? ImageUrl { get; set; }
    }
}