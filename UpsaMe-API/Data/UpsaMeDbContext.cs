using Microsoft.EntityFrameworkCore;
using UpsaMe_API.Models;

namespace UpsaMe_API.Data
{
    public class UpsaMeDbContext : DbContext
    {
        public UpsaMeDbContext(DbContextOptions<UpsaMeDbContext> options) : base(options) { }

        public DbSet<NotificationDevice> NotificationDevices => Set<NotificationDevice>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<UserFavorite> UserFavorites { get; set; } = null!;
        
        // Core
        public DbSet<User> Users => Set<User>();
        public DbSet<Faculty> Faculties => Set<Faculty>();
        public DbSet<Career> Careers => Set<Career>();
        public DbSet<Subject> Subjects => Set<Subject>();
        
        // Social
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<PostReply> PostReplies => Set<PostReply>();

        // Calendly
        public DbSet<CalendlyEvent> CalendlyEvents => Set<CalendlyEvent>();
        public DbSet<CalendlyEventType> CalendlyEventTypes => Set<CalendlyEventType>();
        public DbSet<Session> Sessions => Set<Session>();
        public DbSet<WebhookLog> WebhookLogs => Set<WebhookLog>();

        // User connections
        public DbSet<UserConnection> UserConnections => Set<UserConnection>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========== Índices únicos ==========
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Career>()
                .HasIndex(c => c.Slug)
                .IsUnique();

            modelBuilder.Entity<Faculty>()
                .HasIndex(f => f.Slug)
                .IsUnique();

            modelBuilder.Entity<Subject>()
                .HasIndex(s => s.Slug);

            modelBuilder.Entity<CalendlyEvent>()
                .HasIndex(e => e.EventUri)
                .IsUnique();

            modelBuilder.Entity<CalendlyEventType>()
                .HasIndex(et => et.EventTypeUri)
                .IsUnique();

            modelBuilder.Entity<Post>()
                .HasIndex(p => new { p.Role, p.Status, p.CreatedAtUtc });

            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.IsRead });

            // 👇 Indice UserConnection
            modelBuilder.Entity<UserConnection>()
                .HasIndex(c => c.UserId)
                .IsUnique();

            // ========== Relaciones ==========
            modelBuilder.Entity<Career>()
                .HasOne(c => c.Faculty)
                .WithMany(f => f.Careers)
                .HasForeignKey(c => c.FacultyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Subject>()
                .HasOne(s => s.Career)
                .WithMany(c => c.Subjects)
                .HasForeignKey(s => s.CareerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Post>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Career)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.CareerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Post>()
                .HasOne(p => p.Subject)
                .WithMany()
                .HasForeignKey(p => p.SubjectId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PostReply>()
                .HasOne(r => r.Post)
                .WithMany(p => p.Replies)
                .HasForeignKey(r => r.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PostReply>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<UserFavorite>(entity =>
            {
                // Clave compuesta: un usuario no puede guardar al mismo favorito dos veces
                entity.HasKey(uf => new { uf.UserId, uf.FavoriteUserId });

                entity.HasOne(uf => uf.User)
                    .WithMany(u => u.Favorites)
                    .HasForeignKey(uf => uf.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(uf => uf.FavoriteUser)
                    .WithMany() // si luego agregas FavoritedBy, podrías poner .WithMany(u => u.FavoritedBy)
                    .HasForeignKey(uf => uf.FavoriteUserId)
                    .OnDelete(DeleteBehavior.NoAction); // evita ciclo de cascadas
            });

        }
    }
}