using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UpsaMe_API.Data;

namespace UpsaMe_API.Services
{
    public class ConnectionCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ConnectionCleanupService> _logger;

        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(10);
        private readonly TimeSpan _maxInactiveTime = TimeSpan.FromMinutes(30);

        public ConnectionCleanupService(
            IServiceProvider serviceProvider,
            ILogger<ConnectionCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🔄 ConnectionCleanupService iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<UpsaMeDbContext>();

                    var now = DateTime.UtcNow;

                    var oldConnections = await db.UserConnections
                        .Where(c => now - c.LastActivityUtc > _maxInactiveTime)
                        .ToListAsync(stoppingToken);

                    if (oldConnections.Any())
                    {
                        db.UserConnections.RemoveRange(oldConnections);
                        await db.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation(
                            "🧹 Limpieza: {Count} conexiones eliminadas por inactividad.",
                            oldConnections.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error durante la limpieza de conexiones.");
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }
        }
    }
}
