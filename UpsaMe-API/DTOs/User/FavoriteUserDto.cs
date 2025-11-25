namespace UpsaMe_API.DTOs.User;

public class FavoriteUserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarId { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public string? Career { get; set; }
}