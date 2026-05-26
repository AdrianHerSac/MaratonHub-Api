using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace MaratonHub.Api.Common;

public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst("sub")?.Value 
            ?? connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
