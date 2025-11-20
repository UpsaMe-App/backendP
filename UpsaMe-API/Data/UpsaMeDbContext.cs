using Microsoft.EntityFrameworkCore;
using UpsaMe_API.Models;

namespace UpsaMe_API.Data
{
    public class UpsaMeDbContext : DbContext
    {
        public UpsaMeDbContext(DbContextOptions<UpsaMeDbContext> options) : base(options) { }

        public DbSet<NotificationDevice> NotificationDevices => Set<NotificationDevice>();
        public DbSet<Notification> Notifications => Set<Notification>();
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========== Índices únicos básicos ==========
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Career>()
                .HasIndex(c => c.Slug)
                .IsUnique();
            
            // 👇 AQUÍ AGREGÁS ESTE NUEVO
            modelBuilder.Entity<Faculty>()
                .HasIndex(f => f.Slug)
                .IsUnique();
            
            // Sugerido: si vas a buscar Subjects por slug, al menos indexarlo
            modelBuilder.Entity<Subject>()
                .HasIndex(s => s.Slug);

            // Únicos por diseño (URIs de Calendly)
            modelBuilder.Entity<CalendlyEvent>()
                .HasIndex(e => e.EventUri)
                .IsUnique();

            modelBuilder.Entity<CalendlyEventType>()
                .HasIndex(et => et.EventTypeUri)
                .IsUnique();

            // Feed rápido: índice compuesto por Role/Status/CreatedAtUtc
            modelBuilder.Entity<Post>()
                .HasIndex(p => new { p.Role, p.Status, p.CreatedAtUtc });

            // Notificaciones: listar no leídas por usuario
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.IsRead });

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
            
            
        }
    }
}

