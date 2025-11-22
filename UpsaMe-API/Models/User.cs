using System.ComponentModel.DataAnnotations;

namespace UpsaMe_API.Models;

public class User
{
    [Key]
    public Guid Id { get; set; }

    [Required, EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    // 🔹 Nuevo: FK a Career
    public Guid? CareerId { get; set; }
    public Career? Career { get; set; }

    [Range(1, 10, ErrorMessage = "El semestre debe estar entre 1 y 12.")]
    public int? Semester { get; set; }

    [Phone]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [Url]
    public string? ProfilePhotoUrl { get; set; }

    [MaxLength(50)]
    public string? Timezone { get; set; } = "America/La_Paz";

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAtUtc { get; set; }
    [MaxLength(100)]
    public string? AvatarId { get; set; }
}