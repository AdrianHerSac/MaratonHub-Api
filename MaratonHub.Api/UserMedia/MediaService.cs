using MediaModel = MaratonHub.Api.UserMedia.Models.Media;

namespace MaratonHub.Api.UserMedia;

public class MediaService : IMediaService
{
    private readonly IMediaRepository _mediaRepository;

    public MediaService(IMediaRepository mediaRepository)
    {
        _mediaRepository = mediaRepository;
    }

    public async Task<List<MediaModel>> GetAllAsync()
        => await _mediaRepository.GetAllAsync();

    public async Task<MediaModel?> GetByIdAsync(string id)
        => await _mediaRepository.GetByIdAsync(id);

    public async Task<MediaModel?> GetByTmdbIdAsync(int tmdbId, string mediaType)
        => await _mediaRepository.GetByExternalApiIdAsync(tmdbId, mediaType);

    public async Task<List<MediaModel>> GetByMediaTypeAsync(string mediaType)
        => await _mediaRepository.GetByMediaTypeAsync(mediaType);

    public async Task<List<MediaModel>> SearchByTitleAsync(string title)
        => await _mediaRepository.SearchByTitleAsync(title);

    public async Task<MediaModel> CreateAsync(MediaModel media)
        => await _mediaRepository.CreateAsync(media);

    public async Task<MediaModel?> UpdateAsync(string id, MediaModel media)
        => await _mediaRepository.UpdateAsync(id, media);

    public async Task<bool> DeleteAsync(string id)
        => await _mediaRepository.DeleteAsync(id);
}
