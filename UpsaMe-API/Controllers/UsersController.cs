using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UpsaMe_API.DTOs.User;
using UpsaMe_API.Helpers;
using UpsaMe_API.Services;
using Microsoft.Extensions.Options;
using UpsaMe_API.Config;

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

        public UsersController(
            UserService userService,
            BlobStorageHelper blobHelper,
            IOptions<AzureSettings> azureOptions)
        {
            _userService = userService;
            _blobHelper = blobHelper;
            _azureSettings = azureOptions.Value;
        }

        // ================================================================
        // PERFIL DEL USUARIO AUTENTICADO
        // ================================================================
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

        // ================================================================
        // PERFIL PÚBLICO
        // ================================================================
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

        // ================================================================
        // ESTADO ONLINE (WebSockets)
        // ================================================================
        [HttpGet("{id:guid}/online-status")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOnlineStatus(
            Guid id,
            [FromServices] IConnectionService connectionService)
        {
            var online = await connectionService.IsOnlineAsync(id);
            return Ok(new { userId = id, online });
        }

        // ================================================================
        // OBTENER CATÁLOGO DE AVATARES
        // ================================================================
        [HttpGet("avatars/options")]
        [AllowAnonymous]
        public IActionResult GetAvatarOptions()
        {
            return Ok(AvatarCatalog.GetAll());
        }

        // ================================================================
        // ACTUALIZAR PERFIL (INCLUYE AVATAR Y FOTO REAL)
        // ================================================================
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

            // ------------------------------------------------------------
            // 1) Actualizar datos básicos
            // ------------------------------------------------------------
            await _userService.UpdateProfileAsync(userId, dto);

            // ------------------------------------------------------------
            // 2) Si sube foto → subir a Azure, guardar URL y borrar avatar
            // ------------------------------------------------------------
            if (profilePhoto != null)
            {
                var container = _azureSettings.ProfilePhotosContainer;

                var imageUrl = await _blobHelper.UploadPngAsync(
                    profilePhoto,
                    container,
                    $"user_{userId}");

                await _userService.UpdateProfilePhotoUrlAsync(userId, imageUrl);
                await _userService.UpdateAvatarAsync(userId, null); // ❗ borrar avatar
            }

            // ------------------------------------------------------------
            // 3) Si elige avatar → borrar foto y actualizar AvatarId
            // ------------------------------------------------------------
            if (!string.IsNullOrWhiteSpace(dto.AvatarId))
            {
                await _userService.UpdateAvatarAsync(userId, dto.AvatarId);
                await _userService.UpdateProfilePhotoUrlAsync(userId, null); // ❗ borrar foto
            }

            // ------------------------------------------------------------
            // 4) Devolver perfil actualizado
            // ------------------------------------------------------------
            var updatedProfile = await _userService.GetProfileAsync(userId);
            if (updatedProfile == null)
                return NotFound("No se pudo recuperar el perfil actualizado.");

            return Ok(updatedProfile);
        }
    }
}
