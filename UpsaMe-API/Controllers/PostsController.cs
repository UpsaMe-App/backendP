using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UpsaMe_API.DTOs.Posts;
using UpsaMe_API.Models;
using UpsaMe_API.Services;

namespace UpsaMe_API.Controllers
{
    [ApiController]
    [Route("posts")]
    public class PostsController : ControllerBase
    {
        private readonly PostService _service;

        public PostsController(PostService service)
        {
            _service = service;
        }

        // ============================================
        // GET FEED
        // ============================================
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFeed(
            [FromQuery] PostRole? role,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var feed = await _service.GetFeedAsync(role, page, pageSize);
            return Ok(feed);
        }

        // ============================================
        // CREATE POST
        // ============================================
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(Post), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create([FromBody] CreatePostDto dto)
        {
            if (dto is null) return BadRequest("Body requerido.");
            if (string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest("El contenido no puede estar vacío.");

            // 🔹 Validaciones por rol
            switch (dto.Role)
            {
                case PostRole.Helper:
                    if (!dto.SubjectId.HasValue)
                        return BadRequest("Materia (SubjectId) es obligatoria para rol Helper.");

                    if (!dto.Capacity.HasValue || dto.Capacity.Value <= 0)
                        return BadRequest("Capacidad máxima (Capacity) debe ser > 0 para rol Helper.");
                    break;

                case PostRole.Student:
                    if (!dto.SubjectId.HasValue)
                        return BadRequest("Materia (SubjectId) es obligatoria para rol Student.");

                    if (dto.Topics == null || dto.Topics.Length == 0)
                        return BadRequest("Debes especificar al menos un tema para rol Student.");

                    if (!dto.Capacity.HasValue || dto.Capacity.Value <= 0)
                        return BadRequest("Cantidad de personas (Capacity) debe ser > 0 para rol Student.");
                    break;

                case PostRole.Comment:
                    // comentario libre
                    break;

                default:
                    return BadRequest("Role inválido.");
            }

            // Obtener userId del token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null)
                return Unauthorized("Token inválido: no se encontró el ID de usuario.");

            var userId = Guid.Parse(userIdClaim.Value);

            // Crear post
            var post = new Post
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Role = dto.Role,
                Title = dto.Title,
                Content = dto.Content,
                SubjectId = dto.SubjectId,
                Capacity = dto.Capacity,
                CapacityUsed = 0,
                Status = PostStatus.Active,
                CreatedAtUtc = DateTime.UtcNow,

                TeacherName = dto.TeacherName,
                Topics = dto.Topics != null && dto.Topics.Length > 0
                    ? string.Join(", ", dto.Topics)
                    : null
            };

            var created = await _service.CreateAsync(post);
            return CreatedAtAction(nameof(GetFeed), new { id = created.Id }, created);
        }

        // ============================================
        // ADD REPLY
        // ============================================
        [HttpPost("{id}/replies")]
        [Authorize]
        [ProducesResponseType(typeof(PostReply), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddReply(Guid id, [FromBody] PostReplyDto dto)
        {
            if (dto is null) return BadRequest("Body requerido.");
            if (string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest("El contenido no puede estar vacío.");

            // Autor de la reply = usuario logueado
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null)
                return Unauthorized("Token inválido: no se encontró el ID de usuario.");

            var userId = Guid.Parse(userIdClaim.Value);

            var reply = new PostReply
            {
                Content = dto.Content,
                UserId = userId
            };

            var created = await _service.AddReplyAsync(id, reply);
            if (created == null)
                return NotFound("No se encontró la publicación.");

            return Ok(created);
        }

        // ============================================
        // SEARCH POSTS BY SUBJECT (la lupita)
        // ============================================
        /// <summary>Busca publicaciones por nombre de materia.</summary>
        [HttpGet("search-by-subject")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchBySubject(
            [FromQuery] string q,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var results = await _service.SearchPostsBySubjectAsync(q, page, pageSize);
            return Ok(results);
        }
    }
}
