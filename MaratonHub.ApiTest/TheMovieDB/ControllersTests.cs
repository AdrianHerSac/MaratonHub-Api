using MaratonHub.Api.TheMovieDB.Dtos;
using MaratonHub.Api.TheMovieDB.Controllers;
using MaratonHub.Api.TheMovieDB.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace MaratonHub.ApiTest.TheMovieDB;

public class MoviesControllerTests
{
    private Mock<ITheMovieDBService> _mockService = null!;
    private MoviesController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mockService = new Mock<ITheMovieDBService>();
        _controller = new MoviesController(_mockService.Object);
    }

    [Test]
    public void MoviesController_ShouldExist()
    {
        var type = Type.GetType("MaratonHub.Api.TheMovieDB.Controllers.MoviesController, MaratonHub.Api");
        Assert.That(type, Is.Not.Null);
    }

    [Test]
    public void MoviesController_ShouldHaveApiControllerAttribute()
    {
        var attrs = typeof(MoviesController).GetCustomAttributes(typeof(ApiControllerAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
    }

    [Test]
    public void MoviesController_ShouldHaveRouteAttribute()
    {
        var attrs = typeof(MoviesController).GetCustomAttributes(typeof(RouteAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
        Assert.That(((RouteAttribute)attrs[0]).Template, Is.EqualTo("api/[controller]"));
    }

    // ── GetTrending ───────────────────────────────────────────────────────

    [Test]
    public async Task GetTrending_ShouldReturnOkWithMovies()
    {
        var movies = new List<MovieDto>
        {
            new() { Id = 1, Title = "Trending Movie 1" },
            new() { Id = 2, Title = "Trending Movie 2" }
        };
        _mockService.Setup(s => s.GetTrendingMoviesAsync()).ReturnsAsync(movies);

        var result = await _controller.GetTrending();

        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));
        var returnedMovies = okResult.Value as List<MovieDto>;
        Assert.That(returnedMovies!.Count, Is.EqualTo(2));
        Assert.That(returnedMovies[0].Title, Is.EqualTo("Trending Movie 1"));
    }

    [Test]
    public async Task GetTrending_WhenEmpty_ShouldReturnEmptyList()
    {
        _mockService.Setup(s => s.GetTrendingMoviesAsync()).ReturnsAsync(new List<MovieDto>());

        var result = await _controller.GetTrending();

        var okResult = result as OkObjectResult;
        var movies = okResult!.Value as List<MovieDto>;
        Assert.That(movies!.Count, Is.EqualTo(0));
    }

    // ── GetPopular ────────────────────────────────────────────────────────

    [Test]
    public async Task GetPopular_ShouldReturnOkWithMovies()
    {
        var movies = new List<MovieDto> { new() { Id = 1, Title = "Popular Movie" } };
        _mockService.Setup(s => s.GetPopularMoviesAsync()).ReturnsAsync(movies);

        var result = await _controller.GetPopular();

        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));
    }

    // ── Search ────────────────────────────────────────────────────────────

    [Test]
    public async Task Search_WithValidQuery_ShouldReturnOkWithResults()
    {
        var movies = new List<MovieDto> { new() { Id = 1, Title = "Found Movie" } };
        _mockService.Setup(s => s.SearchMoviesAsync("test")).ReturnsAsync(movies);

        var result = await _controller.Search("test");

        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var returnedMovies = okResult!.Value as List<MovieDto>;
        Assert.That(returnedMovies!.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task Search_WithEmptyQuery_ShouldReturnBadRequest()
    {
        var result = await _controller.Search("");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Search_WithWhitespaceQuery_ShouldReturnBadRequest()
    {
        var result = await _controller.Search("   ");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Search_WithNoResults_ShouldReturnOkWithEmptyList()
    {
        _mockService.Setup(s => s.SearchMoviesAsync("nonexistent")).ReturnsAsync(new List<MovieDto>());

        var result = await _controller.Search("nonexistent");

        var okResult = result as OkObjectResult;
        var movies = okResult!.Value as List<MovieDto>;
        Assert.That(movies!.Count, Is.EqualTo(0));
    }

    // ── GetDetails ────────────────────────────────────────────────────────

    [Test]
    public async Task GetDetails_WhenExists_ShouldReturnOkWithMovie()
    {
        var movie = new MovieDto { Id = 123, Title = "Detailed Movie", VoteAverage = 8.5 };
        _mockService.Setup(s => s.GetMovieDetailsAsync(123)).ReturnsAsync(movie);

        var result = await _controller.GetDetails(123);

        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var returnedMovie = okResult!.Value as MovieDto;
        Assert.That(returnedMovie!.Id, Is.EqualTo(123));
        Assert.That(returnedMovie.Title, Is.EqualTo("Detailed Movie"));
        Assert.That(returnedMovie.VoteAverage, Is.EqualTo(8.5));
    }

    [Test]
    public async Task GetDetails_WhenNotFound_ShouldReturnNotFound()
    {
        _mockService.Setup(s => s.GetMovieDetailsAsync(999)).ReturnsAsync((MovieDto?)null);

        var result = await _controller.GetDetails(999);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    // ── Genre endpoints ───────────────────────────────────────────────────

    [Test]
    public async Task GetHorrorMovies_ShouldCallGetMoviesByGenreWith27()
    {
        var movies = new List<MovieDto> { new() { Id = 1, Title = "Horror Movie" } };
        _mockService.Setup(s => s.GetMoviesByGenreAsync(27)).ReturnsAsync(movies);

        var result = await _controller.GetHorrorMovies();

        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        _mockService.Verify(s => s.GetMoviesByGenreAsync(27), Times.Once);
    }

    [Test]
    public async Task GetComedyMovies_ShouldCallGetMoviesByGenreWith35()
    {
        _mockService.Setup(s => s.GetMoviesByGenreAsync(35)).ReturnsAsync(new List<MovieDto>());

        await _controller.GetComedyMovies();

        _mockService.Verify(s => s.GetMoviesByGenreAsync(35), Times.Once);
    }

    [Test]
    public async Task GetActionMovies_ShouldCallGetMoviesByGenreWith28()
    {
        _mockService.Setup(s => s.GetMoviesByGenreAsync(28)).ReturnsAsync(new List<MovieDto>());

        await _controller.GetActionMovies();

        _mockService.Verify(s => s.GetMoviesByGenreAsync(28), Times.Once);
    }

    [Test]
    public async Task GetAnimationMovies_ShouldCallGetMoviesByGenreWith16()
    {
        _mockService.Setup(s => s.GetMoviesByGenreAsync(16)).ReturnsAsync(new List<MovieDto>());

        await _controller.GetAnimationMovies();

        _mockService.Verify(s => s.GetMoviesByGenreAsync(16), Times.Once);
    }

    [Test]
    public async Task GetSciFiMovies_ShouldCallGetMoviesByGenreWith878()
    {
        _mockService.Setup(s => s.GetMoviesByGenreAsync(878)).ReturnsAsync(new List<MovieDto>());

        await _controller.GetSciFiMovies();

        _mockService.Verify(s => s.GetMoviesByGenreAsync(878), Times.Once);
    }

    [Test]
    public async Task GetByGenre_ShouldPassGenreIdToService()
    {
        var movies = new List<MovieDto> { new() { Id = 1, Title = "Custom Genre Movie" } };
        _mockService.Setup(s => s.GetMoviesByGenreAsync(99)).ReturnsAsync(movies);

        var result = await _controller.GetByGenre(99);

        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        _mockService.Verify(s => s.GetMoviesByGenreAsync(99), Times.Once);
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// TvShowsController Tests
// ══════════════════════════════════════════════════════════════════════════════

public class TvShowsControllerTests
{
    private Mock<ITheMovieDBService> _mockService = null!;
    private TvShowsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mockService = new Mock<ITheMovieDBService>();
        _controller = new TvShowsController(_mockService.Object);
    }

    [Test]
    public void TvShowsController_ShouldExist()
    {
        var type = Type.GetType("MaratonHub.Api.TheMovieDB.Controllers.TvShowsController, MaratonHub.Api");
        Assert.That(type, Is.Not.Null);
    }

    [Test]
    public void TvShowsController_ShouldHaveApiControllerAttribute()
    {
        var attrs = typeof(TvShowsController).GetCustomAttributes(typeof(ApiControllerAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
    }

    // ── GetTrending ───────────────────────────────────────────────────────

    [Test]
    public async Task GetTrending_ShouldReturnOkWithShows()
    {
        var shows = new List<TvShowDto>
        {
            new() { Id = 1, Name = "Trending Show" },
            new() { Id = 2, Name = "Another Show" }
        };
        _mockService.Setup(s => s.GetTrendingTvShowsAsync()).ReturnsAsync(shows);

        var result = await _controller.GetTrending();

        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var returnedShows = okResult!.Value as List<TvShowDto>;
        Assert.That(returnedShows!.Count, Is.EqualTo(2));
    }

    // ── GetPopular ────────────────────────────────────────────────────────

    [Test]
    public async Task GetPopular_ShouldReturnOkWithShows()
    {
        var shows = new List<TvShowDto> { new() { Id = 1, Name = "Popular Show" } };
        _mockService.Setup(s => s.GetPopularTvShowsAsync()).ReturnsAsync(shows);

        var result = await _controller.GetPopular();

        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
    }

    // ── Search ────────────────────────────────────────────────────────────

    [Test]
    public async Task Search_WithValidQuery_ShouldReturnOkWithResults()
    {
        var shows = new List<TvShowDto> { new() { Id = 1, Name = "Found Show" } };
        _mockService.Setup(s => s.SearchTvShowsAsync("test")).ReturnsAsync(shows);

        var result = await _controller.Search("test");

        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
    }

    [Test]
    public async Task Search_WithEmptyQuery_ShouldReturnBadRequest()
    {
        var result = await _controller.Search("");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Search_WithWhitespace_ShouldReturnBadRequest()
    {
        var result = await _controller.Search("   ");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    // ── GetDetails ────────────────────────────────────────────────────────

    [Test]
    public async Task GetDetails_WhenExists_ShouldReturnOkWithShow()
    {
        var show = new TvShowDto { Id = 1, Name = "Show Details", NumberOfSeasons = 5 };
        _mockService.Setup(s => s.GetTvShowDetailsAsync(1)).ReturnsAsync(show);

        var result = await _controller.GetDetails(1);

        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var returnedShow = okResult!.Value as TvShowDto;
        Assert.That(returnedShow!.NumberOfSeasons, Is.EqualTo(5));
    }

    [Test]
    public async Task GetDetails_WhenNotFound_ShouldReturnNotFound()
    {
        _mockService.Setup(s => s.GetTvShowDetailsAsync(999)).ReturnsAsync((TvShowDto?)null);

        var result = await _controller.GetDetails(999);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// PersonsController Tests
// ══════════════════════════════════════════════════════════════════════════════

public class PersonsControllerTests
{
    private Mock<ITheMovieDBService> _mockService = null!;
    private PersonsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mockService = new Mock<ITheMovieDBService>();
        _controller = new PersonsController(_mockService.Object);
    }

    [Test]
    public void PersonsController_ShouldExist()
    {
        var type = Type.GetType("MaratonHub.Api.TheMovieDB.Controllers.PersonsController, MaratonHub.Api");
        Assert.That(type, Is.Not.Null);
    }

    [Test]
    public void PersonsController_ShouldHaveApiControllerAttribute()
    {
        var attrs = typeof(PersonsController).GetCustomAttributes(typeof(ApiControllerAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
    }

    // ── GetPopular ────────────────────────────────────────────────────────

    [Test]
    public async Task GetPopular_ShouldReturnOkWithPersons()
    {
        var persons = new List<PersonDto>
        {
            new() { Id = 1, Name = "Actor 1", Popularity = 99.5 },
            new() { Id = 2, Name = "Actor 2", Popularity = 88.0 }
        };
        _mockService.Setup(s => s.GetPopularPersonsAsync()).ReturnsAsync(persons);

        var result = await _controller.GetPopular();

        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var returnedPersons = okResult!.Value as List<PersonDto>;
        Assert.That(returnedPersons!.Count, Is.EqualTo(2));
    }

    // ── Search ────────────────────────────────────────────────────────────

    [Test]
    public async Task Search_WithValidQuery_ShouldReturnOkWithResults()
    {
        var persons = new List<PersonDto> { new() { Id = 1, Name = "Found Person" } };
        _mockService.Setup(s => s.SearchPersonsAsync("test")).ReturnsAsync(persons);

        var result = await _controller.Search("test");

        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
    }

    [Test]
    public async Task Search_WithEmptyQuery_ShouldReturnBadRequest()
    {
        var result = await _controller.Search("");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Search_WithWhitespace_ShouldReturnBadRequest()
    {
        var result = await _controller.Search("   ");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    // ── GetDetails ────────────────────────────────────────────────────────

    [Test]
    public async Task GetDetails_WhenExists_ShouldReturnOkWithPerson()
    {
        var person = new PersonDto
        {
            Id = 1,
            Name = "Person Details",
            Biography = "A test biography",
            KnownForDepartment = "Acting",
            PlaceOfBirth = "Madrid, Spain"
        };
        _mockService.Setup(s => s.GetPersonDetailsAsync(1)).ReturnsAsync(person);

        var result = await _controller.GetDetails(1);

        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var returnedPerson = okResult!.Value as PersonDto;
        Assert.That(returnedPerson!.Biography, Is.EqualTo("A test biography"));
        Assert.That(returnedPerson.KnownForDepartment, Is.EqualTo("Acting"));
    }

    [Test]
    public async Task GetDetails_WhenNotFound_ShouldReturnNotFound()
    {
        _mockService.Setup(s => s.GetPersonDetailsAsync(999)).ReturnsAsync((PersonDto?)null);

        var result = await _controller.GetDetails(999);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }
}
