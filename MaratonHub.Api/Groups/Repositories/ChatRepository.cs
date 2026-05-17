using MaratonHub.Api.Groups.Models;
using MongoDB.Driver;

namespace MaratonHub.Api.Groups.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly IMongoCollection<ChatMessage> _messages;

    public ChatRepository(IMongoDatabase database)
    {
        _messages = database.GetCollection<ChatMessage>("group_messages");
    }

    public async Task<List<ChatMessage>> GetMessagesAsync(string groupId, int limit = 50)
    {
        return await _messages
            .Find(m => m.GroupId == groupId)
            .SortByDescending(m => m.SentAt)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<ChatMessage> SaveMessageAsync(ChatMessage message)
    {
        message.SentAt = DateTime.UtcNow;
        await _messages.InsertOneAsync(message);
        return message;
    }
}
