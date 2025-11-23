using UpsaMe_API.Models;

namespace UpsaMe_API.Dto.PublicUsers
{
    public class PublicUserPostDto
    {
        public Guid Id { get; set; }
        public PostRole Role { get; set; }

        public string Title { get; set; } = string.Empty;
        public string ContentPreview { get; set; } = string.Empty;

        public Guid? SubjectId { get; set; }
        public string? SubjectName { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public PostStatus Status { get; set; }

        public string? CalendlyUrl { get; set; }
    }

    public class PublicUserProfileDto
    {
        public Guid Id { get; set; }

        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        public Guid? CareerId { get; set; }
        public string? Career { get; set; }
        public int? Semester { get; set; }

        public string? ProfilePhotoUrl { get; set; }
        public string? Phone { get; set; }
        public string? AvatarId { get; set; }
        public string? CalendlyUrl { get; set; }

        // Lista de posts del usuario
        public List<PublicUserPostDto> Posts { get; set; } = new();
    }
}