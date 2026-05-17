using MaratonHub.Api.Notifications.Models;
using MaratonHub.Api.Notifications.Repositories;
using Microsoft.AspNetCore.SignalR;
using MaratonHub.Api.Notifications.Hubs;

namespace MaratonHub.Api.Notifications.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationService(INotificationRepository repository, IHubContext<NotificationHub> hubContext)
    {
        _repository = repository;
        _hubContext = hubContext;
    }

    public async Task CreateAndSendAsync(string userId, string title, string message, string type = "info", string? groupId = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            GroupId = groupId
        };

        var created = await _repository.CreateAsync(notification);

        var unreadCount = await _repository.GetUnreadCountAsync(userId);

        await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", new
        {
            id = created.Id,
            title = created.Title,
            message = created.Message,
            type = created.Type,
            groupId = created.GroupId,
            read = created.Read,
            createdAt = created.CreatedAt
        });

        await _hubContext.Clients.User(userId).SendAsync("UnreadCount", unreadCount);
    }
}
