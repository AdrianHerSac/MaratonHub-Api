using MaratonHub.Api.Common;
using MaratonHub.Api.TheMovieDB.Dtos;
using MaratonHub.Api.TheMovieDB.Repository;
using MaratonHub.Api.TheMovieDB.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MaratonHub.ApiTest.TheMovieDB;

public class TheMovieDBServiceCacheTests
{
    private Mock<IConfiguration> _mockConfig = null!;
    private Mock<IMediaCacheRepository> _mockMediaCache = null!;
    private Mock<IRedisCacheService> _mockRedisCache = null!;
    private Mock<ILogger<TheMovieDBService>> _mockLogger = null!;
    private TheMovieDBService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockMediaCache = new Mock<IMediaCacheRepository>();
        _mockRedisCache = new Mock<IRedisCacheService>();
        _mockLogger = new Mock<ILogger<TheMovieDBService>>();

        var configSection = new Mock<IConfigurationSection>();
        configSection.Setup(s => s.Value).Returns("fake_tmdb_api_key_for_testing");
        _mockConfig.Setup(c => c["TheMovieDB:ApiKey"]).Returns("fake_tmdb_api_key_for_testing");
        _mockConfig.Setup(c => c.GetSection("TheMovieDB:ApiKey")).Returns(configSection.Object);

