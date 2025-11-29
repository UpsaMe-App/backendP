using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UpsaMe_API.DTOs.User;
using UpsaMe_API.Helpers;
using UpsaMe_API.Services;
using Microsoft.Extensions.Options;
using UpsaMe_API.Config;
using UpsaMe_API.Helpers;

namespace UpsaMe_API.Controllers
{
    [ApiController]
    [Route("users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly BlobStorageHelper _blobHelper;
        private readonly AzureSettings _azureSettings;

        public UsersController(UserService userService,BlobStorageHelper blobHelper,
            IOptions<AzureSettings> azureOptions)
        {
            _userService = userService;
            _blobHelper = blobHelper;
            _azureSettings = azureOptions.Value;
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
        /// <summary>Actualizar perfil del usuario autenticado.</summary>
        [HttpPut("me")]
        [RequestSizeLimit(10_000_000)]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateProfile(
            [FromForm] UpdateProfileDto dto,
            IFormFile? profilePhoto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null)
                return Unauthorized("Token inválido: no se encontró el ID de usuario.");

            var userId = Guid.Parse(userIdClaim.Value);

            // 1) Actualizar los datos básicos (nombre, teléfono, etc.)
            await _userService.UpdateProfileAsync(userId, dto);

            // 2) Si viene una nueva foto, la subimos a Azure Blob
            if (profilePhoto != null)
            {
                var container = _azureSettings.ProfilePhotosContainer; // ej. "profile-photos"
                var imageUrl = await _blobHelper.UploadPngAsync(
                    profilePhoto,
                    container,
                    $"user_{userId}");

                // 3) Guardar la URL en el usuario
                await _userService.UpdateProfilePhotoUrlAsync(userId, imageUrl);
            }

            // 4) Volver a leer el perfil actualizado
            var updatedProfile = await _userService.GetProfileAsync(userId);
            if (updatedProfile == null)
                return NotFound("No se pudo recuperar el perfil actualizado.");

            return Ok(updatedProfile);
        }

    }
}
