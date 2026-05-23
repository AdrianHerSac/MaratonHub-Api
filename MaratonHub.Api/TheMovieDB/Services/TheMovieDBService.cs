using MaratonHub.Api.TheMovieDB.Dtos;
using MaratonHub.Api.TheMovieDB.Repository;
using MaratonHub.Api.Common;
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
    private readonly IRedisCacheService _redisCache;
    private readonly ILogger<TheMovieDBService> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="TheMovieDBService"/>.
    /// </summary>
    public TheMovieDBService(IConfiguration configuration, IMediaCacheRepository mediaCache, IRedisCacheService redisCache, ILogger<TheMovieDBService> logger)
    {
        var apiKey = configuration["TheMovieDB:ApiKey"] ?? throw new InvalidOperationException("TMDb API Key not configured");
        _tmdbClient = new TMDbClient(apiKey);
        _tmdbClient.DefaultLanguage = "es-ES";
        _tmdbClient.DefaultCountry = "ES";
        _mediaCache = mediaCache;
        _redisCache = redisCache;
        _logger = logger;
    }

    // ── Movies 

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
        var cacheKey = $"movie_search_{query.ToLowerInvariant().Replace(" ", "_")}";
        
        var cached = await _redisCache.GetAsync<List<MovieDto>>(cacheKey);
        if (cached != null) return cached;

        var results = await _tmdbClient.SearchMovieAsync(query, language: "es-ES");
        var movies = results?.Results?.Select(MapSearchMovieToDto).ToList() ?? new List<MovieDto>();

        if (movies.Count > 0)
        {
            await _redisCache.SetAsync(cacheKey, movies, TimeSpan.FromMinutes(15));
        }

        return movies;
    }

    /// <summary>
    /// Obtiene los detalles completos de una película por ID.
    /// </summary>
    /// <param name="id">ID de la película.</param>
    /// <returns>A task that represents the asynchronous operation, containing the movie details as <see cref="MovieDto"/>.</returns>
    public async Task<MovieDto?> GetMovieDetailsAsync(int id)
    {
        var cacheKey = $"movie_v3_{id}";
        
        var cached = await _redisCache.GetAsync<MovieDto>(cacheKey);
        if (cached != null) return cached;

        try
        {
            var movie = await _tmdbClient.GetMovieAsync(id, MovieMethods.Credits | MovieMethods.Videos);
            if (movie == null) return null;

            var dto = MapMovieToDto(movie);
            await _redisCache.SetAsync(cacheKey, dto, TimeSpan.FromHours(2));
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting movie details for id {Id}: {Msg}", id, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Obtiene los detalles completos de una película por ID.
    /// </summary>
    /// <param name="id">ID de la película.</param>
    /// <returns>A task that represents the asynchronous operation, containing the movie details as <see cref="MovieDto"/>.</returns>
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
        var cacheKey = $"tvsearch_{query.ToLowerInvariant().Replace(" ", "_")}";
        
        var cached = await _redisCache.GetAsync<List<TvShowDto>>(cacheKey);
        if (cached != null) return cached;

        var results = await _tmdbClient.SearchTvShowAsync(query, language: "es-ES");
        var tvShows = results?.Results?.Select(MapSearchTvToDto).ToList() ?? new List<TvShowDto>();

        if (tvShows.Count > 0)
        {
            await _redisCache.SetAsync(cacheKey, tvShows, TimeSpan.FromMinutes(15));
        }

        return tvShows;
    }

    /// <summary>
    /// Obtiene los detalles completos de una serie TV por ID.
    /// </summary>
    /// <param name="id">ID de la serie TV.</param>
    /// <returns>Objeto TvShowDto con los detalles o null si no se encuentra.</returns>
    public async Task<TvShowDto?> GetTvShowDetailsAsync(int id)
    {
        var cacheKey = $"tv_v3_{id}";
        
        var cached = await _redisCache.GetAsync<TvShowDto>(cacheKey);
        if (cached != null) return cached;

        try
        {
            var tvShow = await _tmdbClient.GetTvShowAsync(id, TvShowMethods.Credits | TvShowMethods.Videos);
            if (tvShow == null) return null;

            var dto = MapTvShowToDto(tvShow);
            await _redisCache.SetAsync(cacheKey, dto, TimeSpan.FromHours(2));
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting TV show details for id {Id}: {Msg}", id, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Obtiene los detalles de una temporada de una serie, incluyendo sus episodios.
    /// </summary>
    public async Task<SeasonDto?> GetTvShowSeasonAsync(int tvShowId, int seasonNumber)
    {
        var cacheKey = $"tv_{tvShowId}_season_{seasonNumber}";
        
        var cached = await _redisCache.GetAsync<SeasonDto>(cacheKey);
        if (cached != null) return cached;

        try
        {
            var season = await _tmdbClient.GetTvSeasonAsync(tvShowId, seasonNumber, language: "es-ES");
            if (season == null) return null;

            var dto = new SeasonDto
            {
                Id = (int)season.Id,
                SeasonNumber = season.SeasonNumber,
                Name = season.Name ?? string.Empty,
                Overview = season.Overview ?? string.Empty,
                PosterPath = season.PosterPath,
                EpisodeCount = season.Episodes?.Count ?? 0,
                AirDate = season.AirDate,
                Episodes = season.Episodes?.Select(e => new EpisodeDto
                {
                    Id = (int)e.Id,
                    EpisodeNumber = (int)(e.EpisodeNumber),
                    Name = e.Name ?? string.Empty,
                    Overview = e.Overview ?? string.Empty,
                    StillPath = e.StillPath,
                    AirDate = e.AirDate,
                    VoteAverage = e.VoteAverage
                }).ToList() ?? new List<EpisodeDto>()
            };

            await _redisCache.SetAsync(cacheKey, dto, TimeSpan.FromHours(24));
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting TV season details for id {Id}, season {Season}: {Msg}", tvShowId, seasonNumber, ex.Message);
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
        var cacheKey = $"person_search_{query.ToLowerInvariant().Replace(" ", "_")}";
        
        var cached = await _redisCache.GetAsync<List<PersonDto>>(cacheKey);
        if (cached != null) return cached;

        var results = await _tmdbClient.SearchPersonAsync(query, language: "es-ES");
        var persons = results?.Results?.Select(MapSearchPersonToDto).ToList() ?? new List<PersonDto>();

        if (persons.Count > 0)
        {
            await _redisCache.SetAsync(cacheKey, persons, TimeSpan.FromMinutes(15));
        }

        return persons;
    }

    /// <summary>
    /// Obtiene los detalles completos de una persona por ID.
    /// </summary>
    /// <param name="id">ID de la persona.</param>
    /// <returns>Objeto PersonDto con los detalles o null si no se encuentra.</returns>
    public async Task<PersonDto?> GetPersonDetailsAsync(int id)
    {
        var cacheKey = $"person_{id}";
        
        var cached = await _redisCache.GetAsync<PersonDto>(cacheKey);
        if (cached != null) return cached;

        try
        {
            var person = await _tmdbClient.GetPersonAsync(id, language: "es-ES");
            if (person == null) return null;

            // Fallback biography to English if empty/null in Spanish
            if (string.IsNullOrEmpty(person.Biography))
            {
                try
                {
                    var enPerson = await _tmdbClient.GetPersonAsync(id, language: "en-US");
                    if (enPerson != null && !string.IsNullOrEmpty(enPerson.Biography))
                    {
                        person.Biography = enPerson.Biography;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error fetching English fallback biography for person {Id}: {Msg}", id, ex.Message);
                }
            }

            // Fetch movie and TV credits separately
            TMDbLib.Objects.People.MovieCredits? movieCredits = null;
            TMDbLib.Objects.People.TvCredits? tvCredits = null;
            try
            {
                movieCredits = await _tmdbClient.GetPersonMovieCreditsAsync(id);
                tvCredits = await _tmdbClient.GetPersonTvCreditsAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error fetching credits for person {Id}: {Msg}", id, ex.Message);
            }

            var dto = MapPersonToDto(person, movieCredits, tvCredits);
            await _redisCache.SetAsync(cacheKey, dto, TimeSpan.FromHours(2));
            return dto;
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
        Genres = movie.Genres?.Select(g => new GenreDto { Id = g.Id, Name = g.Name }).ToList() ?? new List<GenreDto>(),
        Cast = movie.Credits?.Cast?.Take(10).Select(c => new CastDto { Id = c.Id, Name = c.Name, Character = c.Character, ProfilePath = c.ProfilePath }).ToList() ?? new List<CastDto>(),
        Director = movie.Credits?.Crew?.FirstOrDefault(c => c.Job == "Director")?.Name,
        Videos = movie.Videos?.Results?.Where(v => v.Site == "YouTube").Select(v => new VideoDto { Id = v.Id, Key = v.Key, Name = v.Name, Site = v.Site, Type = v.Type }).ToList() ?? new List<VideoDto>()
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
        Status = tvShow.Status,
        Cast = tvShow.Credits?.Cast?.Take(10).Select(c => new CastDto { Id = c.Id, Name = c.Name, Character = c.Character, ProfilePath = c.ProfilePath }).ToList() ?? new List<CastDto>(),
        Director = tvShow.CreatedBy?.FirstOrDefault()?.Name ?? tvShow.Credits?.Crew?.FirstOrDefault(c => c.Job == "Executive Producer" || c.Job == "Director")?.Name,
        Videos = tvShow.Videos?.Results?.Where(v => v.Site == "YouTube").Select(v => new VideoDto { Id = v.Id, Key = v.Key, Name = v.Name, Site = v.Site, Type = v.Type }).ToList() ?? new List<VideoDto>(),
        Seasons = tvShow.Seasons?.Select(s => new SeasonDto {
            Id = (int)s.Id,
            SeasonNumber = s.SeasonNumber,
            Name = s.Name ?? string.Empty,
            Overview = s.Overview ?? string.Empty,
            PosterPath = s.PosterPath,
            EpisodeCount = s.EpisodeCount,
            AirDate = s.AirDate
        }).OrderBy(s => s.SeasonNumber).ToList() ?? new List<SeasonDto>()
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
    private static PersonDto MapPersonToDto(Person person, TMDbLib.Objects.People.MovieCredits? movieCredits, TMDbLib.Objects.People.TvCredits? tvCredits)
    {
        var dto = new PersonDto
        {
            Id = person.Id,
            Name = person.Name ?? string.Empty,
            ProfilePath = person.ProfilePath,
            Popularity = person.Popularity,
            KnownForDepartment = person.KnownForDepartment,
            Biography = person.Biography,
            Birthday = person.Birthday,
            PlaceOfBirth = person.PlaceOfBirth,
            Credits = new List<PersonCreditDto>()
        };

        if (movieCredits?.Cast != null)
        {
            dto.Credits.AddRange(movieCredits.Cast.Select(c => new PersonCreditDto
            {
                Id = c.Id,
                Title = c.Title ?? string.Empty,
                PosterPath = c.PosterPath,
                Character = c.Character,
                MediaType = "movie",
                ReleaseDate = c.ReleaseDate
            }));
        }

        if (tvCredits?.Cast != null)
        {
            dto.Credits.AddRange(tvCredits.Cast.Select(c => new PersonCreditDto
            {
                Id = c.Id,
                Title = c.Name ?? string.Empty,
                PosterPath = c.PosterPath,
                Character = c.Character,
                MediaType = "tv",
                ReleaseDate = c.FirstAirDate
            }));
        }

        dto.Credits = dto.Credits
            .OrderByDescending(c => c.ReleaseDate.HasValue)
            .ThenByDescending(c => c.ReleaseDate)
            .ToList();

        return dto;
    }

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
