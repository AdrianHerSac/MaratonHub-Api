namespace MaratonHub.Api.UserMedia.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? ProfilePicturePath { get; set; }
    public string? Biography { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLogin { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public bool IsVerified { get; set; } = false;
    
    public List<string> StreamingPlatforms { get; set; } = new();

    public List<UserMedia> Ratings { get; set; } = new();
}
