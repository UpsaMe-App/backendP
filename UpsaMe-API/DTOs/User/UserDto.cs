namespace UpsaMe_API.DTOs.User
{
    public class UserDto
    {
        public Guid Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public Guid? CareerId { get; set; }
        public string? Career { get; set; }

        public int? Semester { get; set; }

        public string? ProfilePhotoUrl { get; set; }
        public string? Phone { get; set; }
        public string? CalendlyUrl { get; set; } 
    }
}