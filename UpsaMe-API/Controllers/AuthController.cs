using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using UpsaMe_API.DTOs.Auth;
using UpsaMe_API.Services;
using Microsoft.Extensions.Options;
using UpsaMe_API.Config;
using UpsaMe_API.Helpers;

namespace UpsaMe_API.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly IConnectionService _connectionService;
        private readonly BlobStorageHelper _blobHelper;
        private readonly AzureSettings _azureSettings;

        public AuthController(
            AuthService authService,
            IConnectionService connectionService,
            BlobStorageHelper blobHelper,
            IOptions<AzureSettings> azureOptions)
        {
            _authService = authService;
            _connectionService = connectionService;
            _blobHelper = blobHelper;
            _azureSettings = azureOptions.Value;
        }

        /// <summary>Registro de usuario UPSA.</summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TokenResponseDto>> Register(
            [FromForm] RegisterDto dto,
            IFormFile? profilePhoto,
            CancellationToken ct)
        {
            try
            {
                // 1) Registrar usuario como siempre
                var tokens = await _authService.RegisterAsync(dto, ct);

                // 2) EXTRAER userId desde accessToken (ya lo tenías)
                var userId = ExtractUserIdFromToken(tokens.AccessToken);
                if (userId != Guid.Empty)
                {
                    await _connectionService.RegisterConnectionAsync(userId);

                    // 3) Si viene foto de perfil, subirla a Azure Blob y guardar la URL
                    if (profilePhoto != null)
                    {
                        var container = _azureSettings.ProfilePhotosContainer; // ej. "profile-photos"
                        var imageUrl = await _blobHelper.UploadPngAsync(
                            profilePhoto,
                            container,
                            $"user_{userId}");

                        // Método en AuthService para actualizar la foto
                        await _authService.UpdateProfilePhotoUrlAsync(userId, imageUrl, ct);
                    }
                }

                return Ok(tokens);
            }
            catch (Exception ex)
            {
                return Problem(
                    title: "No se pudo registrar",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status400BadRequest);
            }
        }

        /// <summary>Login con email institucional y contraseña.</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TokenResponseDto>> Login(
            [FromBody] LoginDto dto,
            CancellationToken ct)
        {
            try
            {
                var tokens = await _authService.LoginAsync(dto);

                // EXTRAER userId desde accessToken
                var userId = ExtractUserIdFromToken(tokens.AccessToken);
                if (userId != Guid.Empty)
                    await _connectionService.RegisterConnectionAsync(userId);

                return Ok(tokens);
            }
            catch (Exception ex)
            {
                return Problem(
                    title: "Credenciales inválidas",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status400BadRequest);
            }
        }

        /// <summary>Refresca el access token usando el refresh token.</summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TokenResponseDto>> Refresh(
            [FromBody] RefreshTokenRequestDto body,
            CancellationToken ct)
        {
            try
            {
                var tokens = await _authService.RefreshTokenAsync(body.RefreshToken);

                // EXTRAER userId desde accessToken
                var userId = ExtractUserIdFromToken(tokens.AccessToken);
                if (userId != Guid.Empty)
                    await _connectionService.UpdateActivityAsync(userId);

                return Ok(tokens);
            }
            catch (Exception ex)
            {
                return Problem(
                    title: "Refresh inválido",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status400BadRequest);
            }
        }

        // ============================================================
        //          MÉTODO PRIVADO: Extraer UserId del JWT
        // ============================================================
        private Guid ExtractUserIdFromToken(string jwt)
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(jwt))
                return Guid.Empty;

            var token = handler.ReadJwtToken(jwt);

            var sub = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (Guid.TryParse(sub, out var userId))
                return userId;

            return Guid.Empty;
        }
    }

    public sealed class RefreshTokenRequestDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
