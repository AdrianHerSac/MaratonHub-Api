using MaratonHub.Api.TheMovieDB.Dtos;
using MaratonHub.Api.TheMovieDB.Models;
using MaratonHub.Api.TheMovieDB.Repository;
using MongoDB.Driver;
using Moq;

namespace MaratonHub.ApiTest.TheMovieDB;

public class MediaCacheRepositoryTests
{
    private Mock<IMongoCollection<CachedMovie>> _mockMovieCollection = null!;
    private Mock<IMongoCollection<CachedTvShow>> _mockTvCollection = null!;
    private Mock<IMongoCollection<CachedPerson>> _mockPersonCollection = null!;
    private Mock<IMongoDatabase> _mockDb = null!;
    private MediaCacheRepository _repo = null!;

    [SetUp]
    public void SetUp()
    {
        _mockMovieCollection = new Mock<IMongoCollection<CachedMovie>>();
        _mockTvCollection = new Mock<IMongoCollection<CachedTvShow>>();
        _mockPersonCollection = new Mock<IMongoCollection<CachedPerson>>();
        _mockDb = new Mock<IMongoDatabase>();

        _mockDb.Setup(d => d.GetCollection<CachedMovie>("cached_movies", null))
            .Returns(_mockMovieCollection.Object);
        _mockDb.Setup(d => d.GetCollection<CachedTvShow>("cached_tvshows", null))
            .Returns(_mockTvCollection.Object);
        _mockDb.Setup(d => d.GetCollection<CachedPerson>("cached_persons", null))
            .Returns(_mockPersonCollection.Object);

        _repo = new MediaCacheRepository(_mockDb.Object);
    }


    [Test]
    public void MediaCacheRepository_ShouldImplementIMediaCacheRepository()
    {
        var type = typeof(MediaCacheRepository);
        Assert.That(type.GetInterface(nameof(IMediaCacheRepository)), Is.Not.Null);
    }

