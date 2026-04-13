using MaratonHub.Api.UserMedia.Models;

namespace MaratonHub.Api.UserMedia;

public interface IMediaRepository
{
    // --- Queries ---
    Task<List<Models.Media>> GetAllAsync();
    Task<Models.Media?> GetByIdAsync(string id);
    Task<Models.Media?> GetByExternalApiIdAsync(int externalApiId, string mediaType);
    Task<List<Models.Media>> GetByMediaTypeAsync(string mediaType);
    Task<List<Models.Media>> SearchByTitleAsync(string title);

    // --- Commands ---
    Task<Models.Media> CreateAsync(Models.Media media);
    Task<Models.Media?> UpdateAsync(string id, Models.Media media);
    Task<bool> DeleteAsync(string id);
    Task UpsertByTmdbIdAsync(Models.Media media);

    // --- Existence check ---
    Task<bool> ExistsAsync(int externalApiId, string mediaType);
}
