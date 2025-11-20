namespace UpsaMe_API.DTOs.Posts
{
    // 🔹 Para crear post de AYUDANTE
    public class CreateHelperPostDto
    {
        // Requeridos
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        // Materia (obligatoria)
        public Guid SubjectId { get; set; }

        // Capacidad actual (cupos abiertos) y máxima
        public int Capacity { get; set; }
        public int MaxCapacity { get; set; }

        // URL de disponibilidad (Calendly)
        public string CalendlyUrl { get; set; } = string.Empty;
    }

    // 🔹 Para crear post de ESTUDIANTE
    public class CreateStudentPostDto
    {
        // Requeridos
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        // Materia (obligatoria)
        public Guid SubjectId { get; set; }
    }

    // 🔹 Para crear COMENTARIO
    public class CreateCommentPostDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}