    [Test]
    public void MediaCacheRepository_ShouldHaveConstructorWithIMongoDatabase()
    {
        var constructors = typeof(MediaCacheRepository).GetConstructors();
        Assert.That(constructors.Length, Is.EqualTo(1));

        var constructor = constructors[0];
        var parameters = constructor.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(1));
        Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(IMongoDatabase)));
    }

    // ── GetCachedMoviesAsync ────────────────────────────────────────

    [Test]
    public async Task GetCachedMoviesAsync_WhenNoCacheExists_ReturnsNull()
    {
        var cursorMock = new Mock<IAsyncCursor<CachedMovie>>();
        cursorMock.Setup(c => c.Current).Returns(new List<CachedMovie>());
        cursorMock.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // No documents

        _mockMovieCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<CachedMovie>>(),
                It.IsAny<FindOptions<CachedMovie, CachedMovie>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorMock.Object);

        var result = await _repo.GetCachedMoviesAsync("nonexistent_key");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetCachedMoviesAsync_WhenCacheIsFresh_ReturnsMovies()
    {
        var cachedMovie = new CachedMovie
        {
            CacheKey = "test_key",
            CachedAt = DateTime.UtcNow,
            Movies = new List<MovieDto> { new() { Id = 1, Title = "Fresh Movie" } }
        };

        var cursorMock = new Mock<IAsyncCursor<CachedMovie>>();
        cursorMock.Setup(c => c.Current).Returns(new List<CachedMovie> { cachedMovie });
        cursorMock.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true) // Has document
            .ReturnsAsync(false);

        _mockMovieCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<CachedMovie>>(),
                It.IsAny<FindOptions<CachedMovie, CachedMovie>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorMock.Object);

        var result = await _repo.GetCachedMoviesAsync("test_key");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(1));
        Assert.That(result[0].Title, Is.EqualTo("Fresh Movie"));
    }

    [Test]
    public async Task GetCachedMoviesAsync_WhenCacheIsExpired_ReturnsNull()
    {
        var cachedMovie = new CachedMovie
        {
            CacheKey = "old_key",
            CachedAt = DateTime.UtcNow.AddHours(-2), // Older than 1-hour expiry
            Movies = new List<MovieDto> { new() { Id = 1, Title = "Old Movie" } }
        };

        var cursorMock = new Mock<IAsyncCursor<CachedMovie>>();
        cursorMock.Setup(c => c.Current).Returns(new List<CachedMovie> { cachedMovie });
        cursorMock.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockMovieCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<CachedMovie>>(),
                It.IsAny<FindOptions<CachedMovie, CachedMovie>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorMock.Object);

        var result = await _repo.GetCachedMoviesAsync("old_key");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetCachedMoviesAsync_WhenCacheIsAtExpiryBoundary_ReturnsData()
    {
        var cachedMovie = new CachedMovie
        {
            CacheKey = "boundary_key",
            CachedAt = DateTime.UtcNow.AddMinutes(-59), // Less than 1 hour
            Movies = new List<MovieDto> { new() { Id = 1, Title = "Boundary Movie" } }
        };

        var cursorMock = new Mock<IAsyncCursor<CachedMovie>>();
        cursorMock.Setup(c => c.Current).Returns(new List<CachedMovie> { cachedMovie });
        cursorMock.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockMovieCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<CachedMovie>>(),
                It.IsAny<FindOptions<CachedMovie, CachedMovie>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorMock.Object);

        var result = await _repo.GetCachedMoviesAsync("boundary_key");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(1));
    }

    // ── SaveMoviesAsync ─────────────────────────────────────────────

    [Test]
    public async Task SaveMoviesAsync_ShouldUpdateWithUpsert()
    {
        UpdateResult updateResult = CreateUpdateResult(1, 1);
        _mockMovieCollection.Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<CachedMovie>>(),
                It.IsAny<UpdateDefinition<CachedMovie>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updateResult);

        var movies = new List<MovieDto> { new() { Id = 1, Title = "Saved Movie" } };
        await _repo.SaveMoviesAsync("save_key", movies);

        _mockMovieCollection.Verify(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<CachedMovie>>(),
            It.IsAny<UpdateDefinition<CachedMovie>>(),
            It.Is<UpdateOptions>(o => o.IsUpsert == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }



    // ── GetCachedTvShowsAsync ───────────────────────────────────────

    [Test]
    public async Task GetCachedTvShowsAsync_WhenNoCache_ReturnsNull()
    {
        var cursorMock = new Mock<IAsyncCursor<CachedTvShow>>();
        cursorMock.Setup(c => c.Current).Returns(new List<CachedTvShow>());
        cursorMock.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockTvCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<CachedTvShow>>(),
                It.IsAny<FindOptions<CachedTvShow, CachedTvShow>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorMock.Object);

        var result = await _repo.GetCachedTvShowsAsync("tv_key");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetCachedTvShowsAsync_WhenCacheFresh_ReturnsShows()
    {
        var cached = new CachedTvShow
        {
            CacheKey = "tv_key",
            CachedAt = DateTime.UtcNow,
            TvShows = new List<TvShowDto> { new() { Id = 10, Name = "Fresh TV" } }
        };
        var cursorMock = new Mock<IAsyncCursor<CachedTvShow>>();
        cursorMock.Setup(c => c.Current).Returns(new List<CachedTvShow> { cached });
        cursorMock.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        _mockTvCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<CachedTvShow>>(),
                It.IsAny<FindOptions<CachedTvShow, CachedTvShow>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorMock.Object);

        var result = await _repo.GetCachedTvShowsAsync("tv_key");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Fresh TV"));
    }

    [Test]
    public async Task GetCachedTvShowsAsync_WhenExpired_ReturnsNull()
    {
        var cached = new CachedTvShow { CacheKey = "old_tv", CachedAt = DateTime.UtcNow.AddHours(-2) };
        var cursorMock = new Mock<IAsyncCursor<CachedTvShow>>();
        cursorMock.Setup(c => c.Current).Returns(new List<CachedTvShow> { cached });
        cursorMock.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        _mockTvCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<CachedTvShow>>(),
                It.IsAny<FindOptions<CachedTvShow, CachedTvShow>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorMock.Object);

        var result = await _repo.GetCachedTvShowsAsync("old_tv");

        Assert.That(result, Is.Null);
    }

    // ── SaveTvShowsAsync ────────────────────────────────────────────

    [Test]
    public async Task SaveTvShowsAsync_ShouldUseUpsert()
    {
        UpdateResult updateResult = CreateUpdateResult(1, 1);
        _mockTvCollection.Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<CachedTvShow>>(),
                It.IsAny<UpdateDefinition<CachedTvShow>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updateResult);

        await _repo.SaveTvShowsAsync("tv_save", new List<TvShowDto>());

        _mockTvCollection.Verify(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<CachedTvShow>>(),
            It.IsAny<UpdateDefinition<CachedTvShow>>(),
            It.Is<UpdateOptions>(o => o.IsUpsert == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GetCachedPersonsAsync ──────────────────────────────────────

    [Test]
    public async Task GetCachedPersonsAsync_WhenNoCache_ReturnsNull()
    {
        var cursorMock = new Mock<IAsyncCursor<CachedPerson>>();
        cursorMock.Setup(c => c.Current).Returns(new List<CachedPerson>());
        cursorMock.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockPersonCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<CachedPerson>>(),
                It.IsAny<FindOptions<CachedPerson, CachedPerson>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorMock.Object);

        var result = await _repo.GetCachedPersonsAsync("person_key");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetCachedPersonsAsync_WhenCacheFresh_ReturnsPersons()
    {
        var cached = new CachedPerson
        {
            CacheKey = "person_key",
            CachedAt = DateTime.UtcNow,
            Persons = new List<PersonDto> { new() { Id = 100, Name = "Fresh Person" } }
        };
        var cursorMock = new Mock<IAsyncCursor<CachedPerson>>();
        cursorMock.Setup(c => c.Current).Returns(new List<CachedPerson> { cached });
        cursorMock.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        _mockPersonCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<CachedPerson>>(),
                It.IsAny<FindOptions<CachedPerson, CachedPerson>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorMock.Object);

        var result = await _repo.GetCachedPersonsAsync("person_key");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Fresh Person"));
    }

    [Test]
    public async Task GetCachedPersonsAsync_WhenExpired_ReturnsNull()
    {
        var cached = new CachedPerson { CacheKey = "old_person", CachedAt = DateTime.UtcNow.AddHours(-2) };
        var cursorMock = new Mock<IAsyncCursor<CachedPerson>>();
        cursorMock.Setup(c => c.Current).Returns(new List<CachedPerson> { cached });
        cursorMock.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        _mockPersonCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<CachedPerson>>(),
                It.IsAny<FindOptions<CachedPerson, CachedPerson>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorMock.Object);

        var result = await _repo.GetCachedPersonsAsync("old_person");

        Assert.That(result, Is.Null);
    }

    // ── SavePersonsAsync ────────────────────────────────────────────

    [Test]
    public async Task SavePersonsAsync_ShouldUseUpsert()
    {
        UpdateResult updateResult = CreateUpdateResult(1, 1);
        _mockPersonCollection.Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<CachedPerson>>(),
                It.IsAny<UpdateDefinition<CachedPerson>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updateResult);

        await _repo.SavePersonsAsync("person_save", new List<PersonDto>());

        _mockPersonCollection.Verify(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<CachedPerson>>(),
            It.IsAny<UpdateDefinition<CachedPerson>>(),
            It.Is<UpdateOptions>(o => o.IsUpsert == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private static UpdateResult CreateUpdateResult(long matched, long modified)
    {
        var mockResult = new Mock<UpdateResult>();
        mockResult.Setup(r => r.MatchedCount).Returns(matched);
        mockResult.Setup(r => r.ModifiedCount).Returns(modified);
        mockResult.Setup(r => r.IsAcknowledged).Returns(true);
        return mockResult.Object;
    }
}
