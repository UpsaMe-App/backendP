using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UpsaMe_API.Models
{
    public enum PostRole { Helper = 1, Student = 2, Comment = 3 }
    public enum PostStatus { Active, Closed, Deleted }

    public class Post
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }
        public User? User { get; set; }

        // Rol del post (define qué campos son válidos/obligatorios en la lógica de servicio)
        [Required]
        public PostRole Role { get; set; }

        // Requerido para todos los roles: Helper, Student y Comment
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        // Requerido para todos los roles
        [Required]
        [MaxLength(3000)]
        public string Content { get; set; } = string.Empty;

        // Materia:
        // - Helper y Student: requerida (se valida en servicio)
        // - Comment: debe ser null
        [ForeignKey(nameof(Subject))]
        public Guid? SubjectId { get; set; }
        public Subject? Subject { get; set; }

        // Capacidad:
        // - Helper: capacidad actual (cupos disponibles)
        // - Student: NO se usa (debe ser null)
        public int? Capacity { get; set; }

        // Capacidad máxima de personas (solo Helper)
        public int? MaxCapacity { get; set; }

        // Disponibilidad de horario via Calendly (solo Helper)
        [MaxLength(500)]
        public string? CalendlyUrl { get; set; }

        // Estado y métricas
        public int CapacityUsed { get; set; } = 0;
        public PostStatus Status { get; set; } = PostStatus.Active;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        // Respuestas/comentarios al post
        public ICollection<PostReply>? Replies { get; set; }
        
    }
}      