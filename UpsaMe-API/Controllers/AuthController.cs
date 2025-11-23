using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using UpsaMe_API.DTOs.Auth;
using UpsaMe_API.Services;

namespace UpsaMe_API.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly IConnectionService _connectionService;

        public AuthController(AuthService authService, IConnectionService connectionService)
        {
            _authService = authService;
            _connectionService = connectionService;
        }

        /// <summary>Registro de usuario UPSA.</summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TokenResponseDto>> Register(
            [FromBody] RegisterDto dto,
            CancellationToken ct)
        {
            try
            {
                var tokens = await _authService.RegisterAsync(dto);

                // EXTRAER userId desde accessToken
                var userId = ExtractUserIdFromToken(tokens.AccessToken);
                if (userId != Guid.Empty)
                    await _connectionService.RegisterConnectionAsync(userId);

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