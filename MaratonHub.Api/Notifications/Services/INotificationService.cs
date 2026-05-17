namespace MaratonHub.Api.Notifications.Services;

public interface INotificationService
{
    Task CreateAndSendAsync(string userId, string title, string message, string type = "info", string? groupId = null);
}
