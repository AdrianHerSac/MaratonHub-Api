using MaratonHub.Api.TheMovieDB.Dtos;
using MaratonHub.Api.TheMovieDB.Models;
using MongoDB.Driver;

namespace MaratonHub.Api.TheMovieDB.Repository;

public class MediaCacheRepository : IMediaCacheRepository
{
    private readonly IMongoCollection<CachedMovie> _movieCache;
    private readonly IMongoCollection<CachedTvShow> _tvCache;
    private readonly IMongoCollection<CachedPerson> _personCache;

    // Los datos de trending/popular de TMDb cambian a diario — 1 hora es un buen balance
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(1);

    public MediaCacheRepository(IMongoDatabase database)
    {
        _movieCache  = database.GetCollection<CachedMovie>("cached_movies");
        _tvCache     = database.GetCollection<CachedTvShow>("cached_tvshows");
        _personCache = database.GetCollection<CachedPerson>("cached_persons");
    }

    // ─── Películas ─────────────────────────────────────────────────────────

    public async Task<List<MovieDto>?> GetCachedMoviesAsync(string cacheKey)
    {
        var cached = await _movieCache
            .Find(c => c.CacheKey == cacheKey)
            .FirstOrDefaultAsync();

        if (cached == null) return null;
        if (DateTime.UtcNow - cached.CachedAt > CacheExpiry) return null;

        return cached.Movies;
    }

    /// <summary>
    /// Guarda una lista de películas en la caché usando la clave de caché especificada.
    /// Si ya existe una entrada de caché con la clave dada, se actualizará con los nuevos datos.
    /// Si la entrada no existe, se creará una nueva.
    /// </summary>
    /// <param name="cacheKey">La clave que identifica la entrada de caché donde se almacenarán las películas.</param>
    /// <param name="movies">La lista de películas a almacenar en caché. Cada película contiene detalles como título, fecha de estreno y géneros.</param>
    public async Task SaveMoviesAsync(string cacheKey, List<MovieDto> movies)
    {
        var filter = Builders<CachedMovie>.Filter.Eq(c => c.CacheKey, cacheKey);
        var update = Builders<CachedMovie>.Update
            .Set(c => c.CacheKey, cacheKey)
            .Set(c => c.CachedAt, DateTime.UtcNow)
            .Set(c => c.Movies, movies);

        await _movieCache.UpdateOneAsync(
            filter,
            update,
            new UpdateOptions { IsUpsert = true }
        );
    }

    // ─── Series de TV ──────────────────────────────────────────────────────

    public async Task<List<TvShowDto>?> GetCachedTvShowsAsync(string cacheKey)
    {
        var cached = await _tvCache
            .Find(c => c.CacheKey == cacheKey)
            .FirstOrDefaultAsync();

        if (cached == null) return null;
        if (DateTime.UtcNow - cached.CachedAt > CacheExpiry) return null;

        return cached.TvShows;
    }

    public async Task SaveTvShowsAsync(string cacheKey, List<TvShowDto> tvShows)
    {
        var filter = Builders<CachedTvShow>.Filter.Eq(c => c.CacheKey, cacheKey);
        var update = Builders<CachedTvShow>.Update
            .Set(c => c.CacheKey, cacheKey)
            .Set(c => c.CachedAt, DateTime.UtcNow)
            .Set(c => c.TvShows, tvShows);

        await _tvCache.UpdateOneAsync(
            filter,
            update,
            new UpdateOptions { IsUpsert = true }
        );
    }

    // ─── Personas ──────────────────────────────────────────────────────────

    public async Task<List<PersonDto>?> GetCachedPersonsAsync(string cacheKey)
    {
        var cached = await _personCache
            .Find(c => c.CacheKey == cacheKey)
            .FirstOrDefaultAsync();

        if (cached == null) return null;
        if (DateTime.UtcNow - cached.CachedAt > CacheExpiry) return null;

        return cached.Persons;
    }

    public async Task SavePersonsAsync(string cacheKey, List<PersonDto> persons)
    {
        var filter = Builders<CachedPerson>.Filter.Eq(c => c.CacheKey, cacheKey);
        var update = Builders<CachedPerson>.Update
            .Set(c => c.CacheKey, cacheKey)
            .Set(c => c.CachedAt, DateTime.UtcNow)
            .Set(c => c.Persons, persons);

        await _personCache.UpdateOneAsync(
            filter,
            update,
            new UpdateOptions { IsUpsert = true }
        );
    }
}
