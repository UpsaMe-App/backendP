using Microsoft.EntityFrameworkCore;
using UpsaMe_API.Data;
using UpsaMe_API.DTOs.Posts;
using UpsaMe_API.Models;

namespace UpsaMe_API.Services
{
    public class PostService
    {
        private readonly UpsaMeDbContext _context;

        public PostService(UpsaMeDbContext context)
        {
            _context = context;
        }

        // ============================================================
// 📌 1. FEED GENERAL (Home)
// ============================================================
        public async Task<List<object>> GetFeedAsync(PostRole? role = null, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.Posts
                .AsNoTracking()
                .Include(p => p.User)
                .ThenInclude(u => u.Career)   // 👈 para poder sacar la carrera del autor
                .Include(p => p.Subject)
                .Include(p => p.Replies)
                .Where(p => p.Status != PostStatus.Deleted)
                .AsQueryable();

            if (role.HasValue)
                query = query.Where(p => p.Role == role.Value);

            var rows = await query
                .OrderByDescending(p => p.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Role,
                    p.Status,
                    p.Title,
                    p.Content,
                    p.Capacity,
                    p.MaxCapacity,
                    p.CalendlyUrl,
                    p.CapacityUsed,
                    p.CreatedAtUtc,

                    // 🔹 Autor: para perfil y UI
                    AuthorId = p.UserId,
                    Author = p.User != null ? $"{p.User.FirstName} {p.User.LastName}" : "Anónimo",

                    // ✅ LO NUEVO: avatar, foto y carrera del autor
                    AuthorAvatarId = p.User != null ? p.User.AvatarId : null,
                    AuthorProfilePhotoUrl = p.User != null ? p.User.ProfilePhotoUrl : null,
                    AuthorCareer = p.User != null && p.User.Career != null ? p.User.Career.Name : null,

                    // Materia en la card
                    SubjectId = p.SubjectId,
                    Subject = p.Subject != null ? p.Subject.Name : null,

                    RepliesCount = p.Replies != null ? p.Replies.Count : 0
                })
                .ToListAsync();

            return rows.Cast<object>().ToList();
        }


        // ============================================================
        // 📌 2. CREAR POST (GENÉRICO - no se usa directo desde el controller)
        // ============================================================
        public async Task<Post> CreateAsync(Post post)
        {
            if (post == null)
                throw new ArgumentNullException(nameof(post));

            if (string.IsNullOrWhiteSpace(post.Content))
                throw new InvalidOperationException("El contenido no puede estar vacío.");

            post.Id = post.Id == Guid.Empty ? Guid.NewGuid() : post.Id;
            post.CreatedAtUtc = DateTime.UtcNow;
            post.UpdatedAtUtc = null;
            post.Status = PostStatus.Active;
            post.CapacityUsed = Math.Max(0, post.CapacityUsed);

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
            return post;
        }

        // ============================================================
        // 📌 2a. CREAR AYUDANTE
        // ============================================================
        public async Task<Post> CreateHelperAsync(Guid userId, CreateHelperPostDto dto)
        { 
            if (dto.SubjectId == Guid.Empty) 
                throw new InvalidOperationException("SubjectId es obligatorio.");

        var exists = await _context.Subjects.AsNoTracking()
            .AnyAsync(s => s.Id == dto.SubjectId); 
        
        if (!exists) 
            throw new InvalidOperationException("La materia (SubjectId) no existe.");

    if (dto.Capacity < 1)
        throw new InvalidOperationException("Capacity debe ser >= 1.");
    if (dto.MaxCapacity < 1)
        throw new InvalidOperationException("MaxCapacity debe ser >= 1.");
    if (dto.Capacity > dto.MaxCapacity)
        throw new InvalidOperationException("Capacity no puede superar MaxCapacity.");

    if (string.IsNullOrWhiteSpace(dto.Title))
        throw new InvalidOperationException("El título es obligatorio.");
    if (string.IsNullOrWhiteSpace(dto.Content))
        throw new InvalidOperationException("El contenido es obligatorio.");

    // 👇 Si no viene CalendlyUrl en el DTO, lo sacamos del perfil
    string? calendlyUrl = dto.CalendlyUrl;
    if (string.IsNullOrWhiteSpace(calendlyUrl))
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new InvalidOperationException("Usuario no encontrado.");

        calendlyUrl = user.CalendlyUrl;
    }

    if (string.IsNullOrWhiteSpace(calendlyUrl))
        throw new InvalidOperationException("No se ha configurado Calendly para este usuario.");

