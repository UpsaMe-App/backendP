using System.ComponentModel.DataAnnotations;

namespace UpsaMe_API.DTOs.Posts
{
    // 🧙‍♂️ Crear post de AYUDANTE
    public class CreateHelperPostDto
    {
        [Required] [MaxLength(200)] public string Title { get; set; } = string.Empty;

        [Required] [MaxLength(3000)] public string Content { get; set; } = string.Empty;

        [Required] public Guid SubjectId { get; set; }

        // Capacidad actual de cupos disponibles
        [Range(1, int.MaxValue, ErrorMessage = "Capacity debe ser >= 1.")]
        public int Capacity { get; set; }

        // Capacidad máxima de personas
        [Range(1, int.MaxValue, ErrorMessage = "MaxCapacity debe ser >= 1.")]
        public int MaxCapacity { get; set; }

        // Disponibilidad de horario (Calendly)
        [MaxLength(500)]
        public string? CalendlyUrl { get; set; }
    }

    // 🎓 Crear post de ESTUDIANTE
    public class CreateStudentPostDto
    {
        [Required] [MaxLength(200)] public string Title { get; set; } = string.Empty;

        [Required] [MaxLength(3000)] public string Content { get; set; } = string.Empty;

        [Required] public Guid SubjectId { get; set; }
    }

    // 💬 Crear post de COMENTARIO
    public class CreateCommentPostDto
    {
        [Required] [MaxLength(200)] public string Title { get; set; } = string.Empty;

        [Required] [MaxLength(3000)] public string Content { get; set; } = string.Empty;
    }

} 