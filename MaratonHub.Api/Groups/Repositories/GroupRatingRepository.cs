using MaratonHub.Api.Groups.Models;
using MongoDB.Driver;

namespace MaratonHub.Api.Groups.Repositories;

public class GroupRatingRepository : IGroupRatingRepository
{
    private readonly IMongoCollection<GroupRating> _ratings;

    public GroupRatingRepository(IMongoDatabase database)
    {
        _ratings = database.GetCollection<GroupRating>("group_ratings");
    }

    public async Task<List<GroupRating>> GetGroupRatingsAsync(string groupId)
    {
        return await _ratings
            .Find(r => r.GroupId == groupId)
            .SortByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<GroupRating?> GetByIdAsync(string id)
    {
        return await _ratings.Find(r => r.Id == id).FirstOrDefaultAsync();
    }

    public async Task<GroupRating> CreateAsync(GroupRating rating)
    {
        rating.CreatedAt = DateTime.UtcNow;
        await _ratings.InsertOneAsync(rating);
        return rating;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _ratings.DeleteOneAsync(r => r.Id == id);
        return result.DeletedCount > 0;
    }

    public async Task<List<GroupRating>> GetGroupRatingsByMediaAsync(string groupId, int mediaId, string mediaType)
    {
        return await _ratings
            .Find(r => r.GroupId == groupId && r.MediaId == mediaId && r.MediaType == mediaType)
            .ToListAsync();
    }

    public async Task<GroupMediaAverageDto?> GetGroupAverageAsync(string groupId, int mediaId, string mediaType)
    {
        var ratings = await _ratings
            .Find(r => r.GroupId == groupId && r.MediaId == mediaId && r.MediaType == mediaType)
            .ToListAsync();

        if (ratings.Count == 0) return null;

        return new GroupMediaAverageDto
        {
            AverageRating = Math.Round(ratings.Average(r => r.Rating), 1),
            TotalRatings = ratings.Count
        };
    }
}