    var post = new Post
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Role = PostRole.Helper,
        Title = dto.Title.Trim(),
        Content = dto.Content.Trim(),
        SubjectId = dto.SubjectId,
        Capacity = dto.Capacity,
        MaxCapacity = dto.MaxCapacity,
        CalendlyUrl = calendlyUrl.Trim(),    // 👈 aquí ya siempre hay una
        Status = PostStatus.Active,
        CapacityUsed = 0,
        CreatedAtUtc = DateTime.UtcNow
    };

    _context.Posts.Add(post);
    await _context.SaveChangesAsync();
    return post;
}


        // ============================================================
        // 📌 2b. CREAR ESTUDIANTE
        // ============================================================
        public async Task<Post> CreateStudentAsync(Guid userId, CreateStudentPostDto dto)
        {
            if (dto.SubjectId == Guid.Empty)
                throw new InvalidOperationException("SubjectId es obligatorio.");

            var exists = await _context.Subjects
                .AsNoTracking()
                .AnyAsync(s => s.Id == dto.SubjectId);

            if (!exists)
                throw new InvalidOperationException("La materia (SubjectId) no existe.");

            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new InvalidOperationException("El título es obligatorio.");
            if (string.IsNullOrWhiteSpace(dto.Content))
                throw new InvalidOperationException("El contenido es obligatorio.");

            var post = new Post
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Role = PostRole.Student,
                Title = dto.Title.Trim(),
                Content = dto.Content.Trim(),
                SubjectId = dto.SubjectId,
                Status = PostStatus.Active,
                Capacity = null,
                MaxCapacity = null,
                CalendlyUrl = null,
                CapacityUsed = 0,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
            return post;
        }

        // ============================================================
        // 📌 2c. CREAR COMENTARIO
        // ============================================================
        public async Task<Post> CreateCommentAsync(Guid userId, CreateCommentPostDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new InvalidOperationException("El título es obligatorio.");

            if (string.IsNullOrWhiteSpace(dto.Content))
                throw new InvalidOperationException("El contenido es obligatorio.");

            var post = new Post
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Role = PostRole.Comment,
                Title = dto.Title.Trim(),
                Content = dto.Content.Trim(),
                SubjectId = null,
                Status = PostStatus.Active,
                Capacity = null,
                MaxCapacity = null,
                CalendlyUrl = null,
                CapacityUsed = 0,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
            return post;
        }

        // ============================================================
        // 📌 3. REPLY A UN POST
        // ============================================================
        public async Task<PostReply?> AddReplyAsync(Guid postId, PostReply reply)
        {
            if (reply == null)
                throw new ArgumentNullException(nameof(reply));

            if (string.IsNullOrWhiteSpace(reply.Content))
                throw new InvalidOperationException("El contenido de la respuesta no puede estar vacío.");

            var post = await _context.Posts
                .Where(p => p.Id == postId && p.Status != PostStatus.Deleted)
                .FirstOrDefaultAsync();

            if (post == null) //..
                return null;

            reply.Id = Guid.NewGuid();
            reply.PostId = postId;
            reply.CreatedAtUtc = DateTime.UtcNow;

            _context.PostReplies.Add(reply);
            await _context.SaveChangesAsync();

            return reply;
        }
// ============================================================
// 📌 3b. OBTENER REPLIES DE UN POST
// ============================================================
        public async Task<List<object>> GetRepliesForPostAsync(Guid postId)
        {
            var replies = await _context.PostReplies
                .AsNoTracking()
                .Include(r => r.User)
                .Where(r => r.PostId == postId)
                .OrderBy(r => r.CreatedAtUtc)
                .Select(r => new
                {
                    r.Id,
                    r.Content,
                    r.CreatedAtUtc,
                    AuthorId = r.UserId,
                    Author = r.User != null ? $"{r.User.FirstName} {r.User.LastName}" : "Anónimo"
                })
                .ToListAsync();

            return replies.Cast<object>().ToList();
        }
        // ============================================================
        // 📌 4. BUSCAR POSTS POR MATERIA
        // ============================================================
        public async Task<List<object>> SearchPostsBySubjectAsync(string query, int page = 1, int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<object>();

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var q = query.Trim().ToLower();

            var subjectIds = await _context.Subjects
                .AsNoTracking()
                .Where(s =>
                    s.Name.ToLower().Contains(q) ||
                    (s.Slug != null && s.Slug.ToLower().Contains(q)) ||
                    (s.Code != null && s.Code.ToLower().Contains(q)))
                .Select(s => s.Id)
                .ToListAsync();

            if (!subjectIds.Any())
                return new List<object>();

            var queryPosts = _context.Posts
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.Subject)
                .Where(p =>
                    p.Status != PostStatus.Deleted &&
                    p.SubjectId.HasValue &&
                    subjectIds.Contains(p.SubjectId.Value));

            var rows = await queryPosts
                .OrderByDescending(p => p.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Role,
                    p.Status,
                    p.Title,
                    p.Content,
                    p.Capacity,
                    p.MaxCapacity,
                    p.CalendlyUrl,
                    p.CapacityUsed,
                    p.CreatedAtUtc,
                    AuthorId = p.UserId,
                    Author = p.User != null ? $"{p.User.FirstName} {p.User.LastName}" : "Anónimo",
                    SubjectId = p.SubjectId,
                    Subject = p.Subject != null ? p.Subject.Name : null
                })
                .ToListAsync();

            return rows.Cast<object>().ToList();
        }

        // ============================================================
        // 📌 5. EDITAR POST (solo dueño)
        // ============================================================
        public async Task<Post?> UpdateAsync(Guid postId, Guid userId, UpdatePostDto dto)
        {
            var post = await _context.Posts
                .FirstOrDefaultAsync(p => p.Id == postId && p.UserId == userId && p.Status != PostStatus.Deleted);

            if (post == null)
                return null;

            if (!string.IsNullOrWhiteSpace(dto.Title))
                post.Title = dto.Title.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Content))
                post.Content = dto.Content.Trim();

            post.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return post;
        }

        // ============================================================
        // 📌 6. ELIMINAR POST (soft delete, solo dueño)
        // ============================================================
        public async Task<bool> DeleteAsync(Guid postId, Guid userId)
        {
            var post = await _context.Posts
                .FirstOrDefaultAsync(p => p.Id == postId && p.UserId == userId && p.Status != PostStatus.Deleted);

            if (post == null)
                return false;

            post.Status = PostStatus.Deleted;
            post.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        // ============================================================
        // 📌 7. POSTS DE UN USUARIO (para "Mis posts")
        // ============================================================
        public async Task<List<Post>> GetByUserAsync(Guid userId)
        {
            return await _context.Posts
                .Where(p => p.UserId == userId && p.Status != PostStatus.Deleted)
                .OrderByDescending(p => p.CreatedAtUtc)
                .ToListAsync();
        }
    }
}                 