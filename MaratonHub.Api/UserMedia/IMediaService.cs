using MediaModel = MaratonHub.Api.UserMedia.Models.Media;

namespace MaratonHub.Api.UserMedia;

public interface IMediaService
{
    Task<List<MediaModel>> GetAllAsync();
    Task<MediaModel?> GetByIdAsync(string id);
    Task<MediaModel?> GetByTmdbIdAsync(int tmdbId, string mediaType);
    Task<List<MediaModel>> GetByMediaTypeAsync(string mediaType);
    Task<List<MediaModel>> SearchByTitleAsync(string title);
    Task<MediaModel> CreateAsync(MediaModel media);
    Task<MediaModel?> UpdateAsync(string id, MediaModel media);
    Task<bool> DeleteAsync(string id);
}
