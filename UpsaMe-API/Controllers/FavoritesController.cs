using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UpsaMe_API.Data;
using UpsaMe_API.DTOs.User;
using UpsaMe_API.Models;

namespace UpsaMe_API.Controllers
{
    [ApiController]
    [Route("favorites")]
    [Authorize] // Solo usuarios logueados
    public class FavoritesController : ControllerBase
    {
        private readonly UpsaMeDbContext _context;

        public FavoritesController(UpsaMeDbContext context)
        {
            _context = context;
        }

        // Helper para obtener el Id del usuario logueado
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                throw new InvalidOperationException("No se encontró el Id de usuario en el token.");

            return Guid.Parse(userIdClaim);
        }

        /// POST /favorites/{userId}
        [HttpPost("{userId:guid}")]
        public async Task<IActionResult> AddFavorite(Guid userId)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == userId)
            {
                return BadRequest("No puedes agregarte a ti mismo como favorito.");
            }

            var favoriteUserExists = await _context.Users
                .AnyAsync(u => u.Id == userId);

            if (!favoriteUserExists)
            {
                return NotFound("El usuario que intentas agregar a favoritos no existe.");
            }

            var alreadyFavorite = await _context.UserFavorites
                .AnyAsync(uf => uf.UserId == currentUserId && uf.FavoriteUserId == userId);

            if (alreadyFavorite)
            {
                return NoContent();
            }

            var favorite = new UserFavorite
            {
                UserId = currentUserId,
                FavoriteUserId = userId,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.UserFavorites.Add(favorite);
            await _context.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created);
        }

        /// DELETE /favorites/{userId}
        [HttpDelete("{userId:guid}")]
        public async Task<IActionResult> RemoveFavorite(Guid userId)
        {
            var currentUserId = GetCurrentUserId();

            var favorite = await _context.UserFavorites
                .FirstOrDefaultAsync(uf => uf.UserId == currentUserId && uf.FavoriteUserId == userId);

            if (favorite == null)
            {
                return NoContent();
            }

            _context.UserFavorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// GET /favorites
        [HttpGet]
        public async Task<ActionResult<List<FavoriteUserDto>>> GetFavorites()
        {
            var currentUserId = GetCurrentUserId();

            var favorites = await _context.UserFavorites
                .AsNoTracking()
                .Where(uf => uf.UserId == currentUserId)
                .Include(uf => uf.FavoriteUser)
                    .ThenInclude(u => u.Career)
                .OrderByDescending(uf => uf.CreatedAtUtc)
                .ToListAsync();

            var result = favorites.Select(uf => new FavoriteUserDto
            {
                Id = uf.FavoriteUser.Id,
                FullName = $"{uf.FavoriteUser.FirstName} {uf.FavoriteUser.LastName}",
                AvatarId = uf.FavoriteUser.AvatarId,
                ProfilePhotoUrl = uf.FavoriteUser.ProfilePhotoUrl,
                Career = uf.FavoriteUser.Career?.Name
            }).ToList();

            return Ok(result);
        }

        /// GET /favorites/is-favorite/{userId}
        /// Devuelve true/false si el usuario del perfil es favorito del usuario logueado
        [HttpGet("is-favorite/{userId:guid}")]
        public async Task<ActionResult<bool>> IsFavorite(Guid userId)
        {
            var currentUserId = GetCurrentUserId();

            // Opcional: nunca consideres favorito a uno mismo
            if (currentUserId == userId)
            {
                return Ok(false);
            }

            var exists = await _context.UserFavorites
                .AsNoTracking()
                .AnyAsync(uf => uf.UserId == currentUserId && uf.FavoriteUserId == userId);

            return Ok(exists);
        }
        /// GET /favorites/{userId}
        /// Público: permite ver los favoritos de cualquier usuario
        [HttpGet("{userId:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFavoritesByUser(Guid userId)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
                return NotFound("Usuario no encontrado.");

            var favorites = await _context.UserFavorites
                .AsNoTracking()
                .Where(uf => uf.UserId == userId)
                .Include(uf => uf.FavoriteUser)!.ThenInclude(u => u.Career)
                .OrderByDescending(uf => uf.CreatedAtUtc)
                .ToListAsync();

            // Para cada favorito calcular cuántas personas lo tienen como favorito
            var result = favorites.Select(uf => new 
            {
                Id = uf.FavoriteUser.Id,
                FullName = $"{uf.FavoriteUser.FirstName} {uf.FavoriteUser.LastName}",
                AvatarId = uf.FavoriteUser.AvatarId,
                ProfilePhotoUrl = uf.FavoriteUser.ProfilePhotoUrl,
                Career = uf.FavoriteUser.Career?.Name,
                FavoriteCount = _context.UserFavorites.Count(f => f.FavoriteUserId == uf.FavoriteUserId)
            });

            return Ok(result);
        }

    }
}
