using Microsoft.EntityFrameworkCore;
using UpsaMe_API.Data;
using UpsaMe_API.DTOs.User;
using UpsaMe_API.Models;

namespace UpsaMe_API.Services
{
    public class UserService
    {
        private readonly UpsaMeDbContext _context;

        public UserService(UpsaMeDbContext context)
        {
            _context = context;
        }

        // ======================================================
        // OBTENER PERFIL
        // ======================================================
        public async Task<UserDto?> GetProfileAsync(Guid userId, CancellationToken ct = default)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Career)
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user == null) return null;

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = $"{user.FirstName} {user.LastName}",
                CareerId = user.CareerId,
                Career = user.Career?.Name,
                Semester = user.Semester,
                ProfilePhotoUrl = user.ProfilePhotoUrl,
                AvatarId = user.AvatarId,
                Phone = user.Phone,
                CalendlyUrl = user.CalendlyUrl
            };
        }

        // ======================================================
        // ACTUALIZAR PERFIL (SIN manejar archivo aquí)
        // ======================================================
        public async Task UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken ct = default)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user == null)
                throw new InvalidOperationException("Usuario no encontrado.");

            // ---------- Datos básicos ----------
            if (!string.IsNullOrWhiteSpace(dto.FirstName))
                user.FirstName = dto.FirstName.Trim();

            if (!string.IsNullOrWhiteSpace(dto.LastName))
                user.LastName = dto.LastName.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Phone))
                user.Phone = dto.Phone.Trim();

            if (!string.IsNullOrWhiteSpace(dto.CalendlyUrl))
                user.CalendlyUrl = dto.CalendlyUrl.Trim();

            if (dto.Semester.HasValue)
                user.Semester = dto.Semester.Value;

            // ---------- Carrera ----------
            if (dto.CareerId.HasValue)
            {
                var exists = await _context.Careers
                    .AsNoTracking()
                    .AnyAsync(c => c.Id == dto.CareerId.Value, ct);

                if (!exists)
                    throw new InvalidOperationException("La carrera seleccionada no existe.");

                user.CareerId = dto.CareerId.Value;
            }

            // ---------- Si elige avatar → guardar y limpiar foto ----------
            if (!string.IsNullOrWhiteSpace(dto.AvatarId))
            {
                user.AvatarId = dto.AvatarId;
                user.ProfilePhotoUrl = null;
            }

            await _context.SaveChangesAsync(ct);
        }

        // ======================================================
        // ACTUALIZAR FOTO EN BD
        // ======================================================
        public async Task UpdateProfilePhotoUrlAsync(Guid userId, string? imageUrl, CancellationToken ct = default)
        {
            var user = await _context.Users.FindAsync(new object?[] { userId }, ct);
            if (user == null) return;

            user.ProfilePhotoUrl = imageUrl;

            _context.Users.Update(user);
            await _context.SaveChangesAsync(ct);
        }

        // ======================================================
        // ACTUALIZAR AVATAR (Opcional desde Controller)
        // ======================================================
        public async Task UpdateAvatarAsync(Guid userId, string? avatarId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;

            user.AvatarId = avatarId;

            if (!string.IsNullOrEmpty(avatarId))
                user.ProfilePhotoUrl = null;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}
