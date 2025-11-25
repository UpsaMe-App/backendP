namespace UpsaMe_API.DTOs.Posts
{
    public class MyReplyDto
    {
        // Info de la reply
        public Guid ReplyId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }

        // Autor de la reply (si otro usuario entra a tu perfil)
        public Guid ReplyAuthorId { get; set; }
        public string ReplyAuthorFullName { get; set; } = string.Empty;
        public string? ReplyAuthorAvatarId { get; set; }
        public string? ReplyAuthorProfilePhotoUrl { get; set; }

        // Info del post original
        public Guid PostId { get; set; }
        public string PostTitle { get; set; } = string.Empty;
        public string? PostContentPreview { get; set; }

        // Autor del post original
        public Guid PostAuthorId { get; set; }
        public string PostAuthorFullName { get; set; } = string.Empty;
        public string? PostAuthorAvatarId { get; set; }
        public string? PostAuthorProfilePhotoUrl { get; set; }

        // Opcional
        public Guid? SubjectId { get; set; }
        public string? SubjectName { get; set; }
    }
}