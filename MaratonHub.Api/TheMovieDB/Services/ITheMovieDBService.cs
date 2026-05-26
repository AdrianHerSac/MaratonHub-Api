using MaratonHub.Api.TheMovieDB.Dtos;

namespace MaratonHub.Api.TheMovieDB.Services;

public interface ITheMovieDBService
{
    // Movies
    Task<List<MovieDto>> GetTrendingMoviesAsync();
    Task<List<MovieDto>> GetPopularMoviesAsync();
    Task<List<MovieDto>> GetMoviesByGenreAsync(int genreId);
    Task<List<MovieDto>> SearchMoviesAsync(string query);
    Task<MovieDto?> GetMovieDetailsAsync(int id);

    // TV Shows
    Task<List<TvShowDto>> GetTrendingTvShowsAsync();
    Task<List<TvShowDto>> GetPopularTvShowsAsync();
    Task<List<TvShowDto>> GetTvShowsByGenreAsync(int genreId);
    Task<List<TvShowDto>> SearchTvShowsAsync(string query);
    Task<TvShowDto?> GetTvShowDetailsAsync(int id);
    Task<SeasonDto?> GetTvShowSeasonAsync(int tvShowId, int seasonNumber);

    // Persons
    Task<List<PersonDto>> GetPopularPersonsAsync();
    Task<List<PersonDto>> SearchPersonsAsync(string query);
    Task<PersonDto?> GetPersonDetailsAsync(int id);

    // Daily Changes (for sync worker)
    Task<List<int>> GetChangedMovieIdsAsync();
    Task<List<int>> GetChangedTvShowIdsAsync();
}
