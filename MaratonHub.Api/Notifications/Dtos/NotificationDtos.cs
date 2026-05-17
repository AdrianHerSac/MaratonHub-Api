namespace MaratonHub.Api.Notifications.Dtos;

public class NotificationDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "info";
    public string? GroupId { get; set; }
    public bool Read { get; set; }
    public DateTime CreatedAt { get; set; }
}
