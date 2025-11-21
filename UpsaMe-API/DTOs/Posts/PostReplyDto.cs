namespace UpsaMe_API.DTOs.Posts
{
    public class CreateReplyDto
    {
        public Guid UserId { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}