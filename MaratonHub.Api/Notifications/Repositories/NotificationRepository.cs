using MaratonHub.Api.Notifications.Models;
using MongoDB.Driver;

namespace MaratonHub.Api.Notifications.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly IMongoCollection<Notification> _notifications;

    public NotificationRepository(IMongoDatabase database)
    {
        _notifications = database.GetCollection<Notification>("notifications");
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(string userId, int limit = 50)
    {
        return await _notifications
            .Find(n => n.UserId == userId)
            .SortByDescending(n => n.CreatedAt)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<long> GetUnreadCountAsync(string userId)
    {
        return await _notifications.CountDocumentsAsync(n => n.UserId == userId && !n.Read);
    }

    public async Task<Notification> CreateAsync(Notification notification)
    {
        notification.CreatedAt = DateTime.UtcNow;
        await _notifications.InsertOneAsync(notification);
        return notification;
    }

    public async Task<bool> MarkAsReadAsync(string id)
    {
        var result = await _notifications.UpdateOneAsync(
            n => n.Id == id,
            Builders<Notification>.Update.Set(n => n.Read, true));
        return result.ModifiedCount > 0;
    }

    public async Task<bool> MarkAllAsReadAsync(string userId)
    {
        var result = await _notifications.UpdateManyAsync(
            n => n.UserId == userId && !n.Read,
            Builders<Notification>.Update.Set(n => n.Read, true));
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _notifications.DeleteOneAsync(n => n.Id == id);
        return result.DeletedCount > 0;
    }

    public async Task<bool> ClearAllAsync(string userId)
    {
        var result = await _notifications.DeleteManyAsync(n => n.UserId == userId);
        return result.DeletedCount > 0;
    }
}
