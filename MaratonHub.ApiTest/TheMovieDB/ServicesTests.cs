using MaratonHub.Api.TheMovieDB.Services;
using MaratonHub.Api.TheMovieDB.Repository;
using MaratonHub.Api.TheMovieDB.Models;
using MaratonHub.Api.TheMovieDB.Dtos;
using MongoDB.Driver;

namespace MaratonHub.ApiTest.TheMovieDB;

public class ITheMovieDBServiceTests
{
    private readonly Type _type = typeof(ITheMovieDBService);

    [Test]
    public void ShouldBeAnInterface()
    {
        Assert.That(_type.IsInterface, Is.True);
    }

    // ── Movies ────────────────────────────────────────────────────────────

    [Test]
    public void ShouldDefineGetTrendingMoviesAsync()
    {
        var method = _type.GetMethod("GetTrendingMoviesAsync");
        Assert.That(method, Is.Not.Null);
        Assert.That(method!.GetParameters().Length, Is.EqualTo(0));
        Assert.That(method.ReturnType, Is.EqualTo(typeof(Task<List<MovieDto>>)));
    }

    [Test]
    public void ShouldDefineGetPopularMoviesAsync()
    {
        var method = _type.GetMethod("GetPopularMoviesAsync");
        Assert.That(method, Is.Not.Null);
        Assert.That(method!.GetParameters().Length, Is.EqualTo(0));
        Assert.That(method.ReturnType, Is.EqualTo(typeof(Task<List<MovieDto>>)));
    }

    [Test]
    public void ShouldDefineGetMoviesByGenreAsync()
    {
        var method = _type.GetMethod("GetMoviesByGenreAsync");
        Assert.That(method, Is.Not.Null);
        var parameters = method!.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(1));
        Assert.That(parameters[0].Name, Is.EqualTo("genreId"));
        Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(int)));
        Assert.That(method.ReturnType, Is.EqualTo(typeof(Task<List<MovieDto>>)));
    }

    [Test]
    public void ShouldDefineSearchMoviesAsync()
    {
        var method = _type.GetMethod("SearchMoviesAsync");
        Assert.That(method, Is.Not.Null);
        var parameters = method!.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(1));
        Assert.That(parameters[0].Name, Is.EqualTo("query"));
        Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void ShouldDefineGetMovieDetailsAsync()
    {
        var method = _type.GetMethod("GetMovieDetailsAsync");
        Assert.That(method, Is.Not.Null);
        var parameters = method!.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(1));
        Assert.That(parameters[0].Name, Is.EqualTo("id"));
        Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(int)));
    }

    // ── TV Shows

    [Test]
    public void ShouldDefineGetTrendingTvShowsAsync()
    {
        var method = _type.GetMethod("GetTrendingTvShowsAsync");
        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<List<TvShowDto>>)));
    }

    [Test]
    public void ShouldDefineGetPopularTvShowsAsync()
    {
        var method = _type.GetMethod("GetPopularTvShowsAsync");
        Assert.That(method, Is.Not.Null);
    }

    [Test]
    public void ShouldDefineGetTvShowsByGenreAsync()
    {
        var method = _type.GetMethod("GetTvShowsByGenreAsync");
        Assert.That(method, Is.Not.Null);
        var parameters = method!.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(1));
        Assert.That(parameters[0].Name, Is.EqualTo("genreId"));
        Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(int)));
        Assert.That(method.ReturnType, Is.EqualTo(typeof(Task<List<TvShowDto>>)));
    }

    [Test]
    public void ShouldDefineSearchTvShowsAsync()
    {
        var method = _type.GetMethod("SearchTvShowsAsync");
        Assert.That(method, Is.Not.Null);
        var parameters = method!.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(1));
        Assert.That(parameters[0].Name, Is.EqualTo("query"));
    }

    [Test]
    public void ShouldDefineGetTvShowDetailsAsync()
    {
        var method = _type.GetMethod("GetTvShowDetailsAsync");
        Assert.That(method, Is.Not.Null);
        var parameters = method!.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(1));
        Assert.That(parameters[0].Name, Is.EqualTo("id"));
    }

    [Test]
    public void ShouldDefineGetTvShowSeasonAsync()
    {
        var method = _type.GetMethod("GetTvShowSeasonAsync");
        Assert.That(method, Is.Not.Null);
        var parameters = method!.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(2));
        Assert.That(parameters[0].Name, Is.EqualTo("tvShowId"));
        Assert.That(parameters[1].Name, Is.EqualTo("seasonNumber"));
    }

    // ── Persons

    [Test]
    public void ShouldDefineGetPopularPersonsAsync()
    {
        var method = _type.GetMethod("GetPopularPersonsAsync");
        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<List<PersonDto>>)));
    }

    [Test]
    public void ShouldDefineSearchPersonsAsync()
    {
        var method = _type.GetMethod("SearchPersonsAsync");
        Assert.That(method, Is.Not.Null);
    }

    [Test]
    public void ShouldDefineGetPersonDetailsAsync()
    {
        var method = _type.GetMethod("GetPersonDetailsAsync");
        Assert.That(method, Is.Not.Null);
    }

    // ── Changes

    [Test]
    public void ShouldDefineGetChangedMovieIdsAsync()
    {
        var method = _type.GetMethod("GetChangedMovieIdsAsync");
        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<List<int>>)));
    }

    [Test]
    public void ShouldDefineGetChangedTvShowIdsAsync()
    {
        var method = _type.GetMethod("GetChangedTvShowIdsAsync");
        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<List<int>>)));
    }

    [Test]
    public void ShouldHaveExactly16Methods()
    {
        var methods = _type.GetMethods();
        Assert.That(methods.Length, Is.EqualTo(16));
    }
}

