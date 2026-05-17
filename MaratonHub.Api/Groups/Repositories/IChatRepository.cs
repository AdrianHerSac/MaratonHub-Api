using MaratonHub.Api.Groups.Models;

namespace MaratonHub.Api.Groups.Repositories;

public interface IChatRepository
{
    Task<List<ChatMessage>> GetMessagesAsync(string groupId, int limit = 50);
    Task<ChatMessage> SaveMessageAsync(ChatMessage message);
}
