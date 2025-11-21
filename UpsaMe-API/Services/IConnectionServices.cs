using UpsaMe_API.Models;

namespace UpsaMe_API.Services
{
    public interface IConnectionService
    {
        Task RegisterConnectionAsync(Guid userId);
        Task UpdateActivityAsync(Guid userId);
        Task RemoveConnectionAsync(Guid userId);
        Task<bool> IsOnlineAsync(Guid userId);
        Task<UserConnection?> GetConnectionAsync(Guid userId);
    }
}