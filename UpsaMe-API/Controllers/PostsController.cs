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
        // GET FEED (Home)
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
        // CREATE HELPER POST
        // Campos: Título, Materia, Capacity, MaxCapacity, CalendlyUrl, Content
        // ============================================
        [HttpPost("helper")]
        [Authorize]
        [ProducesResponseType(typeof(Post), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateHelper([FromBody] CreateHelperPostDto dto)
        {
            if (dto == null) return BadRequest("Body requerido.");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null)
                return Unauthorized("Token inválido: no se encontró el ID de usuario.");

            var userId = Guid.Parse(userIdClaim.Value);

            try
            {
                var created = await _service.CreateHelperAsync(userId, dto);
                return CreatedAtAction(nameof(GetFeed), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ============================================
        // CREATE STUDENT POST
        // Campos: Título, Materia, Contenido
        // ============================================
        [HttpPost("student")]
        [Authorize]
        [ProducesResponseType(typeof(Post), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateStudent([FromBody] CreateStudentPostDto dto)
        {
            if (dto == null) return BadRequest("Body requerido.");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null)
                return Unauthorized("Token inválido: no se encontró el ID de usuario.");

            var userId = Guid.Parse(userIdClaim.Value);

            try
            {
                var created = await _service.CreateStudentAsync(userId, dto);
                return CreatedAtAction(nameof(GetFeed), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ============================================
        // CREATE COMMENT POST
        // Campos: Título, Contenido
        // ============================================
        [HttpPost("comment")]
        [Authorize]
        [ProducesResponseType(typeof(Post), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateComment([FromBody] CreateCommentPostDto dto)
        {
            if (dto == null) return BadRequest("Body requerido.");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null)
                return Unauthorized("Token inválido: no se encontró el ID de usuario.");

            var userId = Guid.Parse(userIdClaim.Value);

            try
            {
                var created = await _service.CreateCommentAsync(userId, dto);
                return CreatedAtAction(nameof(GetFeed), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ============================================
        // ADD REPLY
        // ============================================
        [HttpPost("{postId:guid}/replies")]
        [Authorize]
        [ProducesResponseType(typeof(PostReply), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddReply(Guid postId, [FromBody] CreateReplyDto dto)
        {
            if (dto is null) return BadRequest("Body requerido.");
            if (string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest("El contenido no puede estar vacío.");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null)
                return Unauthorized("Token inválido: no se encontró el ID de usuario.");

            var userId = Guid.Parse(userIdClaim.Value);

            var reply = new PostReply
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                UserId = userId,
                Content = dto.Content.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            };

            var created = await _service.AddReplyAsync(postId, reply);
            if (created == null)
                return NotFound(new { message = "Post no encontrado o eliminado." });

            return Ok(new
            {
                created.Id,
                created.Content,
                created.CreatedAtUtc,
                created.UserId
            });
        }

        // ============================================
        // SEARCH POSTS BY SUBJECT
        // ============================================
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

        // ============================================
        // UPDATE POST (solo dueño)
        // ============================================
        [HttpPut("{postId:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(Post), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid postId, [FromBody] UpdatePostDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null) return Unauthorized();
            var userId = Guid.Parse(userIdClaim.Value);

            var updated = await _service.UpdateAsync(postId, userId, dto);
            if (updated == null) return NotFound("Post no encontrado o no autorizado.");
            return Ok(updated);
        }

        // ============================================
        // DELETE POST (solo dueño, soft delete)
        // ============================================
        [HttpDelete("{postId:guid}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid postId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null) return Unauthorized();
            var userId = Guid.Parse(userIdClaim.Value);

            var deleted = await _service.DeleteAsync(postId, userId);
            if (!deleted) return NotFound("Post no encontrado o no autorizado.");
            return NoContent();
        }

        // ============================================
        // GET MY POSTS (solo dueño)
        // ============================================
        [HttpGet("mine")]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<Post>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyPosts()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null) return Unauthorized();
            var userId = Guid.Parse(userIdClaim.Value);

            var posts = await _service.GetByUserAsync(userId);
            return Ok(posts);
        }
    }
}  