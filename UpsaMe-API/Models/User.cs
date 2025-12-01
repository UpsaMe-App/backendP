using System.ComponentModel.DataAnnotations;

namespace UpsaMe_API.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        // =========================
        // IDENTIDAD Y NOMBRE
        // =========================
        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        // =========================
        // CARRERA Y SEMESTRE
        // =========================
        public Guid? CareerId { get; set; }
        public Career? Career { get; set; }

        [Range(1, 12, ErrorMessage = "El semestre debe estar entre 1 y 12.")]
        public int? Semester { get; set; }

        // =========================
        // CONTACTO
        // =========================
        [Phone, MaxLength(20)]
        public string? Phone { get; set; }

        // =========================
        // PERFIL — FOTO Y AVATAR
        // =========================
        [Url]
        public string? ProfilePhotoUrl { get; set; }

        // ID del avatar predefinido (no archivo)
        [MaxLength(100)]
        public string? AvatarId { get; set; }

        // Calendly opcional
        public string? CalendlyUrl { get; set; }

        // Zona horaria por defecto
        [MaxLength(50)]
        public string? Timezone { get; set; } = "America/La_Paz";

        // =========================
        // CREDENCIALES Y TOKENS
        // =========================
        [Required, MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiresAtUtc { get; set; }

        // =========================
        // RELACIONES
        // =========================
        public ICollection<UserFavorite> Favorites { get; set; } = new List<UserFavorite>();

        // (Si tienes Posts, Replies, etc, se agregan aquí)
    }
}
