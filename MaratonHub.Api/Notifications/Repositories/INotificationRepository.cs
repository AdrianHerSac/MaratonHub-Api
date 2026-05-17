using MaratonHub.Api.Notifications.Models;

namespace MaratonHub.Api.Notifications.Repositories;

public interface INotificationRepository
{
    Task<List<Notification>> GetUserNotificationsAsync(string userId, int limit = 50);
    Task<long> GetUnreadCountAsync(string userId);
    Task<Notification> CreateAsync(Notification notification);
    Task<bool> MarkAsReadAsync(string id);
    Task<bool> MarkAllAsReadAsync(string userId);
    Task<bool> DeleteAsync(string id);
    Task<bool> ClearAllAsync(string userId);
}
