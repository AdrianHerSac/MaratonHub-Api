using MaratonHub.Api.Reviews.Models;
using MaratonHub.Api.Reviews.Dtos;
using MaratonHub.Api.Reviews.Controllers;
using MaratonHub.Api.Reviews.Reposytory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;

namespace MaratonHub.ApiTest.Reviews;

// ══════════════════════════════════════════════════════════════════════════════
// MediaIdentifier DTO Tests
// ══════════════════════════════════════════════════════════════════════════════

public class MediaIdentifierTests
{
    [Test]
    public void MediaIdentifier_ShouldInitializeWithDefaultValues()
    {
        var id = new MediaIdentifier();
        Assert.That(id.MediaId, Is.EqualTo(0));
        Assert.That(id.MediaType, Is.EqualTo(string.Empty));
    }

    [Test]
    public void MediaIdentifier_ShouldAllowSettingProperties()
    {
        var id = new MediaIdentifier { MediaId = 42, MediaType = "Movie" };
        Assert.That(id.MediaId, Is.EqualTo(42));
        Assert.That(id.MediaType, Is.EqualTo("Movie"));
    }

    [Test]
    public void MediaIdentifier_ShouldSupportAllMediaTypes()
    {
        foreach (var type in new[] { "Movie", "TvShow", "Season", "Episode", "Person" })
        {
            var id = new MediaIdentifier { MediaId = 1, MediaType = type };
            Assert.That(id.MediaType, Is.EqualTo(type));
        }
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// GetBatchAverageRating tests
// ══════════════════════════════════════════════════════════════════════════════

public class ReviewsControllerBatchTests
{
    private Mock<IReviewRepository> _mockRepo = null!;
    private ReviewsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<IReviewRepository>();
        var mockHubContext = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MaratonHub.Api.Groups.Hubs.ChatHub>>();
        _controller = new ReviewsController(_mockRepo.Object, mockHubContext.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Test]
    public async Task GetBatchAverageRating_ShouldReturnOkWithDictionary()
    {
        var items = new List<MediaIdentifier>
        {
            new() { MediaId = 1, MediaType = "Season" },
            new() { MediaId = 2, MediaType = "Episode" }
        };
        var expected = new Dictionary<string, RatingAverageDto>
        {
            { "Season_1", new RatingAverageDto { Average = 4.0, Percentage = 80, TotalReviews = 5 } },
            { "Episode_2", new RatingAverageDto { Average = 3.5, Percentage = 70, TotalReviews = 2 } }
        };
        _mockRepo.Setup(r => r.GetBatchAveragesAsync(items)).ReturnsAsync(expected);

        var result = await _controller.GetBatchAverageRating(items);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var dict = ok!.Value as Dictionary<string, RatingAverageDto>;
        Assert.That(dict, Is.Not.Null);
        Assert.That(dict!["Season_1"].Average, Is.EqualTo(4.0));
        Assert.That(dict["Episode_2"].TotalReviews, Is.EqualTo(2));
    }

    [Test]
    public async Task GetBatchAverageRating_WithEmptyList_ShouldReturnOkWithEmptyDict()
    {
        var items = new List<MediaIdentifier>();
        _mockRepo.Setup(r => r.GetBatchAveragesAsync(items)).ReturnsAsync(new Dictionary<string, RatingAverageDto>());

        var result = await _controller.GetBatchAverageRating(items);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var dict = ok!.Value as Dictionary<string, RatingAverageDto>;
        Assert.That(dict!.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task GetBatchAverageRating_ShouldCallRepositoryOnce()
    {
        var items = new List<MediaIdentifier> { new() { MediaId = 1, MediaType = "Movie" } };
        _mockRepo.Setup(r => r.GetBatchAveragesAsync(items)).ReturnsAsync(new Dictionary<string, RatingAverageDto>());

        await _controller.GetBatchAverageRating(items);

        _mockRepo.Verify(r => r.GetBatchAveragesAsync(items), Times.Once);
    }

    // ── GetTopRated ────────────────────────────────────────────────────────

    [Test]
    public async Task GetTopRated_WithMovieType_ShouldMapToMovieString()
    {
        var topRated = new List<TopRatedAppMediaDto>
        {
            new() { MediaId = 1, MediaType = "movie" }
        };
        _mockRepo.Setup(r => r.GetTopRatedMediaAsync("Movie", 1, 20)).ReturnsAsync(topRated);

        var result = await _controller.GetTopRated("movie");

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
    }

    [Test]
    public async Task GetTopRated_WithTvType_ShouldMapToTvShowString()
    {
        _mockRepo.Setup(r => r.GetTopRatedMediaAsync("TvShow", 1, 20)).ReturnsAsync(new List<TopRatedAppMediaDto>());

        var result = await _controller.GetTopRated("tv");

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetTopRated_WithPersonType_ShouldMapToPersonString()
    {
        _mockRepo.Setup(r => r.GetTopRatedMediaAsync("Person", 1, 20)).ReturnsAsync(new List<TopRatedAppMediaDto>());

        var result = await _controller.GetTopRated("person");

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetTopRated_WithCustomPagination_ShouldPassCorrectParams()
    {
        _mockRepo.Setup(r => r.GetTopRatedMediaAsync("Movie", 2, 10)).ReturnsAsync(new List<TopRatedAppMediaDto>());

        var result = await _controller.GetTopRated("movie", 2, 10);

        _mockRepo.Verify(r => r.GetTopRatedMediaAsync("Movie", 2, 10), Times.Once);
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetTopRated_ShouldSetMediaTypeOnResults()
    {
        var topRated = new List<TopRatedAppMediaDto>
        {
            new() { MediaId = 1, MediaType = "Movie" }
        };
        _mockRepo.Setup(r => r.GetTopRatedMediaAsync("Movie", 1, 20)).ReturnsAsync(topRated);

        var result = await _controller.GetTopRated("movie");

        var ok = result as OkObjectResult;
        var list = ok!.Value as List<TopRatedAppMediaDto>;
        // Should set MediaType to the original mediaType argument
        Assert.That(list![0].MediaType, Is.EqualTo("movie"));
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// IReviewRepository extended method tests
// ══════════════════════════════════════════════════════════════════════════════

public class IReviewRepositoryExtendedTests
{
    [Test]
    public void GetBatchAveragesAsync_ShouldBeDefined()
    {
        var m = typeof(IReviewRepository).GetMethod("GetBatchAveragesAsync");
        Assert.That(m, Is.Not.Null);
        var p = m!.GetParameters();
        Assert.That(p.Length, Is.EqualTo(1));
        Assert.That(p[0].ParameterType, Is.EqualTo(typeof(List<MediaIdentifier>)));
    }

    [Test]
    public void CountReviewsAsync_ShouldReturnTaskOfLong()
    {
        var m = typeof(IReviewRepository).GetMethod("CountReviewsAsync");
        Assert.That(m, Is.Not.Null);
        Assert.That(m!.ReturnType, Is.EqualTo(typeof(Task<long>)));
    }

    [Test]
    public void GetTopRatedMediaAsync_ShouldHaveCorrectSignature()
    {
        var m = typeof(IReviewRepository).GetMethod("GetTopRatedMediaAsync");
        Assert.That(m, Is.Not.Null);
        var p = m!.GetParameters();
        Assert.That(p.Length, Is.EqualTo(3));
        Assert.That(p[0].ParameterType, Is.EqualTo(typeof(string)));
        Assert.That(p[1].ParameterType, Is.EqualTo(typeof(int)));
        Assert.That(p[2].ParameterType, Is.EqualTo(typeof(int)));
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// TopRatedAppMediaDto Tests
// ══════════════════════════════════════════════════════════════════════════════

public class TopRatedAppMediaDtoTests
{
    [Test]
    public void TopRatedAppMediaDto_ShouldAllowSettingProperties()
    {
        var dto = new TopRatedAppMediaDto
        {
            MediaId = 1,
            MediaType = "Movie",
            Average = 4.5,
            Percentage = 90,
            TotalReviews = 100
        };

        Assert.That(dto.MediaId, Is.EqualTo(1));
        Assert.That(dto.MediaType, Is.EqualTo("Movie"));
        Assert.That(dto.Average, Is.EqualTo(4.5));
        Assert.That(dto.Percentage, Is.EqualTo(90));
        Assert.That(dto.TotalReviews, Is.EqualTo(100));
    }

    [Test]
    public void TopRatedAppMediaDto_ShouldInitializeWithDefaultValues()
    {
        var dto = new TopRatedAppMediaDto();
        Assert.That(dto.MediaId, Is.EqualTo(0));
        Assert.That(dto.MediaType, Is.EqualTo(string.Empty));
        Assert.That(dto.Average, Is.EqualTo(0));
        Assert.That(dto.Percentage, Is.EqualTo(0));
        Assert.That(dto.TotalReviews, Is.EqualTo(0));
    }
}
