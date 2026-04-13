using MaratonHub.Api.UserMedia.Models;
using MongoDB.Driver;

namespace MaratonHub.Api.UserMedia;

public class MediaRepository : IMediaRepository
{
    private readonly IMongoCollection<Models.Media> _media;

    public MediaRepository(IMongoDatabase database)
    {
        _media = database.GetCollection<Models.Media>("media");
    }

    // --- Queries ---

    public async Task<List<Models.Media>> GetAllAsync()
    {
        return await _media.Find(_ => true).ToListAsync();
    }

    public async Task<Models.Media?> GetByIdAsync(string id)
    {
        return await _media.Find(m => m.Id == id).FirstOrDefaultAsync();
    }

    public async Task<Models.Media?> GetByExternalApiIdAsync(int externalApiId, string mediaType)
    {
        return await _media
            .Find(m => m.ExternalApiId == externalApiId && m.MediaType == mediaType)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Models.Media>> GetByMediaTypeAsync(string mediaType)
    {
        return await _media
            .Find(m => m.MediaType == mediaType)
            .ToListAsync();
    }

    public async Task<List<Models.Media>> SearchByTitleAsync(string title)
    {
        var filter = Builders<Models.Media>.Filter.Regex(
            m => m.Title,
            new MongoDB.Bson.BsonRegularExpression(title, "i")
        );
        return await _media.Find(filter).ToListAsync();
    }

    // --- Commands ---

    public async Task<Models.Media> CreateAsync(Models.Media media)
    {
        await _media.InsertOneAsync(media);
        return media;
    }

    public async Task<Models.Media?> UpdateAsync(string id, Models.Media media)
    {
        var result = await _media.ReplaceOneAsync(m => m.Id == id, media);
        return result.ModifiedCount > 0 ? media : null;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _media.DeleteOneAsync(m => m.Id == id);
        return result.DeletedCount > 0;
    }

    // --- Upsert by TMDB ID ---

    public async Task UpsertByTmdbIdAsync(Models.Media media)
    {
        var filter = Builders<Models.Media>.Filter.And(
            Builders<Models.Media>.Filter.Eq(m => m.ExternalApiId, media.ExternalApiId),
            Builders<Models.Media>.Filter.Eq(m => m.MediaType, media.MediaType)
        );
        await _media.ReplaceOneAsync(filter, media, new ReplaceOptions { IsUpsert = true });
    }

    // --- Existence check ---

    public async Task<bool> ExistsAsync(int externalApiId, string mediaType)
    {
        var count = await _media
            .CountDocumentsAsync(m => m.ExternalApiId == externalApiId && m.MediaType == mediaType);
        return count > 0;
    }
}