public class IMediaCacheRepositoryTests
{
    private readonly Type _type = typeof(IMediaCacheRepository);

    [Test]
    public void ShouldBeAnInterface()
    {
        Assert.That(_type.IsInterface, Is.True);
    }

    [Test]
    public void ShouldDefineGetCachedMoviesAsync()
    {
        var method = _type.GetMethod("GetCachedMoviesAsync");
        Assert.That(method, Is.Not.Null);
        var parameters = method!.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(1));
        Assert.That(parameters[0].Name, Is.EqualTo("cacheKey"));
    }

    [Test]
    public void ShouldDefineSaveMoviesAsync()
    {
        var method = _type.GetMethod("SaveMoviesAsync");
        Assert.That(method, Is.Not.Null);
        var parameters = method!.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(2));
        Assert.That(parameters[0].Name, Is.EqualTo("cacheKey"));
        Assert.That(parameters[1].Name, Is.EqualTo("movies"));
    }

    [Test]
    public void ShouldDefineGetCachedTvShowsAsync()
    {
        var method = _type.GetMethod("GetCachedTvShowsAsync");
        Assert.That(method, Is.Not.Null);
    }

    [Test]
    public void ShouldDefineSaveTvShowsAsync()
    {
        var method = _type.GetMethod("SaveTvShowsAsync");
        Assert.That(method, Is.Not.Null);
    }

    [Test]
    public void ShouldDefineGetCachedPersonsAsync()
    {
        var method = _type.GetMethod("GetCachedPersonsAsync");
        Assert.That(method, Is.Not.Null);
    }

    [Test]
    public void ShouldDefineSavePersonsAsync()
    {
        var method = _type.GetMethod("SavePersonsAsync");
        Assert.That(method, Is.Not.Null);
    }

    [Test]
    public void ShouldHaveExactly6Methods()
    {
        var methods = _type.GetMethods();
        Assert.That(methods.Length, Is.EqualTo(6));
    }
}

public class CachedModelTests
{
    [Test]
    public void CachedMovie_ShouldHaveCorrectProperties()
    {
        var cached = new CachedMovie
        {
            CacheKey = "test_key",
            CachedAt = DateTime.UtcNow,
            Movies = new List<MovieDto>
            {
                new() { Id = 1, Title = "Test Movie" },
                new() { Id = 2, Title = "Test Movie 2" }
            }
        };

        Assert.That(cached.CacheKey, Is.EqualTo("test_key"));
        Assert.That(cached.Movies.Count, Is.EqualTo(2));
        Assert.That(cached.Movies[0].Title, Is.EqualTo("Test Movie"));
    }

    [Test]
    public void CachedMovie_ShouldHaveDefaultId()
    {
        var cached = new CachedMovie();
        Assert.That(cached.Id, Is.Not.EqualTo(MongoDB.Bson.ObjectId.Empty));
    }

    [Test]
    public void CachedMovie_ShouldHaveDefaultEmptyMoviesList()
    {
        var cached = new CachedMovie();
        Assert.That(cached.Movies, Is.Not.Null);
        Assert.That(cached.Movies.Count, Is.EqualTo(0));
    }

