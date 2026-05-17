using MaratonHub.Api.Groups.Models;

namespace MaratonHub.Api.Groups.Repositories;

public interface IGroupRatingRepository
{
    Task<List<GroupRating>> GetGroupRatingsAsync(string groupId);
    Task<GroupRating?> GetByIdAsync(string id);
    Task<GroupRating> CreateAsync(GroupRating rating);
    Task<bool> DeleteAsync(string id);
    Task<List<GroupRating>> GetGroupRatingsByMediaAsync(string groupId, int mediaId, string mediaType);
    Task<GroupMediaAverageDto?> GetGroupAverageAsync(string groupId, int mediaId, string mediaType);
}

public class GroupMediaAverageDto
{
    public double AverageRating { get; set; }
    public int TotalRatings { get; set; }
}
