using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UpsaMe_API.Data;

namespace UpsaMe_API.Controllers
{
    [ApiController]
    [Route("public-users")] // ✅ YA NO CHOCA CON UsersController
    public class PublicUsersController : ControllerBase
    {
        private readonly UpsaMeDbContext _context;

        public PublicUsersController(UpsaMeDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene el perfil público de un usuario por su Id.
        /// </summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Career)
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    u.Id,
                    FullName = u.FirstName + " " + u.LastName,
                    u.Email,
                    u.Phone,            // ✅ AGREGADO
                    Career = u.Career != null ? u.Career.Name : null,
                    u.Semester,
                    u.ProfilePhotoUrl
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound("Usuario no encontrado.");

            return Ok(user);
        }
    }
}