using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UpsaMe_API.DTOs.Posts;
using UpsaMe_API.Models;
using UpsaMe_API.Services;
using Microsoft.Extensions.Options;
using UpsaMe_API.Config;
using UpsaMe_API.Helpers;


namespace UpsaMe_API.Controllers
{
    [ApiController]
    [Route("posts")]
    public class PostsController : ControllerBase
    {
        private readonly PostService _service;
        private readonly BlobStorageHelper _blobHelper;
        private readonly AzureSettings _azureSettings;

        public PostsController(PostService service, BlobStorageHelper blobHelper,
            IOptions<AzureSettings> azureOptions)
        {
            _service = service;
            _blobHelper = blobHelper;
            _azureSettings = azureOptions.Value;
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
// Campos: Título, Materia, Contenido + (opcional) imagen PNG
// ============================================
        [HttpPost("student")]
        [Authorize]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(Post), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateStudent(
            [FromForm] CreateStudentPostDto dto,
            IFormFile? image)
        {
            if (dto == null) return BadRequest("Body requerido.");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null)
                return Unauthorized("Token inválido: no se encontró el ID de usuario.");

            var userId = Guid.Parse(userIdClaim.Value);

            try
            {
                // 1) Si hay imagen, subirla PRIMERO
                string? imageUrl = null;
                if (image != null)
                {
                    var container = _azureSettings.PostImagesContainer;
                    imageUrl = await _blobHelper.UploadPngAsync(
                        image,
                        container,
                        $"post_{Guid.NewGuid()}");  // Generar ID temporal
                }

                // 2) Crear post YA CON la imageUrl
                var created = await _service.CreateStudentAsync(userId, dto, imageUrl);

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
// ADD REPLY (opcional: una imagen PNG)
// ============================================
        [HttpPost("{postId:guid}/replies")]
        [Authorize]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(PostReply), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddReply(
            Guid postId,
            [FromForm] CreateReplyDto dto,
            IFormFile? image)
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

            // 👇 Solo si se envía imagen
            if (image != null)
            {
                var container = _azureSettings.ReplyImagesContainer; // ej: "reply-images"
                var imageUrl = await _blobHelper.UploadPngAsync(
                    image,
                    container,
                    $"reply_{reply.Id}");

                reply.ImageUrl = imageUrl;
            }

            var created = await _service.AddReplyAsync(postId, reply);
            if (created == null)
                return NotFound(new { message = "Post no encontrado o eliminado." });

            return Ok(new
            {
                created.Id,
                created.Content,
                created.CreatedAtUtc,
                created.UserId,
                created.ImageUrl  // 👈 para que el front ya reciba la URL
            });
        }

        // ============================================
// GET REPLIES DE UN POST
// ============================================
        [HttpGet("{postId:guid}/replies")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<PostReplyDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReplies(Guid postId)
        {
            var replies = await _service.GetRepliesForPostAsync(postId);
            return Ok(replies);
        }
        // ============================================
        // GET MY REPLIES (respuestas que YO hice)
        // ============================================
        [HttpGet("replies/mine")]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<MyReplyDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyReplies()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null) return Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);

            var replies = await _service.GetRepliesByUserAsync(userId);
            return Ok(replies);
        }
        // ============================================
// GET USER REPLIES (público por userId)
// ============================================
        [HttpGet("replies/user/{userId:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<MyReplyDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRepliesByUser(Guid userId)
        {
            var replies = await _service.GetRepliesByUserPublicAsync(userId);
            return Ok(replies);
        }
        
        // ============================================
// DELETE REPLY (solo dueño o dueño del post)
// ============================================
        [HttpDelete("{postId:guid}/replies/{replyId:guid}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteReply(Guid postId, Guid replyId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null)
                return Unauthorized("Token inválido: no se encontró el ID de usuario.");

            var userId = Guid.Parse(userIdClaim.Value);

            var ok = await _service.DeleteReplyAsync(postId, replyId, userId);
            if (!ok)
                return NotFound("Reply no encontrada o no autorizado para borrarla.");

            return NoContent();
        }



        // ============================================
        // SEARCH POSTS BY SUBJECT
        // ============================================
        [HttpGet("search-by-subject")]
        public async Task<ActionResult<IEnumerable<PostListItemDto>>> SearchBySubject(
            [FromQuery] string q,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            var posts = await _service.SearchBySubjectAsync(q, page, pageSize, ct);

            // Convertimos a DTO aquí
            var result = posts.Select(p => new PostListItemDto
            {
                Id = p.Id,
                Role = (int)p.Role,
                Status = (int)p.Status,
                Title = p.Title,
                Content = p.Content,
                Capacity = p.Capacity,
                MaxCapacity = p.MaxCapacity,
                CalendlyUrl = p.CalendlyUrl,
                CapacityUsed = p.CapacityUsed,
                CreatedAtUtc = p.CreatedAtUtc,

                AuthorId = p.UserId,
                Author = p.User != null
                    ? p.User.FirstName + " " + p.User.LastName
                    : string.Empty,

                SubjectId = p.SubjectId,
                Subject = p.Subject != null ? p.Subject.Name : null,

                ImageUrl = p.ImageUrl,

                // Avatar + foto desde el usuario
                AuthorAvatarId = p.User?.AvatarId,
                AuthorProfilePhotoUrl = p.User?.ProfilePhotoUrl
            });

            return Ok(result);
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