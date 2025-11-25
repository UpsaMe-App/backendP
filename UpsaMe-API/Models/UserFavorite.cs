namespace UpsaMe_API.Models;

public class UserFavorite
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid FavoriteUserId { get; set; }
    public User FavoriteUser { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}