using System.ComponentModel.DataAnnotations;

namespace UpsaMe_API.DTOs.Auth
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        public string LastName { get; set; } = string.Empty;
        
        public string? AvatarId { get; set; }
        
        // 🔹 Id de carrera
        public Guid? CareerId { get; set; }

        [Range(1, 12, ErrorMessage = "El semestre debe estar entre 1 y 12.")]
        public int? Semester { get; set; }

        // 🔹 Teléfono
        [Phone(ErrorMessage = "El número de teléfono no es válido.")]
        [Required(ErrorMessage = "El número de teléfono es obligatorio.")]
        public string Phone { get; set; } = string.Empty;
    }
}