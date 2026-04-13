using MaratonHub.Api.TheMovieDB.Dtos;
using MaratonHub.Api.TheMovieDB.Repository;
using TMDbLib.Client;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.People;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.Trending;
using TMDbLib.Objects.TvShows;
using TMDbLib.Objects.General; 
using TMDbLib.Objects.Changes;

namespace MaratonHub.Api.TheMovieDB.Services;

public class TheMovieDBService : ITheMovieDBService
{
    private readonly TMDbClient _tmdbClient;
    private readonly IMediaCacheRepository _mediaCache;
    private readonly ILogger<TheMovieDBService> _logger;

    public TheMovieDBService(IConfiguration configuration, IMediaCacheRepository mediaCache, ILogger<TheMovieDBService> logger)
    {
        var apiKey = configuration["TheMovieDB:ApiKey"] ?? throw new InvalidOperationException("TMDb API Key not configured");
        _tmdbClient = new TMDbClient(apiKey);
        _tmdbClient.DefaultLanguage = "es-ES";
        _tmdbClient.DefaultCountry = "ES";
        _mediaCache = mediaCache;
        _logger = logger;
    }

    // ── Movies ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene las películas que son tendencia en las últimas 24 horas.
    /// </summary>
    /// <returns>Lista de películas en tendencia.</returns>
    public async Task<List<MovieDto>> GetTrendingMoviesAsync()
    {
        const string key = "trending_movies_es_extended";
        try
        {
            var cached = await _mediaCache.GetCachedMoviesAsync(key);
            if (cached != null) return cached;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("MongoDB not available (cache read failed): {Msg}. Calling TMDB directly.", ex.Message);
        }

        var result = new List<MovieDto>();
        for (int i = 1; i <= 3; i++)
        {
            var trendingPage = await _tmdbClient.GetTrendingMoviesAsync(TimeWindow.Day, page: i, language: "es-ES");
            if (trendingPage?.Results != null)
            {
                result.AddRange(trendingPage.Results.Select(MapSearchMovieToDto));
            }
        }

        try { await _mediaCache.SaveMoviesAsync(key, result); }
        catch (Exception ex) { _logger.LogWarning("MongoDB cache write failed: {Msg}", ex.Message); }

        return result;
    }
    

    /// <summary>
    /// Retrieves a list of popular movies from the external TMDB service.
    /// If cache is available, it attempts to fetch movies from the cache;
    /// otherwise, it fetches directly from TMDB, updates the cache, and returns the data.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation, containing a list of popular movies as <see cref="MovieDto"/>.</returns>
    public async Task<List<MovieDto>> GetPopularMoviesAsync()
    {
        const string key = "popular_movies_es_extended";
        try
        {
            var cached = await _mediaCache.GetCachedMoviesAsync(key);
            if (cached != null) return cached;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("MongoDB not available (cache read failed): {Msg}. Calling TMDB directly.", ex.Message);
        }

        var result = new List<MovieDto>();
        for (int i = 1; i <= 3; i++)
        {
            var popularPage = await _tmdbClient.GetMoviePopularListAsync(language: "es-ES", page: i);
            if (popularPage?.Results != null)
            {
                result.AddRange(popularPage.Results.Select(MapSearchMovieToDto));
            }
        }

        try { await _mediaCache.SaveMoviesAsync(key, result); }
        catch (Exception ex) { _logger.LogWarning("MongoDB cache write failed: {Msg}", ex.Message); }

        return result;
    }

    /// <summary>
    /// Searches for movies in the TMDB service based on the provided query string.
    /// Retrieves a list of movie results matching the query and maps them to DTOs.
    /// </summary>
    /// <param name="query">The query string used to search for movies.</param>
    /// <returns>A task that represents the asynchronous operation, containing a list of matching movies as <see cref="MovieDto"/>.</returns>
    public async Task<List<MovieDto>> SearchMoviesAsync(string query)
    {
        var results = await _tmdbClient.SearchMovieAsync(query, language: "es-ES");
        return results?.Results?.Select(MapSearchMovieToDto).ToList() ?? new List<MovieDto>();
    }

