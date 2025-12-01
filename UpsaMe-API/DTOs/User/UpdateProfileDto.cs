using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace UpsaMe_API.DTOs.User
{
    public class UpdateProfileDto
    {
        [MaxLength(50, ErrorMessage = "El nombre no puede tener más de 50 caracteres.")]
        public string? FirstName { get; set; }

        [MaxLength(50, ErrorMessage = "El apellido no puede tener más de 50 caracteres.")]
        public string? LastName { get; set; }

        [Phone(ErrorMessage = "El número de teléfono no tiene un formato válido.")]
        public string? Phone { get; set; }

        [Range(1, 12, ErrorMessage = "El semestre debe estar entre 1 y 12.")]
        public int? Semester { get; set; }

        public Guid? CareerId { get; set; }

        /// Imagen subida por el usuario
        public IFormFile? ProfilePhoto { get; set; }

        /// Avatar seleccionado desde catálogo
        public string? AvatarId { get; set; }

        [Url(ErrorMessage = "El Calendly URL no es válido.")]
        public string? CalendlyUrl { get; set; }
    }
}