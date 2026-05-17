using MaratonHub.Api.Groups.Models;
using MaratonHub.Api.Groups.Repositories;
using MaratonHub.Api.Notifications.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace MaratonHub.Api.Groups.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatRepository _chatRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly INotificationService _notificationService;

    public ChatHub(IChatRepository chatRepository, IGroupRepository groupRepository, INotificationService notificationService)
    {
        _chatRepository = chatRepository;
        _groupRepository = groupRepository;
        _notificationService = notificationService;
    }

    public async Task JoinGroup(string groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
    }

    public async Task LeaveGroup(string groupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
    }

    public async Task SendMessage(string groupId, string message)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub") ?? "";
        var userName = Context.User?.FindFirstValue("unique_name")
            ?? Context.User?.FindFirstValue(ClaimTypes.Name) ?? "Unknown";

        if (string.IsNullOrWhiteSpace(message)) return;

        var chatMessage = new ChatMessage
        {
            GroupId = groupId,
            UserId = userId,
            UserName = userName,
            Message = message
        };

        var saved = await _chatRepository.SaveMessageAsync(chatMessage);

        await Clients.Group(groupId).SendAsync("ReceiveMessage", new
        {
            id = saved.Id,
            groupId = saved.GroupId,
            userId = saved.UserId,
            userName = saved.UserName,
            message = saved.Message,
            sentAt = saved.SentAt
        });

        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group != null)
        {
            foreach (var member in group.Members.Where(m => m.UserId != userId))
            {
                await _notificationService.CreateAndSendAsync(
                    member.UserId,
                    $"Nuevo mensaje en {group.Name}",
                    $"{userName}: {message}",
                    "info",
                    groupId);
            }
        }
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
