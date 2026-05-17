using MaratonHub.Api.Groups.Models;
using MongoDB.Driver;

namespace MaratonHub.Api.Groups.Repositories;

public class GroupRepository : IGroupRepository
{
    private readonly IMongoCollection<Group> _groups;

    public GroupRepository(IMongoDatabase database)
    {
        _groups = database.GetCollection<Group>("groups");
    }

    public async Task<Group?> GetByIdAsync(string id)
    {
        return await _groups.Find(g => g.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<Group>> GetUserGroupsAsync(string userId)
    {
        return await _groups
            .Find(g => g.Members.Any(m => m.UserId == userId))
            .SortByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<Group> CreateAsync(Group group)
    {
        group.CreatedAt = DateTime.UtcNow;
        await _groups.InsertOneAsync(group);
        return group;
    }

    public async Task<Group?> UpdateAsync(string id, Group group)
    {
        var result = await _groups.ReplaceOneAsync(g => g.Id == id, group);
        return result.ModifiedCount > 0 ? group : null;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _groups.DeleteOneAsync(g => g.Id == id);
        return result.DeletedCount > 0;
    }

    public async Task<Group?> GetByInviteCodeAsync(string inviteCode)
    {
        return await _groups.Find(g => g.InviteCode == inviteCode).FirstOrDefaultAsync();
    }

    public async Task<List<Group>> SearchGroupsAsync(string query)
    {
        return await _groups
            .Find(g => g.Name.ToLower().Contains(query.ToLower()))
            .SortByDescending(g => g.CreatedAt)
            .ToListAsync();
    }
}
