using MaratonHub.Api.Groups.Models;

namespace MaratonHub.Api.Groups.Repositories;

public interface IGroupRepository
{
    Task<Group?> GetByIdAsync(string id);
    Task<List<Group>> GetUserGroupsAsync(string userId);
    Task<Group> CreateAsync(Group group);
    Task<Group?> UpdateAsync(string id, Group group);
    Task<bool> DeleteAsync(string id);
    Task<Group?> GetByInviteCodeAsync(string inviteCode);
    Task<List<Group>> SearchGroupsAsync(string query);
}