        _service = new TheMovieDBService(
            _mockConfig.Object,
            _mockMediaCache.Object,
            _mockRedisCache.Object,
            _mockLogger.Object
        );
    }

    // ── Trending Movies: MongoDB cache tests ────────────────────────

    [Test]
    public async Task GetTrendingMoviesAsync_WhenCachedInMongo_ReturnsFromCache_NoTMDBCall()
    {
        var cached = new List<MovieDto> { new() { Id = 1, Title = "Cached Movie" } };
        _mockMediaCache.Setup(c => c.GetCachedMoviesAsync("trending_movies_es_extended"))
            .ReturnsAsync(cached);

        var result = await _service.GetTrendingMoviesAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Title, Is.EqualTo("Cached Movie"));
        _mockMediaCache.Verify(c => c.GetCachedMoviesAsync("trending_movies_es_extended"), Times.Once);
        _mockMediaCache.Verify(c => c.SaveMoviesAsync(It.IsAny<string>(), It.IsAny<List<MovieDto>>()), Times.Never);
    }

    [Test]
    public async Task GetTrendingMoviesAsync_WhenMongoDbThrows_LogsWarning_AndCallsTMDB()
    {
        _mockMediaCache.Setup(c => c.GetCachedMoviesAsync("trending_movies_es_extended"))
            .ThrowsAsync(new Exception("MongoDB connection failed"));

        var result = await _service.GetTrendingMoviesAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0));
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MongoDB not available")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetTrendingMoviesAsync_WhenSaveFails_LogsWarning_ReturnsResults()
    {
        _mockMediaCache.Setup(c => c.GetCachedMoviesAsync("trending_movies_es_extended"))
            .ReturnsAsync((List<MovieDto>?)null);
        _mockMediaCache.Setup(c => c.SaveMoviesAsync("trending_movies_es_extended", It.IsAny<List<MovieDto>>()))
            .ThrowsAsync(new Exception("MongoDB write failed"));

        var result = await _service.GetTrendingMoviesAsync();

        Assert.That(result, Is.Not.Null);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MongoDB cache write failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── Popular Movies: MongoDB cache tests ─────────────────────────

    [Test]
    public async Task GetPopularMoviesAsync_WhenCachedInMongo_ReturnsFromCache()
    {
        var cached = new List<MovieDto> { new() { Id = 2, Title = "Popular Movie" } };
        _mockMediaCache.Setup(c => c.GetCachedMoviesAsync("popular_movies_es_extended"))
            .ReturnsAsync(cached);

        var result = await _service.GetPopularMoviesAsync();

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Title, Is.EqualTo("Popular Movie"));
        _mockMediaCache.Verify(c => c.SaveMoviesAsync(It.IsAny<string>(), It.IsAny<List<MovieDto>>()), Times.Never);
    }

    [Test]
    public async Task GetPopularMoviesAsync_WhenMongoDbDown_ReturnsEmptyList()
    {
        _mockMediaCache.Setup(c => c.GetCachedMoviesAsync("popular_movies_es_extended"))
            .ThrowsAsync(new Exception("Connection refused"));

        var result = await _service.GetPopularMoviesAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task GetPopularMoviesAsync_WhenNotCached_SavesToCache()
    {
        _mockMediaCache.Setup(c => c.GetCachedMoviesAsync("popular_movies_es_extended"))
            .ReturnsAsync((List<MovieDto>?)null);
        _mockMediaCache.Setup(c => c.SaveMoviesAsync("popular_movies_es_extended", It.IsAny<List<MovieDto>>()))
            .Returns(Task.CompletedTask);

        var result = await _service.GetPopularMoviesAsync();

        _mockMediaCache.Verify(c => c.SaveMoviesAsync("popular_movies_es_extended", It.IsAny<List<MovieDto>>()), Times.Once);
    }

    // ── Trending TV Shows: MongoDB cache tests ──────────────────────

    [Test]
    public async Task GetTrendingTvShowsAsync_WhenCachedInMongo_ReturnsFromCache()
    {
        var cached = new List<TvShowDto> { new() { Id = 10, Name = "Cached Show" } };
        _mockMediaCache.Setup(c => c.GetCachedTvShowsAsync("trending_tv_es"))
            .ReturnsAsync(cached);

        var result = await _service.GetTrendingTvShowsAsync();

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Cached Show"));
        _mockMediaCache.Verify(c => c.SaveTvShowsAsync(It.IsAny<string>(), It.IsAny<List<TvShowDto>>()), Times.Never);
    }

    [Test]
    public async Task GetTrendingTvShowsAsync_WhenNotCached_SavesToCache()
    {
        _mockMediaCache.Setup(c => c.GetCachedTvShowsAsync("trending_tv_es"))
            .ReturnsAsync((List<TvShowDto>?)null);
        _mockMediaCache.Setup(c => c.SaveTvShowsAsync("trending_tv_es", It.IsAny<List<TvShowDto>>()))
            .Returns(Task.CompletedTask);

        await _service.GetTrendingTvShowsAsync();

        _mockMediaCache.Verify(c => c.SaveTvShowsAsync("trending_tv_es", It.IsAny<List<TvShowDto>>()), Times.Once);
    }

    // ── Popular TV Shows: MongoDB cache tests ───────────────────────

    [Test]
    public async Task GetPopularTvShowsAsync_WhenCachedInMongo_ReturnsFromCache()
    {
        var cached = new List<TvShowDto> { new() { Id = 20, Name = "Popular Show" } };
        _mockMediaCache.Setup(c => c.GetCachedTvShowsAsync("popular_tv_es"))
            .ReturnsAsync(cached);

        var result = await _service.GetPopularTvShowsAsync();

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Popular Show"));
    }

    [Test]
    public async Task GetPopularTvShowsAsync_WhenMongoDbDown_ReturnsEmptyList()
    {
        _mockMediaCache.Setup(c => c.GetCachedTvShowsAsync("popular_tv_es"))
            .ThrowsAsync(new Exception("Timeout"));

        var result = await _service.GetPopularTvShowsAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0));
    }

    // ── Popular Persons: MongoDB cache tests ────────────────────────

    [Test]
    public async Task GetPopularPersonsAsync_WhenCachedInMongo_ReturnsFromCache()
    {
        var cached = new List<PersonDto> { new() { Id = 100, Name = "Cached Actor" } };
        _mockMediaCache.Setup(c => c.GetCachedPersonsAsync("popular_persons_es"))
            .ReturnsAsync(cached);

        var result = await _service.GetPopularPersonsAsync();

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Cached Actor"));
        _mockMediaCache.Verify(c => c.SavePersonsAsync(It.IsAny<string>(), It.IsAny<List<PersonDto>>()), Times.Never);
    }

    [Test]
    public async Task GetPopularPersonsAsync_WhenNotCached_SavesToCache()
    {
        _mockMediaCache.Setup(c => c.GetCachedPersonsAsync("popular_persons_es"))
            .ReturnsAsync((List<PersonDto>?)null);
        _mockMediaCache.Setup(c => c.SavePersonsAsync("popular_persons_es", It.IsAny<List<PersonDto>>()))
            .Returns(Task.CompletedTask);

        await _service.GetPopularPersonsAsync();

        _mockMediaCache.Verify(c => c.SavePersonsAsync("popular_persons_es", It.IsAny<List<PersonDto>>()), Times.Once);
    }

    [Test]
    public async Task GetPopularPersonsAsync_WhenMongoDbDown_ReturnsEmptyList()
    {
        _mockMediaCache.Setup(c => c.GetCachedPersonsAsync("popular_persons_es"))
            .ThrowsAsync(new Exception("MongoDB offline"));

        var result = await _service.GetPopularPersonsAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0));
    }

    // ── Movies by Genre: MongoDB cache tests ────────────────────────

    [Test]
    public async Task GetMoviesByGenreAsync_WhenCachedInMongo_ReturnsFromCache()
    {
        var cached = new List<MovieDto> { new() { Id = 30, Title = "Genre Movie" } };
        _mockMediaCache.Setup(c => c.GetCachedMoviesAsync("genre_28_movies_es_extended"))
            .ReturnsAsync(cached);

        var result = await _service.GetMoviesByGenreAsync(28);

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Title, Is.EqualTo("Genre Movie"));
    }

    [Test]
    public async Task GetMoviesByGenreAsync_WhenMongoDbThrows_LogsWarning()
    {
        _mockMediaCache.Setup(c => c.GetCachedMoviesAsync("genre_28_movies_es_extended"))
            .ThrowsAsync(new Exception("Mongo error"));

        var result = await _service.GetMoviesByGenreAsync(28);

        Assert.That(result, Is.Not.Null);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("cache read failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── Search Movies: Redis cache tests ────────────────────────────

    [Test]
    public async Task SearchMoviesAsync_WhenCachedInRedis_ReturnsFromRedis()
    {
        var cached = new List<MovieDto> { new() { Id = 5, Title = "Redis Cached" } };
        _mockRedisCache.Setup(r => r.GetAsync<List<MovieDto>>("movie_search_inception"))
            .ReturnsAsync(cached);

        var result = await _service.SearchMoviesAsync("Inception");

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Title, Is.EqualTo("Redis Cached"));
        _mockRedisCache.Verify(r => r.GetAsync<List<MovieDto>>("movie_search_inception"), Times.Once);
    }

    [Test]
    public async Task SearchMoviesAsync_WhenNotCached_ReturnsEmptyList()
    {
        _mockRedisCache.Setup(r => r.GetAsync<List<MovieDto>>("movie_search_inception"))
            .ReturnsAsync((List<MovieDto>?)null);

        var result = await _service.SearchMoviesAsync("Inception");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0));
        // Note: TMDB call would fail in test, should not throw
    }

    [Test]
    public async Task SearchMoviesAsync_CacheKey_UsesLowercaseAndUnderscores()
    {
        _mockRedisCache.Setup(r => r.GetAsync<List<MovieDto>>("movie_search_the_dark_knight"))
            .ReturnsAsync((List<MovieDto>?)null);

        await _service.SearchMoviesAsync("The Dark Knight");

        _mockRedisCache.Verify(r => r.GetAsync<List<MovieDto>>("movie_search_the_dark_knight"), Times.Once);
    }

    // ── Search TV Shows: Redis cache tests ──────────────────────────

    [Test]
    public async Task SearchTvShowsAsync_WhenCachedInRedis_ReturnsFromRedis()
    {
        var cached = new List<TvShowDto> { new() { Id = 50, Name = "Cached Show" } };
        _mockRedisCache.Setup(r => r.GetAsync<List<TvShowDto>>("tvsearch_stranger"))
            .ReturnsAsync(cached);

        var result = await _service.SearchTvShowsAsync("Stranger");

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Cached Show"));
    }

    [Test]
    public async Task SearchTvShowsAsync_WhenNotCached_ReturnsEmptyList()
    {
        _mockRedisCache.Setup(r => r.GetAsync<List<TvShowDto>>("tvsearch_stranger"))
            .ReturnsAsync((List<TvShowDto>?)null);

        var result = await _service.SearchTvShowsAsync("Stranger");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0));
    }

    // ── Search Persons: Redis cache tests ───────────────────────────

    [Test]
    public async Task SearchPersonsAsync_WhenCachedInRedis_ReturnsFromRedis()
    {
        var cached = new List<PersonDto> { new() { Id = 200, Name = "Tom Hanks" } };
        _mockRedisCache.Setup(r => r.GetAsync<List<PersonDto>>("person_search_tom_hanks"))
            .ReturnsAsync(cached);

        var result = await _service.SearchPersonsAsync("Tom Hanks");

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Tom Hanks"));
    }

    [Test]
    public async Task SearchPersonsAsync_WhenNotCached_ReturnsEmptyList()
    {
        _mockRedisCache.Setup(r => r.GetAsync<List<PersonDto>>("person_search_tom_hanks"))
            .ReturnsAsync((List<PersonDto>?)null);

        var result = await _service.SearchPersonsAsync("Tom Hanks");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0));
    }

    // ── Movie Details: Redis cache tests ────────────────────────────

    [Test]
    public async Task GetMovieDetailsAsync_WhenCachedInRedis_ReturnsFromRedis()
    {
        var cached = new MovieDto { Id = 100, Title = "Cached Detail" };
        _mockRedisCache.Setup(r => r.GetAsync<MovieDto>("movie_v3_100"))
            .ReturnsAsync(cached);

        var result = await _service.GetMovieDetailsAsync(100);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Title, Is.EqualTo("Cached Detail"));
        _mockRedisCache.Verify(r => r.GetAsync<MovieDto>("movie_v3_100"), Times.Once);
    }

    [Test]
    public async Task GetMovieDetailsAsync_WhenNotCached_ReturnsNull()
    {
        _mockRedisCache.Setup(r => r.GetAsync<MovieDto>("movie_v3_999"))
            .ReturnsAsync((MovieDto?)null);

        var result = await _service.GetMovieDetailsAsync(999);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetMovieDetailsAsync_CacheKey_UsesMoviePrefixAndVersion()
    {
        _mockRedisCache.Setup(r => r.GetAsync<MovieDto>("movie_v3_550"))
            .ReturnsAsync((MovieDto?)null);

        await _service.GetMovieDetailsAsync(550);

        _mockRedisCache.Verify(r => r.GetAsync<MovieDto>("movie_v3_550"), Times.Once);
    }

    // ── TV Show Details: Redis cache tests ──────────────────────────

    [Test]
    public async Task GetTvShowDetailsAsync_WhenCachedInRedis_ReturnsFromRedis()
    {
        var cached = new TvShowDto { Id = 200, Name = "Cached TV Detail" };
        _mockRedisCache.Setup(r => r.GetAsync<TvShowDto>("tv_v3_200"))
            .ReturnsAsync(cached);

        var result = await _service.GetTvShowDetailsAsync(200);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Cached TV Detail"));
    }

    [Test]
    public async Task GetTvShowDetailsAsync_WhenNotCached_ReturnsNull()
    {
        _mockRedisCache.Setup(r => r.GetAsync<TvShowDto>("tv_v3_999"))
            .ReturnsAsync((TvShowDto?)null);

        var result = await _service.GetTvShowDetailsAsync(999);

        Assert.That(result, Is.Null);
    }

    // ── TV Season Details: Redis cache tests ────────────────────────

    [Test]
    public async Task GetTvShowSeasonAsync_WhenCachedInRedis_ReturnsFromRedis()
    {
        var cached = new SeasonDto { Id = 300, SeasonNumber = 1, Name = "Season 1" };
        _mockRedisCache.Setup(r => r.GetAsync<SeasonDto>("tv_200_season_1"))
            .ReturnsAsync(cached);

        var result = await _service.GetTvShowSeasonAsync(200, 1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.SeasonNumber, Is.EqualTo(1));
    }

    [Test]
    public async Task GetTvShowSeasonAsync_WhenNotCached_ReturnsNull()
    {
        _mockRedisCache.Setup(r => r.GetAsync<SeasonDto>("tv_999_season_5"))
            .ReturnsAsync((SeasonDto?)null);

        var result = await _service.GetTvShowSeasonAsync(999, 5);

        Assert.That(result, Is.Null);
    }

    // ── Person Details: Redis cache tests ───────────────────────────

    [Test]
    public async Task GetPersonDetailsAsync_WhenCachedInRedis_ReturnsFromRedis()
    {
        var cached = new PersonDto { Id = 500, Name = "Cached Person" };
        _mockRedisCache.Setup(r => r.GetAsync<PersonDto>("person_500"))
            .ReturnsAsync(cached);

        var result = await _service.GetPersonDetailsAsync(500);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Cached Person"));
    }

    [Test]
    public async Task GetPersonDetailsAsync_WhenNotCached_ReturnsNull()
    {
        _mockRedisCache.Setup(r => r.GetAsync<PersonDto>("person_999"))
            .ReturnsAsync((PersonDto?)null);

        var result = await _service.GetPersonDetailsAsync(999);

        Assert.That(result, Is.Null);
    }

    // ── Changed IDs tests ───────────────────────────────────────────

    [Test]
    public async Task GetChangedMovieIdsAsync_WhenTMDBThrows_ReturnsEmptyList()
    {
        var result = await _service.GetChangedMovieIdsAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task GetChangedTvShowIdsAsync_WhenTMDBThrows_ReturnsEmptyList()
    {
        var result = await _service.GetChangedTvShowIdsAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0));
    }

    // ── Constructor tests ───────────────────────────────────────────

    [Test]
    public void Constructor_WhenApiKeyMissing_ThrowsInvalidOperationException()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["TheMovieDB:ApiKey"]).Returns((string?)null);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new TheMovieDBService(config.Object, _mockMediaCache.Object, _mockRedisCache.Object, _mockLogger.Object));

        Assert.That(ex!.Message, Does.Contain("TMDb API Key not configured"));
    }

    // ── GetTrendingMoviesAsync: TMDB error path ─────────────────────

    [Test]
    public async Task GetTrendingMoviesAsync_WhenNotCached_TMDBFails_LogsError()
    {
        _mockMediaCache.Setup(c => c.GetCachedMoviesAsync("trending_movies_es_extended"))
            .ReturnsAsync((List<MovieDto>?)null);

        var result = await _service.GetTrendingMoviesAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0));
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error fetching trending movies")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetTrendingMoviesAsync_WhenNotCached_SavesEmptyListToCache()
    {
        _mockMediaCache.Setup(c => c.GetCachedMoviesAsync("trending_movies_es_extended"))
            .ReturnsAsync((List<MovieDto>?)null);
        _mockMediaCache.Setup(c => c.SaveMoviesAsync("trending_movies_es_extended", It.IsAny<List<MovieDto>>()))
            .Returns(Task.CompletedTask);

        await _service.GetTrendingMoviesAsync();

        _mockMediaCache.Verify(c => c.SaveMoviesAsync("trending_movies_es_extended", It.IsAny<List<MovieDto>>()), Times.Once);
    }

    // ── GetPopularMoviesAsync: additional paths ─────────────────────

    [Test]
    public async Task GetPopularMoviesAsync_WhenNotCached_TMDBFails_LogsError()
    {
        _mockMediaCache.Setup(c => c.GetCachedMoviesAsync("popular_movies_es_extended"))
            .ReturnsAsync((List<MovieDto>?)null);

        var result = await _service.GetPopularMoviesAsync();

        Assert.That(result.Count, Is.EqualTo(0));
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error fetching popular movies")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetPopularMoviesAsync_WhenSaveFails_LogsWarning()
    {
        _mockMediaCache.Setup(c => c.GetCachedMoviesAsync("popular_movies_es_extended"))
            .ReturnsAsync((List<MovieDto>?)null);
        _mockMediaCache.Setup(c => c.SaveMoviesAsync("popular_movies_es_extended", It.IsAny<List<MovieDto>>()))
            .ThrowsAsync(new Exception("MongoDB write failed"));

        var result = await _service.GetPopularMoviesAsync();

        Assert.That(result, Is.Not.Null);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MongoDB cache write failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── GetTrendingTvShowsAsync: additional paths ───────────────────

    [Test]
    public async Task GetTrendingTvShowsAsync_WhenMongoDbDown_LogsWarning()
    {
        _mockMediaCache.Setup(c => c.GetCachedTvShowsAsync("trending_tv_es"))
            .ThrowsAsync(new Exception("Connection refused"));

        var result = await _service.GetTrendingTvShowsAsync();

        Assert.That(result, Is.Not.Null);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MongoDB not available")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetTrendingTvShowsAsync_WhenSaveFails_LogsWarning()
    {
        _mockMediaCache.Setup(c => c.GetCachedTvShowsAsync("trending_tv_es"))
            .ReturnsAsync((List<TvShowDto>?)null);
        _mockMediaCache.Setup(c => c.SaveTvShowsAsync("trending_tv_es", It.IsAny<List<TvShowDto>>()))
            .ThrowsAsync(new Exception("MongoDB write failed"));

        var result = await _service.GetTrendingTvShowsAsync();

        Assert.That(result, Is.Not.Null);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MongoDB cache write failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetTrendingTvShowsAsync_WhenNotCached_TMDBFails_LogsError()
    {
        _mockMediaCache.Setup(c => c.GetCachedTvShowsAsync("trending_tv_es"))
            .ReturnsAsync((List<TvShowDto>?)null);

        var result = await _service.GetTrendingTvShowsAsync();

        Assert.That(result.Count, Is.EqualTo(0));
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error fetching trending TV shows")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── GetPopularTvShowsAsync: additional paths ────────────────────

    [Test]
    public async Task GetPopularTvShowsAsync_WhenNotCached_SavesToCache()
    {
        _mockMediaCache.Setup(c => c.GetCachedTvShowsAsync("popular_tv_es"))
            .ReturnsAsync((List<TvShowDto>?)null);
        _mockMediaCache.Setup(c => c.SaveTvShowsAsync("popular_tv_es", It.IsAny<List<TvShowDto>>()))
            .Returns(Task.CompletedTask);

        await _service.GetPopularTvShowsAsync();

        _mockMediaCache.Verify(c => c.SaveTvShowsAsync("popular_tv_es", It.IsAny<List<TvShowDto>>()), Times.Once);
    }

    [Test]
    public async Task GetPopularTvShowsAsync_WhenSaveFails_LogsWarning()
    {
        _mockMediaCache.Setup(c => c.GetCachedTvShowsAsync("popular_tv_es"))
            .ReturnsAsync((List<TvShowDto>?)null);
        _mockMediaCache.Setup(c => c.SaveTvShowsAsync("popular_tv_es", It.IsAny<List<TvShowDto>>()))
            .ThrowsAsync(new Exception("MongoDB write failed"));

        var result = await _service.GetPopularTvShowsAsync();

        Assert.That(result, Is.Not.Null);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MongoDB cache write failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetPopularTvShowsAsync_WhenNotCached_TMDBFails_LogsError()
    {
        _mockMediaCache.Setup(c => c.GetCachedTvShowsAsync("popular_tv_es"))
            .ReturnsAsync((List<TvShowDto>?)null);

        var result = await _service.GetPopularTvShowsAsync();

        Assert.That(result.Count, Is.EqualTo(0));
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error fetching popular TV shows")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── GetPopularPersonsAsync: additional paths ────────────────────

    [Test]
    public async Task GetPopularPersonsAsync_WhenSaveFails_LogsWarning()
    {
        _mockMediaCache.Setup(c => c.GetCachedPersonsAsync("popular_persons_es"))
            .ReturnsAsync((List<PersonDto>?)null);
        _mockMediaCache.Setup(c => c.SavePersonsAsync("popular_persons_es", It.IsAny<List<PersonDto>>()))
            .ThrowsAsync(new Exception("MongoDB write failed"));

        var result = await _service.GetPopularPersonsAsync();

        Assert.That(result, Is.Not.Null);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MongoDB cache write failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetPopularPersonsAsync_WhenNotCached_TMDBFails_LogsError()
    {
        _mockMediaCache.Setup(c => c.GetCachedPersonsAsync("popular_persons_es"))
            .ReturnsAsync((List<PersonDto>?)null);

        var result = await _service.GetPopularPersonsAsync();

        Assert.That(result.Count, Is.EqualTo(0));
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error fetching popular persons")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── GetMoviesByGenreAsync: additional paths ─────────────────────

    [Test]
    public async Task GetMoviesByGenreAsync_WhenNotCached_SavesToCache()
    {
        _mockMediaCache.Setup(c => c.GetCachedMoviesAsync("genre_28_movies_es_extended"))
            .ReturnsAsync((List<MovieDto>?)null);
        _mockMediaCache.Setup(c => c.SaveMoviesAsync("genre_28_movies_es_extended", It.IsAny<List<MovieDto>>()))
            .Returns(Task.CompletedTask);

        await _service.GetMoviesByGenreAsync(28);

        _mockMediaCache.Verify(c => c.SaveMoviesAsync("genre_28_movies_es_extended", It.IsAny<List<MovieDto>>()), Times.Once);
    }

    [Test]
    public async Task GetMoviesByGenreAsync_WhenSaveFails_LogsWarning()
    {
        _mockMediaCache.Setup(c => c.GetCachedMoviesAsync("genre_28_movies_es_extended"))
            .ReturnsAsync((List<MovieDto>?)null);
        _mockMediaCache.Setup(c => c.SaveMoviesAsync("genre_28_movies_es_extended", It.IsAny<List<MovieDto>>()))
            .ThrowsAsync(new Exception("MongoDB write failed"));

        var result = await _service.GetMoviesByGenreAsync(28);

        Assert.That(result, Is.Not.Null);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MongoDB cache write failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetMoviesByGenreAsync_WhenNotCached_TMDBFails_LogsError()
    {
        _mockMediaCache.Setup(c => c.GetCachedMoviesAsync("genre_99_movies_es_extended"))
            .ReturnsAsync((List<MovieDto>?)null);

        var result = await _service.GetMoviesByGenreAsync(99);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0));
    }

    // ── SearchMoviesAsync: additional paths ──────────────────────────

    [Test]
    public async Task SearchMoviesAsync_WhenNotCached_TMDBFails_LogsError()
    {
        _mockRedisCache.Setup(r => r.GetAsync<List<MovieDto>>("movie_search_test"))
            .ReturnsAsync((List<MovieDto>?)null);

        var result = await _service.SearchMoviesAsync("Test");

        Assert.That(result.Count, Is.EqualTo(0));
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error searching movies")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SearchMoviesAsync_WhenNotCached_DoesNotSaveEmptyListToRedis()
    {
        _mockRedisCache.Setup(r => r.GetAsync<List<MovieDto>>("movie_search_noresults"))
            .ReturnsAsync((List<MovieDto>?)null);

        await _service.SearchMoviesAsync("NoResults");

        _mockRedisCache.Verify(r => r.SetAsync(It.IsAny<string>(), It.IsAny<List<MovieDto>>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    // ── SearchTvShowsAsync: additional paths ────────────────────────

    [Test]
    public async Task SearchTvShowsAsync_CacheKey_UsesLowercaseAndUnderscores()
    {
        _mockRedisCache.Setup(r => r.GetAsync<List<TvShowDto>>("tvsearch_breaking_bad"))
            .ReturnsAsync((List<TvShowDto>?)null);

        await _service.SearchTvShowsAsync("Breaking Bad");

        _mockRedisCache.Verify(r => r.GetAsync<List<TvShowDto>>("tvsearch_breaking_bad"), Times.Once);
    }

    [Test]
    public async Task SearchTvShowsAsync_WhenNotCached_TMDBFails_LogsError()
    {
        _mockRedisCache.Setup(r => r.GetAsync<List<TvShowDto>>("tvsearch_test"))
            .ReturnsAsync((List<TvShowDto>?)null);

        var result = await _service.SearchTvShowsAsync("Test");

        Assert.That(result.Count, Is.EqualTo(0));
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error searching TV shows")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── SearchPersonsAsync: additional paths ────────────────────────

    [Test]
    public async Task SearchPersonsAsync_CacheKey_UsesLowercaseAndUnderscores()
    {
        _mockRedisCache.Setup(r => r.GetAsync<List<PersonDto>>("person_search_brad_pitt"))
            .ReturnsAsync((List<PersonDto>?)null);

        await _service.SearchPersonsAsync("Brad Pitt");

        _mockRedisCache.Verify(r => r.GetAsync<List<PersonDto>>("person_search_brad_pitt"), Times.Once);
    }

    [Test]
    public async Task SearchPersonsAsync_WhenNotCached_TMDBFails_LogsError()
    {
        _mockRedisCache.Setup(r => r.GetAsync<List<PersonDto>>("person_search_test"))
            .ReturnsAsync((List<PersonDto>?)null);

        var result = await _service.SearchPersonsAsync("Test");

        Assert.That(result.Count, Is.EqualTo(0));
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error searching persons")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── GetMovieDetailsAsync: additional paths ──────────────────────

    [Test]
    public async Task GetMovieDetailsAsync_WhenNotCached_TMDBFails_LogsError()
    {
        _mockRedisCache.Setup(r => r.GetAsync<MovieDto>("movie_v3_888"))
            .ReturnsAsync((MovieDto?)null);

        var result = await _service.GetMovieDetailsAsync(888);

        Assert.That(result, Is.Null);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error getting movie details")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── GetTvShowDetailsAsync: additional paths ─────────────────────

    [Test]
    public async Task GetTvShowDetailsAsync_WhenNotCached_TMDBFails_LogsError()
    {
        _mockRedisCache.Setup(r => r.GetAsync<TvShowDto>("tv_v3_888"))
            .ReturnsAsync((TvShowDto?)null);

        var result = await _service.GetTvShowDetailsAsync(888);

        Assert.That(result, Is.Null);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error getting TV show details")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetTvShowDetailsAsync_CacheKey_UsesTvPrefixAndVersion()
    {
        _mockRedisCache.Setup(r => r.GetAsync<TvShowDto>("tv_v3_550"))
            .ReturnsAsync((TvShowDto?)null);

        await _service.GetTvShowDetailsAsync(550);

        _mockRedisCache.Verify(r => r.GetAsync<TvShowDto>("tv_v3_550"), Times.Once);
    }

    // ── GetTvShowSeasonAsync: additional paths ──────────────────────

    [Test]
    public async Task GetTvShowSeasonAsync_WhenNotCached_TMDBFails_LogsError()
    {
        _mockRedisCache.Setup(r => r.GetAsync<SeasonDto>("tv_888_season_1"))
            .ReturnsAsync((SeasonDto?)null);

        var result = await _service.GetTvShowSeasonAsync(888, 1);

        Assert.That(result, Is.Null);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error getting TV season details")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetTvShowSeasonAsync_CacheKey_UsesTvIdAndSeasonNumber()
    {
        _mockRedisCache.Setup(r => r.GetAsync<SeasonDto>("tv_100_season_3"))
            .ReturnsAsync((SeasonDto?)null);

        await _service.GetTvShowSeasonAsync(100, 3);

        _mockRedisCache.Verify(r => r.GetAsync<SeasonDto>("tv_100_season_3"), Times.Once);
    }

    // ── GetPersonDetailsAsync: additional paths ─────────────────────

    [Test]
    public async Task GetPersonDetailsAsync_WhenNotCached_TMDBFails_LogsError()
    {
        _mockRedisCache.Setup(r => r.GetAsync<PersonDto>("person_888"))
            .ReturnsAsync((PersonDto?)null);

        var result = await _service.GetPersonDetailsAsync(888);

        Assert.That(result, Is.Null);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error getting person details")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetPersonDetailsAsync_CacheKey_UsesPersonPrefix()
    {
        _mockRedisCache.Setup(r => r.GetAsync<PersonDto>("person_42"))
            .ReturnsAsync((PersonDto?)null);

        await _service.GetPersonDetailsAsync(42);

        _mockRedisCache.Verify(r => r.GetAsync<PersonDto>("person_42"), Times.Once);
    }

    // ── GetChangedMovieIdsAsync / GetChangedTvShowIdsAsync ──────────

    [Test]
    public async Task GetChangedMovieIdsAsync_TMDBFails_LogsError()
    {
        var result = await _service.GetChangedMovieIdsAsync();

        Assert.That(result.Count, Is.EqualTo(0));
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetChangedTvShowIdsAsync_TMDBFails_LogsError()
    {
        var result = await _service.GetChangedTvShowIdsAsync();

        Assert.That(result.Count, Is.EqualTo(0));
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
