using UpsaMe_API.Data;
using UpsaMe_API.Models;
using Microsoft.EntityFrameworkCore;

namespace UpsaMe_API.Services
{
    public class DbConnectionService : IConnectionService
    {
        private readonly UpsaMeDbContext _db;

        public DbConnectionService(UpsaMeDbContext db)
        {
            _db = db;
        }

        public async Task RegisterConnectionAsync(Guid userId)
        {
            var existing = await _db.UserConnections
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (existing != null)
            {
                existing.LastActivityUtc = DateTime.UtcNow;
            }
            else
            {
                var conn = new UserConnection
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ConnectedAtUtc = DateTime.UtcNow,
                    LastActivityUtc = DateTime.UtcNow
                };

                _db.UserConnections.Add(conn);
            }

            await _db.SaveChangesAsync();
        }

        public async Task UpdateActivityAsync(Guid userId)
        {
            var conn = await _db.UserConnections
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (conn != null)
            {
                conn.LastActivityUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        public async Task RemoveConnectionAsync(Guid userId)
        {
            var conn = await _db.UserConnections
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (conn != null)
            {
                _db.UserConnections.Remove(conn);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<bool> IsOnlineAsync(Guid userId)
        {
            var conn = await _db.UserConnections
                .FirstOrDefaultAsync(c => c.UserId == userId);

            return conn?.IsOnline ?? false;
        }

        public async Task<UserConnection?> GetConnectionAsync(Guid userId)
        {
            return await _db.UserConnections
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }
    }
}