    public async Task<MovieDto?> GetMovieDetailsAsync(int id)
    {
        try
        {
            var movie = await _tmdbClient.GetMovieAsync(id, language: "es-ES");
            return movie == null ? null : MapMovieToDto(movie);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting movie details for id {Id}: {Msg}", id, ex.Message);
            return null;
        }
    }

    public async Task<List<MovieDto>> GetMoviesByGenreAsync(int genreId)
    {
        var key = $"genre_{genreId}_movies_es_extended";

        try
        {
            var cached = await _mediaCache.GetCachedMoviesAsync(key);
            if (cached != null) return cached;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("MongoDB cache read failed: {Msg}", ex.Message);
        }

        var result = new List<MovieDto>();
        for (int i = 1; i <= 3; i++)
        {
            // Usually TMDbLib Discover accepts page in Query method or has a WherePage() method. 
            // In TMDbLib, Query(int page, string language = null...) or similar.
            try 
            {
                var page = await _tmdbClient.DiscoverMoviesAsync()
                    .IncludeWithAllOfGenre([genreId])
                    .Query(page: i, language: "es-ES");

                if (page?.Results != null)
                {
                    result.AddRange(page.Results.Select(MapSearchMovieToDto));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching genre page {Page}: {Error}", i, ex.Message);
            }
        }

        try { await _mediaCache.SaveMoviesAsync(key, result); }
        catch (Exception ex) { _logger.LogWarning("MongoDB cache write failed: {Msg}", ex.Message); }

        return result;
    }


    // ── TV Shows ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene las series TV que son tendencia en las últimas 24 horas.
    /// </summary>
    /// <returns>Lista de series TV en tendencia.</returns>
    public async Task<List<TvShowDto>> GetTrendingTvShowsAsync()
    {
        const string key = "trending_tv_es";
        try
        {
            var cached = await _mediaCache.GetCachedTvShowsAsync(key);
            if (cached != null) return cached;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("MongoDB not available (cache read failed): {Msg}. Calling TMDB directly.", ex.Message);
        }

        var result = new List<TvShowDto>();
        for (int i = 1; i <= 3; i++)
        {
            var trendingPage = await _tmdbClient.GetTrendingTvAsync(TimeWindow.Day, page: i, language: "es-ES");
            if (trendingPage?.Results != null)
                result.AddRange(trendingPage.Results.Select(MapSearchTvToDto));
        }

        try { await _mediaCache.SaveTvShowsAsync(key, result); }
        catch (Exception ex) { _logger.LogWarning("MongoDB cache write failed: {Msg}", ex.Message); }

        return result;
    }

    /// <summary>
    /// Obtiene las series TV más populares.
    /// </summary>
    /// <returns>Lista de series TV populares.</returns>
    public async Task<List<TvShowDto>> GetPopularTvShowsAsync()
    {
        const string key = "popular_tv_es";
        try
        {
            var cached = await _mediaCache.GetCachedTvShowsAsync(key);
            if (cached != null) return cached;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("MongoDB not available (cache read failed): {Msg}. Calling TMDB directly.", ex.Message);
        }

        var result = new List<TvShowDto>();
        for (int i = 1; i <= 3; i++)
        {
            var popularPage = await _tmdbClient.GetTvShowPopularAsync(language: "es-ES", page: i);
            if (popularPage?.Results != null)
                result.AddRange(popularPage.Results.Select(MapSearchTvToDto));
        }

        try { await _mediaCache.SaveTvShowsAsync(key, result); }
        catch (Exception ex) { _logger.LogWarning("MongoDB cache write failed: {Msg}", ex.Message); }

        return result;
    }

    /// <summary>
    /// Busca series TV por título.
    /// </summary>
    /// <param name="query">Término de búsqueda.</param>
    /// <returns>Lista de series TV encontradas.</returns>
    public async Task<List<TvShowDto>> SearchTvShowsAsync(string query)
    {
        var results = await _tmdbClient.SearchTvShowAsync(query, language: "es-ES");
        return results?.Results?.Select(MapSearchTvToDto).ToList() ?? new List<TvShowDto>();
    }

    /// <summary>
    /// Obtiene los detalles completos de una serie TV por ID.
    /// </summary>
    /// <param name="id">ID de la serie TV.</param>
    /// <returns>Objeto TvShowDto con los detalles o null si no se encuentra.</returns>
    public async Task<TvShowDto?> GetTvShowDetailsAsync(int id)
    {
        try
        {
            var tvShow = await _tmdbClient.GetTvShowAsync(id, language: "es-ES");
            return tvShow == null ? null : MapTvShowToDto(tvShow);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting TV show details for id {Id}: {Msg}", id, ex.Message);
            return null;
        }
    }

    // ── Persons ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene las personas más populares (actores/directores).
    /// </summary>
    /// <returns>Lista de personas populares.</returns>
    public async Task<List<PersonDto>> GetPopularPersonsAsync()
    {
        const string key = "popular_persons_es";
        try
        {
            var cached = await _mediaCache.GetCachedPersonsAsync(key);
            if (cached != null) return cached;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("MongoDB not available (cache read failed): {Msg}. Calling TMDB directly.", ex.Message);
        }

        var popular = await _tmdbClient.GetPersonPopularListAsync(language: "es-ES");
        var result = popular?.Results?.Select(MapSearchPersonToDto).ToList() ?? new List<PersonDto>();

        try { await _mediaCache.SavePersonsAsync(key, result); }
        catch (Exception ex) { _logger.LogWarning("MongoDB cache write failed: {Msg}", ex.Message); }

        return result;
    }

    /// <summary>
    /// Busca personas por nombre.
    /// </summary>
    /// <param name="query">Término de búsqueda.</param>
    /// <returns>Lista de personas encontradas.</returns>
    public async Task<List<PersonDto>> SearchPersonsAsync(string query)
    {
        var results = await _tmdbClient.SearchPersonAsync(query, language: "es-ES");
        return results?.Results?.Select(MapSearchPersonToDto).ToList() ?? new List<PersonDto>();
    }

    /// <summary>
    /// Obtiene los detalles completos de una persona por ID.
    /// </summary>
    /// <param name="id">ID de la persona.</param>
    /// <returns>Objeto PersonDto con los detalles o null si no se encuentra.</returns>
    public async Task<PersonDto?> GetPersonDetailsAsync(int id)
    {
        try
        {
            var person = await _tmdbClient.GetPersonAsync(id, language: "es-ES");
            return person == null ? null : MapPersonToDto(person);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting person details for id {Id}: {Msg}", id, ex.Message);
            return null;
        }
    }

    // ── Mapping helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Convierte un objeto SearchMovie a MovieDto.
    /// </summary>
    private static MovieDto MapSearchMovieToDto(SearchMovie movie) => new()
    {
        Id = movie.Id,
        Title = movie.Title ?? string.Empty,
        Overview = movie.Overview ?? string.Empty,
        PosterPath = movie.PosterPath,
        BackdropPath = movie.BackdropPath,
        ReleaseDate = movie.ReleaseDate,
        VoteAverage = movie.VoteAverage,
        VoteCount = movie.VoteCount,
        OriginalLanguage = movie.OriginalLanguage,
        Genres = new List<GenreDto>()
    };

    /// <summary>
    /// Convierte un objeto Movie a MovieDto.
    /// </summary>
    private static MovieDto MapMovieToDto(Movie movie) => new()
    {
        Id = movie.Id,
        Title = movie.Title ?? string.Empty,
        Overview = movie.Overview ?? string.Empty,
        PosterPath = movie.PosterPath,
        BackdropPath = movie.BackdropPath,
        ReleaseDate = movie.ReleaseDate,
        VoteAverage = movie.VoteAverage,
        VoteCount = movie.VoteCount,
        OriginalLanguage = movie.OriginalLanguage,
        Genres = movie.Genres?.Select(g => new GenreDto { Id = g.Id, Name = g.Name }).ToList() ?? new List<GenreDto>()
    };

    /// <summary>
    /// Convierte un objeto SearchTv a TvShowDto.
    /// </summary>
    private static TvShowDto MapSearchTvToDto(SearchTv tvShow) => new()
    {
        Id = tvShow.Id,
        Name = tvShow.Name ?? string.Empty,
        Overview = tvShow.Overview ?? string.Empty,
        PosterPath = tvShow.PosterPath,
        BackdropPath = tvShow.BackdropPath,
        FirstAirDate = tvShow.FirstAirDate,
        VoteAverage = tvShow.VoteAverage,
        VoteCount = tvShow.VoteCount,
        OriginalLanguage = tvShow.OriginalLanguage,
        Genres = new List<GenreDto>()
    };

    /// <summary>
    /// Convierte un objeto TvShow a TvShowDto.
    /// </summary>
    private static TvShowDto MapTvShowToDto(TvShow tvShow) => new()
    {
        Id = tvShow.Id,
        Name = tvShow.Name ?? string.Empty,
        Overview = tvShow.Overview ?? string.Empty,
        PosterPath = tvShow.PosterPath,
        BackdropPath = tvShow.BackdropPath,
        FirstAirDate = tvShow.FirstAirDate,
        VoteAverage = tvShow.VoteAverage,
        VoteCount = tvShow.VoteCount,
        OriginalLanguage = tvShow.OriginalLanguage,
        Genres = tvShow.Genres?.Select(g => new GenreDto { Id = g.Id, Name = g.Name }).ToList() ?? new List<GenreDto>(),
        NumberOfSeasons = tvShow.NumberOfSeasons,
        NumberOfEpisodes = tvShow.NumberOfEpisodes,
        Status = tvShow.Status
    };

    /// <summary>
    /// Convierte un objeto SearchPerson a PersonDto.
    /// </summary>
    private static PersonDto MapSearchPersonToDto(SearchPerson person) => new()
    {
        Id = person.Id,
        Name = person.Name ?? string.Empty,
        ProfilePath = person.ProfilePath,
        Popularity = person.Popularity,
        KnownForDepartment = null,
        Biography = null,
        Birthday = null,
        PlaceOfBirth = null
    };

    /// <summary>
    /// Convierte un objeto Person a PersonDto.
    /// </summary>
    private static PersonDto MapPersonToDto(Person person) => new()
    {
        Id = person.Id,
        Name = person.Name ?? string.Empty,
        ProfilePath = person.ProfilePath,
        Popularity = person.Popularity,
        KnownForDepartment = person.KnownForDepartment,
        Biography = person.Biography,
        Birthday = person.Birthday,
        PlaceOfBirth = person.PlaceOfBirth
    };

    // ── Daily Changes ───────────────────────────────────────

    /// <summary>
    /// Obtiene los IDs de las películas que han cambiado en las últimas 24 horas.
    /// </summary>
    /// <returns>Lista de IDs de películas.</returns>
    public async Task<List<int>> GetChangedMovieIdsAsync()
    {
        try
        {
            int page = 1;
            DateTime? endDate = DateTime.UtcNow;
            DateTime? startDate = endDate.Value.AddDays(-1);

            // Método correcto en TMDbLib: GetMoviesChangesAsync (con 's')
            var changes = await _tmdbClient.GetMoviesChangesAsync(page, startDate, endDate);

            if (changes?.Results == null) return new List<int>();

            return changes.Results.Select(c => c.Id).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener IDs cambiados de películas.");
            return new List<int>();
        }
    }

    /// <summary>
    /// Obtiene los IDs de las series TV que han cambiado en las últimas 24 horas.
    /// </summary>
    /// <returns>Lista de IDs de series TV.</returns>
    public async Task<List<int>> GetChangedTvShowIdsAsync()
    {
        try
        {
            int page = 1;
            DateTime? endDate = DateTime.UtcNow;
            DateTime? startDate = endDate.Value.AddDays(-1);

            // Método correcto en TMDbLib: GetTvChangesAsync
            var changes = await _tmdbClient.GetTvChangesAsync(page, startDate, endDate);

            if (changes?.Results == null) return new List<int>();

            return changes.Results.Select(c => c.Id).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener IDs cambiados de series TV.");
            return new List<int>();
        }
    }
}
