namespace MaratonHub.Api.UserMedia.Models;

public class UserMedia
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int MediaId { get; set; }
    public Media Media { get; set; } = null!;

    public int Rating { get; set; } 
    public string Review { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; 
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
