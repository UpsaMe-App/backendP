using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UpsaMe_API.DTOs.User;
using UpsaMe_API.Helpers;
using UpsaMe_API.Services;

namespace UpsaMe_API.Controllers
{
    [ApiController]
    [Route("users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;

        public UsersController(UserService userService)
        {
            _userService = userService;
        }

        /// <summary>Perfil del usuario autenticado.</summary>
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null)
                return Unauthorized("Token inválido: no se encontró el ID de usuario.");

            var userId = Guid.Parse(userIdClaim.Value);
            var user = await _userService.GetProfileAsync(userId);

            if (user == null)
                return NotFound("Usuario no encontrado.");

            return Ok(user);
        }

        /// <summary>Perfil público por Id.</summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPublicProfile(Guid id)
        {
            var user = await _userService.GetProfileAsync(id);
            if (user == null)
                return NotFound("Usuario no encontrado.");

            return Ok(user);
        }

        /// 🔥 NUEVO ENDPOINT: Estado de conexión de un usuario
        [HttpGet("{id:guid}/online-status")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOnlineStatus(
            Guid id,
            [FromServices] IConnectionService connectionService)
        {
            var online = await connectionService.IsOnlineAsync(id);
            return Ok(new { userId = id, online });
        }

        // AVATARS
        [HttpGet("avatars/options")]
        [AllowAnonymous]
        public IActionResult GetAvatarOptions()
        {
            return Ok(AvatarCatalog.GetAll());
        }

        /// <summary>Actualizar perfil del usuario autenticado.</summary>
        [HttpPut("me")]
        [RequestSizeLimit(10_000_000)]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null)
                return Unauthorized("Token inválido: no se encontró el ID de usuario.");

            var userId = Guid.Parse(userIdClaim.Value);

            await _userService.UpdateProfileAsync(userId, dto);

            var updatedProfile = await _userService.GetProfileAsync(userId);
            if (updatedProfile == null)
                return NotFound("No se pudo recuperar el perfil actualizado.");

            return Ok(updatedProfile);
        }
    }
}
