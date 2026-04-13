using MaratonHub.Api.TheMovieDB.Dtos;

namespace MaratonHub.Api.TheMovieDB.Repository;

public interface IMediaCacheRepository
{
    // Movies
    Task<List<MovieDto>?> GetCachedMoviesAsync(string cacheKey);
    Task SaveMoviesAsync(string cacheKey, List<MovieDto> movies);

    // TV Shows
    Task<List<TvShowDto>?> GetCachedTvShowsAsync(string cacheKey);
    Task SaveTvShowsAsync(string cacheKey, List<TvShowDto> tvShows);

    // Persons
    Task<List<PersonDto>?> GetCachedPersonsAsync(string cacheKey);
    Task SavePersonsAsync(string cacheKey, List<PersonDto> persons);
}
