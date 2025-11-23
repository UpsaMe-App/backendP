using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UpsaMe_API.Data;
using UpsaMe_API.Dto.PublicUsers;
using UpsaMe_API.Models;

namespace UpsaMe_API.Controllers
{
    [ApiController]
    [Route("public-users")] // ✅ Endpoint público
    public class PublicUsersController : ControllerBase
    {
        private readonly UpsaMeDbContext _context;

        public PublicUsersController(UpsaMeDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene el perfil público de un usuario por su Id, incluyendo sus últimos posts.
        /// </summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PublicUserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            // 1) Traemos al usuario con su carrera
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Career)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound("Usuario no encontrado.");
            }

            // 2) Traemos los posts de ese usuario (por ejemplo, los últimos 20)
            var posts = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Subject)
                .Where(p => p.UserId == id)
                .OrderByDescending(p => p.CreatedAtUtc)
                .Take(20)
                .ToListAsync();

            // 3) Mapeamos a DTO
            var dto = new PublicUserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = $"{user.FirstName} {user.LastName}",
                Phone = user.Phone,
                CareerId = user.CareerId,
                Career = user.Career?.Name,
                Semester = user.Semester,
                ProfilePhotoUrl = user.ProfilePhotoUrl,
                AvatarId = user.AvatarId,
                CalendlyUrl = user.CalendlyUrl,
                Posts = posts.Select(p => new PublicUserPostDto
                {
                    Id = p.Id,
                    Role = p.Role,
                    Title = p.Title,
                    ContentPreview = p.Content.Length > 250
                        ? p.Content.Substring(0, 250) + "..."
                        : p.Content,
                    SubjectId = p.SubjectId,
                    SubjectName = p.Subject?.Name,
                    CreatedAtUtc = p.CreatedAtUtc,
                    Status = p.Status,
                    CalendlyUrl = p.CalendlyUrl
                }).ToList()
            };

            return Ok(dto);
        }
    }
}
