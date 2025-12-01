using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using UpsaMe_API.Config;
using UpsaMe_API.Data;
using UpsaMe_API.DTOs.Auth;
using UpsaMe_API.Helpers;
using UpsaMe_API.Models;

namespace UpsaMe_API.Services
{
    public class AuthService
    {
        private readonly UpsaMeDbContext _context;
        private readonly JwtSettings _jwt;

        // a(2005–2025) + 6 dígitos + dominio
        private static readonly Regex UpsaEmailRegex =
            new(@"^a(2005|2006|2007|2008|2009|201[0-9]|202[0-5])\d{6}@estudiantes\.upsa\.edu\.bo$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public AuthService(UpsaMeDbContext context, IConfiguration config)
        {
            _context = context;
            _jwt = config.GetSection("JwtSettings").Get<JwtSettings>()
                   ?? throw new InvalidOperationException("JwtSettings no configurado.");
        }

        // =======================
        // REGISTER
        // =======================
        public async Task<TokenResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();
            if (!UpsaEmailRegex.IsMatch(email))
                throw new InvalidOperationException("Email institucional no válido.");

            ValidatePassword(dto.Password);

            var exists = await _context.Users.AnyAsync(u => u.Email == email);
            if (exists)
                throw new InvalidOperationException("El correo ya está registrado.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = PasswordHasher.HashPassword(dto.Password),
                FirstName = dto.FirstName?.Trim() ?? string.Empty,
                LastName = dto.LastName?.Trim() ?? string.Empty,
                CareerId = dto.CareerId,
                Semester = dto.Semester,
                Phone = dto.Phone,
                
                // 👇 AvatarId viene del DTO (opcional)
                AvatarId = dto.AvatarId,      

                // 👇 Foto se llena luego vía UpdateProfilePhotoUrlAsync
                ProfilePhotoUrl = null
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return await GenerateTokensAsync(user);
        }

        // =======================
        // GUARDAR URL DE FOTO DE PERFIL
        // =======================
        public async Task UpdateProfilePhotoUrlAsync(Guid userId, string imageUrl, CancellationToken ct = default)
        {
            var user = await _context.Users.FindAsync(new object?[] { userId }, ct);
            if (user == null) return;

            user.ProfilePhotoUrl = imageUrl;

            _context.Users.Update(user);
            await _context.SaveChangesAsync(ct);
        }

        // =======================
        // LOGIN
        // =======================
        public async Task<TokenResponseDto> LoginAsync(LoginDto dto)
        {
            var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                throw new InvalidOperationException("Credenciales incorrectas.");

            if (string.IsNullOrEmpty(user.PasswordHash) ||
                !PasswordHasher.VerifyPassword(dto.Password, user.PasswordHash))
                throw new InvalidOperationException("Credenciales incorrectas.");

            return await GenerateTokensAsync(user);
        }

        // =======================
        // REFRESH TOKEN
        // =======================
        public async Task<TokenResponseDto> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new InvalidOperationException("Refresh token requerido.");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            if (user == null)
                throw new InvalidOperationException("Refresh token inválido.");

            if (!user.RefreshTokenExpiresAtUtc.HasValue ||
                user.RefreshTokenExpiresAtUtc.Value < DateTime.UtcNow)
                throw new InvalidOperationException("Refresh token expirado.");

            return await GenerateTokensAsync(user);
        }

        // =======================
        // GENERAR TOKENS
        // =======================
        private async Task<TokenResponseDto> GenerateTokensAsync(User user)
        {
            var keyBytes = Encoding.UTF8.GetBytes(_jwt.Key);
            var creds = new SigningCredentials(
                new SymmetricSecurityKey(keyBytes),
                SecurityAlgorithms.HmacSha256
            );

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new("fullName", $"{user.FirstName} {user.LastName}")
            };

            var accessToken = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes),
                signingCredentials: creds
            );

            var accessTokenString = new JwtSecurityTokenHandler().WriteToken(accessToken);

            var newRefreshToken = Guid.NewGuid().ToString("N");
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpiryDays);

            await _context.SaveChangesAsync();

            return new TokenResponseDto
            {
                AccessToken = accessTokenString,
                RefreshToken = newRefreshToken,
                ExpiresAtUtc = accessToken.ValidTo
            };
        }

        // =======================
        // VALIDACIÓN DE PASSWORD
        // =======================
        private static void ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                throw new InvalidOperationException("La contraseña debe tener al menos 8 caracteres.");

            var hasUpper = password.Any(char.IsUpper);
            var hasLower = password.Any(char.IsLower);
            var hasDigit = password.Any(char.IsDigit);

            if (!(hasUpper && hasLower && hasDigit))
                throw new InvalidOperationException("La contraseña debe incluir mayúsculas, minúsculas y dígitos.");
        }
    }
}
