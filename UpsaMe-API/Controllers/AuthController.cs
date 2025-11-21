using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UpsaMe_API.DTOs.Auth;
using UpsaMe_API.Services;

namespace UpsaMe_API.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
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
    }

    /// <summary>Body para /auth/refresh.</summary>
    public sealed class RefreshTokenRequestDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}