    [Test]
    public void CachedTvShow_ShouldHaveCorrectProperties()
    {
        var cached = new CachedTvShow
        {
            CacheKey = "tv_key",
            CachedAt = DateTime.UtcNow,
            TvShows = new List<TvShowDto>
            {
                new() { Id = 1, Name = "Test Show" }
            }
        };

        Assert.That(cached.CacheKey, Is.EqualTo("tv_key"));
        Assert.That(cached.TvShows.Count, Is.EqualTo(1));
    }

    [Test]
    public void CachedTvShow_ShouldHaveDefaultId()
    {
        var cached = new CachedTvShow();
        Assert.That(cached.Id, Is.Not.EqualTo(MongoDB.Bson.ObjectId.Empty));
    }

    [Test]
    public void CachedTvShow_ShouldHaveDefaultEmptyShowsList()
    {
        var cached = new CachedTvShow();
        Assert.That(cached.TvShows, Is.Not.Null);
        Assert.That(cached.TvShows.Count, Is.EqualTo(0));
    }

    [Test]
    public void CachedPerson_ShouldHaveCorrectProperties()
    {
        var cached = new CachedPerson
        {
            CacheKey = "person_key",
            CachedAt = DateTime.UtcNow,
            Persons = new List<PersonDto>
            {
                new() { Id = 1, Name = "Test Actor" }
            }
        };

        Assert.That(cached.CacheKey, Is.EqualTo("person_key"));
        Assert.That(cached.Persons.Count, Is.EqualTo(1));
    }

    [Test]
    public void CachedPerson_ShouldHaveDefaultId()
    {
        var cached = new CachedPerson();
        Assert.That(cached.Id, Is.Not.EqualTo(MongoDB.Bson.ObjectId.Empty));
    }

    [Test]
    public void CachedPerson_ShouldHaveDefaultEmptyPersonsList()
    {
        var cached = new CachedPerson();
        Assert.That(cached.Persons, Is.Not.Null);
        Assert.That(cached.Persons.Count, Is.EqualTo(0));
    }

    [Test]
    public void CachedMovie_CachedAt_ShouldDefaultToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var cached = new CachedMovie();
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.That(cached.CachedAt, Is.GreaterThanOrEqualTo(before));
        Assert.That(cached.CachedAt, Is.LessThanOrEqualTo(after));
    }

    [Test]
    public void CachedTvShow_CachedAt_ShouldDefaultToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var cached = new CachedTvShow();
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.That(cached.CachedAt, Is.GreaterThanOrEqualTo(before));
        Assert.That(cached.CachedAt, Is.LessThanOrEqualTo(after));
    }

    [Test]
    public void CachedPerson_CachedAt_ShouldDefaultToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var cached = new CachedPerson();
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.That(cached.CachedAt, Is.GreaterThanOrEqualTo(before));
        Assert.That(cached.CachedAt, Is.LessThanOrEqualTo(after));
    }

    [Test]
    public void CachedMovie_CacheKey_ShouldDefaultToEmpty()
    {
        var cached = new CachedMovie();
        Assert.That(cached.CacheKey, Is.EqualTo(string.Empty));
    }

    [Test]
    public void CachedTvShow_CacheKey_ShouldDefaultToEmpty()
    {
        var cached = new CachedTvShow();
        Assert.That(cached.CacheKey, Is.EqualTo(string.Empty));
    }

    [Test]
    public void CachedPerson_CacheKey_ShouldDefaultToEmpty()
    {
        var cached = new CachedPerson();
        Assert.That(cached.CacheKey, Is.EqualTo(string.Empty));
    }
}

public class TheMovieDBServiceTests
{
    [Test]
    public void TheMovieDBService_ShouldImplementITheMovieDBService()
    {
        var type = typeof(TheMovieDBService);
        Assert.That(type.GetInterface(nameof(ITheMovieDBService)), Is.Not.Null);
    }

    [Test]
    public void TheMovieDBService_ShouldHaveCorrectConstructor()
    {
        var constructors = typeof(TheMovieDBService).GetConstructors();
        Assert.That(constructors.Length, Is.EqualTo(1));

        var parameters = constructors[0].GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(4));
        Assert.That(parameters[0].ParameterType.Name, Does.Contain("IConfiguration"));
        Assert.That(parameters[1].ParameterType.Name, Does.Contain("IMediaCacheRepository"));
        Assert.That(parameters[2].ParameterType.Name, Does.Contain("IRedisCacheService"));
        Assert.That(parameters[3].ParameterType.Name, Does.Contain("ILogger"));
    }
}