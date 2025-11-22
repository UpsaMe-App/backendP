using Microsoft.EntityFrameworkCore;
using UpsaMe_API.Data;
using UpsaMe_API.DTOs.User;
using UpsaMe_API.Helpers;

namespace UpsaMe_API.Services
{
    public class UserService
    {
        private readonly UpsaMeDbContext _context;
        private readonly BlobStorageHelper _blobStorageHelper;

        public UserService(UpsaMeDbContext context, BlobStorageHelper blobStorageHelper)
        {
            _context = context;
            _blobStorageHelper = blobStorageHelper;
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
                Phone = user.Phone,
                AvatarId = user.AvatarId 

            };
        }

        // ======================================================
        // ACTUALIZAR PERFIL
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

            if (dto.Semester.HasValue)
                user.Semester = dto.Semester.Value;

            // ---------- Carrera (FK) ----------
            // Solo actualiza si vino y existe en la BD
            if (dto.CareerId.HasValue)
            {
                var exists = await _context.Careers
                    .AsNoTracking()
                    .AnyAsync(c => c.Id == dto.CareerId.Value, ct);

                if (!exists)
                    throw new InvalidOperationException("La carrera seleccionada no existe.");

                user.CareerId = dto.CareerId.Value;
            }

            // ---------- Foto / Avatar ----------
            // Prioridad:
            // 1) Si viene archivo -> subimos a Blob
            // 2) Si NO viene archivo pero sí AvatarId -> usamos avatar fijo
            if (dto.ProfilePhoto != null && dto.ProfilePhoto.Length > 0)
            {
                var contentType = string.IsNullOrWhiteSpace(dto.ProfilePhoto.ContentType)
                    ? "image/jpeg"
                    : dto.ProfilePhoto.ContentType;

                using var stream = dto.ProfilePhoto.OpenReadStream();

                user.ProfilePhotoUrl = await _blobStorageHelper
                    .UploadProfilePhotoAsync(user.Id, stream, contentType);
                user.AvatarId = null;
            }
            else if (!string.IsNullOrWhiteSpace(dto.AvatarId))
            {
                var avatarUrl = AvatarCatalog.ResolveUrl(dto.AvatarId);
                if (avatarUrl == null)
                    throw new InvalidOperationException("Avatar no válido.");

                user.ProfilePhotoUrl = avatarUrl;
                user.AvatarId = dto.AvatarId;
            }

            await _context.SaveChangesAsync(ct);
        }
    }
}
