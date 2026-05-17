using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MaratonHub.Api.Notifications.Hubs;

/// <summary>
/// Hub para notificaciones en tiempo real entre usuarios.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    /// <summary>
    /// Se ejecuta cuando un cliente se conecta al hub.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Se ejecuta cuando un cliente se desconecta del hub.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
