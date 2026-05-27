using MaratonHub.Api.Reviews.Models;
using MaratonHub.Api.Reviews.Dtos;
using MaratonHub.Api.Reviews.Controllers;
using MaratonHub.Api.Reviews.Reposytory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;

namespace MaratonHub.ApiTest.Reviews;

public class ReviewModelTests
{
    [Test]
    public void Review_ShouldInitializeWithDefaultValues()
    {
        var review = new Review();

        Assert.That(review.Id, Is.Null);
        Assert.That(review.UserId, Is.EqualTo(string.Empty));
        Assert.That(review.MediaId, Is.EqualTo(0));
        Assert.That(review.MediaType, Is.EqualTo(string.Empty));
        Assert.That(review.UserName, Is.EqualTo(string.Empty));
        Assert.That(review.Rating, Is.EqualTo(0));
        Assert.That(review.Comment, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Review_ShouldAllowSettingProperties()
    {
        var review = new Review
        {
            Id = "abc123",
            UserId = "user123",
            MediaId = 100,
            MediaType = "Movie",
            UserName = "TestUser",
            Rating = 5,
            Comment = "Great movie!",
            CreatedAt = new DateTime(2024, 1, 1)
        };

        Assert.That(review.Id, Is.EqualTo("abc123"));
        Assert.That(review.UserId, Is.EqualTo("user123"));
        Assert.That(review.MediaId, Is.EqualTo(100));
        Assert.That(review.MediaType, Is.EqualTo("Movie"));
        Assert.That(review.UserName, Is.EqualTo("TestUser"));
        Assert.That(review.Rating, Is.EqualTo(5));
        Assert.That(review.Comment, Is.EqualTo("Great movie!"));
        Assert.That(review.CreatedAt, Is.EqualTo(new DateTime(2024, 1, 1)));
    }

    [Test]
    public void Review_CreatedAt_ShouldDefaultToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var review = new Review();
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.That(review.CreatedAt, Is.GreaterThanOrEqualTo(before));
        Assert.That(review.CreatedAt, Is.LessThanOrEqualTo(after));
    }

    [Test]
    public void Review_ShouldHaveBsonIdAttribute()
    {
        var property = typeof(Review).GetProperty("Id");
        var attrs = property!.GetCustomAttributes(typeof(MongoDB.Bson.Serialization.Attributes.BsonIdAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
    }

    [Test]
    public void Review_ShouldHaveBsonRepresentationAttribute()
    {
        var property = typeof(Review).GetProperty("Id");
        var attrs = property!.GetCustomAttributes(typeof(MongoDB.Bson.Serialization.Attributes.BsonRepresentationAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// DTO Tests
// ══════════════════════════════════════════════════════════════════════════════

public class ReviewDtoTests
{
    [Test]
    public void ReviewDto_ShouldInitializeWithDefaultValues()
    {
        var dto = new ReviewDto();

        Assert.That(dto.Id, Is.Null);
        Assert.That(dto.UserId, Is.EqualTo(string.Empty));
        Assert.That(dto.MediaId, Is.EqualTo(0));
        Assert.That(dto.MediaType, Is.EqualTo(string.Empty));
        Assert.That(dto.UserName, Is.EqualTo(string.Empty));
        Assert.That(dto.Rating, Is.EqualTo(0));
        Assert.That(dto.Comment, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ReviewDto_ShouldAllowSettingProperties()
    {
        var now = DateTime.UtcNow;
        var dto = new ReviewDto
        {
            Id = "review123",
            UserId = "user123",
            MediaId = 100,
            MediaType = "Movie",
            UserName = "TestUser",
            Rating = 5,
            Comment = "Great!",
            CreatedAt = now
        };

        Assert.That(dto.Id, Is.EqualTo("review123"));
        Assert.That(dto.UserId, Is.EqualTo("user123"));
        Assert.That(dto.MediaId, Is.EqualTo(100));
        Assert.That(dto.MediaType, Is.EqualTo("Movie"));
        Assert.That(dto.UserName, Is.EqualTo("TestUser"));
        Assert.That(dto.Rating, Is.EqualTo(5));
        Assert.That(dto.Comment, Is.EqualTo("Great!"));
        Assert.That(dto.CreatedAt, Is.EqualTo(now));
    }
}

public class CreateReviewDtoTests
{
    [Test]
    public void CreateReviewDto_ShouldInitializeWithDefaultValues()
    {
        var dto = new CreateReviewDto();

        Assert.That(dto.MediaId, Is.EqualTo(0));
        Assert.That(dto.MediaType, Is.EqualTo(string.Empty));
        Assert.That(dto.Rating, Is.EqualTo(0));
        Assert.That(dto.Comment, Is.EqualTo(string.Empty));
    }

    [Test]
    public void CreateReviewDto_ShouldAllowSettingProperties()
    {
        var dto = new CreateReviewDto
        {
            MediaId = 100,
            MediaType = "Movie",
            Rating = 4,
            Comment = "Good movie"
        };

        Assert.That(dto.MediaId, Is.EqualTo(100));
        Assert.That(dto.MediaType, Is.EqualTo("Movie"));
        Assert.That(dto.Rating, Is.EqualTo(4));
        Assert.That(dto.Comment, Is.EqualTo("Good movie"));
    }
}

public class RatingAverageDtoTests
{
    [Test]
    public void RatingAverageDto_ShouldInitializeWithDefaultValues()
    {
        var dto = new RatingAverageDto();

        Assert.That(dto.Average, Is.EqualTo(0));
        Assert.That(dto.Percentage, Is.EqualTo(0));
        Assert.That(dto.TotalReviews, Is.EqualTo(0));
    }

    [Test]
    public void RatingAverageDto_ShouldAllowSettingProperties()
    {
        var dto = new RatingAverageDto
        {
            Average = 4.5,
            Percentage = 90,
            TotalReviews = 100
        };

        Assert.That(dto.Average, Is.EqualTo(4.5));
        Assert.That(dto.Percentage, Is.EqualTo(90));
        Assert.That(dto.TotalReviews, Is.EqualTo(100));
    }

    [Test]
    public void RatingAverageDto_Percentage_ShouldHandleBoundaryValues()
    {
        var dtoZero = new RatingAverageDto { Average = 0, Percentage = 0, TotalReviews = 0 };
        Assert.That(dtoZero.Percentage, Is.EqualTo(0));

        var dtoMax = new RatingAverageDto { Average = 5.0, Percentage = 100, TotalReviews = 1000 };
        Assert.That(dtoMax.Percentage, Is.EqualTo(100));
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// Controller Tests (con Moq)
// ══════════════════════════════════════════════════════════════════════════════

public class ReviewsControllerTests
{
    private Mock<IReviewRepository> _mockRepo = null!;
    private ReviewsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<IReviewRepository>();

        var mockHubContext = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MaratonHub.Api.Groups.Hubs.ChatHub>>();
        var mockClients = new Mock<Microsoft.AspNetCore.SignalR.IHubClients>();
        var mockClientProxy = new Mock<Microsoft.AspNetCore.SignalR.IClientProxy>();
        mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);

        _controller = new ReviewsController(_mockRepo.Object, mockHubContext.Object);
    }

    [Test]
    public void ReviewsController_ShouldExist()
    {
        var type = Type.GetType("MaratonHub.Api.Reviews.Controllers.ReviewsController, MaratonHub.Api");
        Assert.That(type, Is.Not.Null);
    }

    [Test]
    public void ReviewsController_ShouldHaveApiControllerAttribute()
    {
        var attrs = typeof(ReviewsController).GetCustomAttributes(typeof(ApiControllerAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
    }

    [Test]
    public void ReviewsController_ShouldHaveRouteAttribute()
    {
        var attrs = typeof(ReviewsController).GetCustomAttributes(typeof(RouteAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
        Assert.That(((RouteAttribute)attrs[0]).Template, Is.EqualTo("api/[controller]"));
    }

    // ── GetAverageRating ──────────────────────────────────────────────────

    [Test]
    public async Task GetAverageRating_ShouldReturnOkWithResult()
    {
        var expected = new RatingAverageDto { Average = 4.0, Percentage = 80, TotalReviews = 10 };
        _mockRepo.Setup(r => r.GetAverageRatingAsync(1, "Movie")).ReturnsAsync(expected);

        var result = await _controller.GetAverageRating("Movie", 1);

        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));
        var dto = okResult.Value as RatingAverageDto;
        Assert.That(dto!.Average, Is.EqualTo(4.0));
        Assert.That(dto.TotalReviews, Is.EqualTo(10));
    }

    [Test]
    public async Task GetAverageRating_WithNoReviews_ShouldReturnZeroAverage()
    {
        var expected = new RatingAverageDto { Average = 0, Percentage = 0, TotalReviews = 0 };
        _mockRepo.Setup(r => r.GetAverageRatingAsync(999, "Movie")).ReturnsAsync(expected);

        var result = await _controller.GetAverageRating("Movie", 999);

        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var dto = okResult!.Value as RatingAverageDto;
        Assert.That(dto!.TotalReviews, Is.EqualTo(0));
    }

    // ── GetReviewsByMedia ─────────────────────────────────────────────────

    [Test]
    public async Task GetReviewsByMedia_ShouldReturnOkWithReviews()
    {
        var reviews = new List<Review>
        {
            new() { Id = "r1", UserId = "u1", MediaId = 1, MediaType = "Movie", UserName = "User1", Rating = 5, Comment = "Excellent" },
            new() { Id = "r2", UserId = "u2", MediaId = 1, MediaType = "Movie", UserName = "User2", Rating = 3, Comment = "OK" }
        };
        _mockRepo.Setup(r => r.GetReviewsByMediaAsync(1, "Movie")).ReturnsAsync(reviews);

        var result = await _controller.GetReviewsByMedia("Movie", 1);

        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var dtos = okResult!.Value as List<ReviewDto>;
        Assert.That(dtos, Is.Not.Null);
        Assert.That(dtos!.Count, Is.EqualTo(2));
        Assert.That(dtos[0].Id, Is.EqualTo("r1"));
        Assert.That(dtos[0].UserName, Is.EqualTo("User1"));
        Assert.That(dtos[1].Rating, Is.EqualTo(3));
    }

    [Test]
    public async Task GetReviewsByMedia_WithNoReviews_ShouldReturnEmptyList()
    {
        _mockRepo.Setup(r => r.GetReviewsByMediaAsync(999, "Movie")).ReturnsAsync(new List<Review>());

        var result = await _controller.GetReviewsByMedia("Movie", 999);

        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var dtos = okResult!.Value as List<ReviewDto>;
        Assert.That(dtos!.Count, Is.EqualTo(0));
    }

    // ── GetReviewsByUser ──────────────────────────────────────────────────

    [Test]
    public async Task GetReviewsByUser_ShouldReturnOkWithReviews()
    {
        var reviews = new List<Review>
        {
            new() { Id = "r1", UserName = "TestUser", Rating = 4, Comment = "Nice" }
        };
        _mockRepo.Setup(r => r.GetReviewsByUserAsync("TestUser")).ReturnsAsync(reviews);

        var result = await _controller.GetReviewsByUser("TestUser");

        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var dtos = okResult!.Value as List<ReviewDto>;
        Assert.That(dtos!.Count, Is.EqualTo(1));
        Assert.That(dtos[0].UserName, Is.EqualTo("TestUser"));
    }

    [Test]
    public async Task GetReviewsByUser_WithNoReviews_ShouldReturnEmptyList()
    {
        _mockRepo.Setup(r => r.GetReviewsByUserAsync("Nobody")).ReturnsAsync(new List<Review>());

        var result = await _controller.GetReviewsByUser("Nobody");

        var okResult = result as OkObjectResult;
        var dtos = okResult!.Value as List<ReviewDto>;
        Assert.That(dtos!.Count, Is.EqualTo(0));
    }

    // ── CreateReview ──────────────────────────────────────────────────────

    private void SetupUserClaims(string userId, string username)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("unique_name", username)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Test]
    public async Task CreateReview_WithValidData_ShouldReturnCreatedAtAction()
    {
        SetupUserClaims("user123", "TestUser");
        var dto = new CreateReviewDto { MediaId = 1, MediaType = "Movie", Rating = 5, Comment = "Amazing!" };

        _mockRepo.Setup(r => r.CreateReviewAsync(It.IsAny<Review>()))
            .ReturnsAsync((Review r) => { r.Id = "newId"; return r; });

        var result = await _controller.CreateReview(dto);

        var createdResult = result as CreatedAtActionResult;
        Assert.That(createdResult, Is.Not.Null);
        Assert.That(createdResult!.StatusCode, Is.EqualTo(201));

        var reviewDto = createdResult.Value as ReviewDto;
        Assert.That(reviewDto, Is.Not.Null);
        Assert.That(reviewDto!.Id, Is.EqualTo("newId"));
        Assert.That(reviewDto.Rating, Is.EqualTo(5));
        Assert.That(reviewDto.Comment, Is.EqualTo("Amazing!"));
        Assert.That(reviewDto.UserName, Is.EqualTo("TestUser"));
        Assert.That(reviewDto.UserId, Is.EqualTo("user123"));
    }

    [Test]
    public async Task CreateReview_WithRatingTooLow_ShouldReturnBadRequest()
    {
        SetupUserClaims("user123", "TestUser");
        var dto = new CreateReviewDto { MediaId = 1, MediaType = "Movie", Rating = 0, Comment = "Bad" };

        var result = await _controller.CreateReview(dto);

        var badRequest = result as BadRequestObjectResult;
        Assert.That(badRequest, Is.Not.Null);
        Assert.That(badRequest!.StatusCode, Is.EqualTo(400));
        Assert.That(badRequest.Value!.ToString(), Does.Contain("Rating must be between 1 and 5"));
    }

    [Test]
    public async Task CreateReview_WithRatingTooHigh_ShouldReturnBadRequest()
    {
        SetupUserClaims("user123", "TestUser");
        var dto = new CreateReviewDto { MediaId = 1, MediaType = "Movie", Rating = 6, Comment = "Too high" };

        var result = await _controller.CreateReview(dto);

        var badRequest = result as BadRequestObjectResult;
        Assert.That(badRequest, Is.Not.Null);
        Assert.That(badRequest!.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task CreateReview_WithMinRating_ShouldSucceed()
    {
        SetupUserClaims("user123", "TestUser");
        var dto = new CreateReviewDto { MediaId = 1, MediaType = "Movie", Rating = 1, Comment = "Min" };
        _mockRepo.Setup(r => r.CreateReviewAsync(It.IsAny<Review>()))
            .ReturnsAsync((Review r) => { r.Id = "id1"; return r; });

        var result = await _controller.CreateReview(dto);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
    }

    [Test]
    public async Task CreateReview_WithMaxRating_ShouldSucceed()
    {
        SetupUserClaims("user123", "TestUser");
        var dto = new CreateReviewDto { MediaId = 1, MediaType = "Movie", Rating = 5, Comment = "Max" };
        _mockRepo.Setup(r => r.CreateReviewAsync(It.IsAny<Review>()))
            .ReturnsAsync((Review r) => { r.Id = "id5"; return r; });

        var result = await _controller.CreateReview(dto);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
    }

    [Test]
    public async Task CreateReview_WithNoClaims_ShouldUseDefaultValues()
    {
        // Sin claims — debería usar "UnknownID" y "Unknown"
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };
        var dto = new CreateReviewDto { MediaId = 1, MediaType = "Movie", Rating = 3, Comment = "Test" };
        _mockRepo.Setup(r => r.CreateReviewAsync(It.IsAny<Review>()))
            .ReturnsAsync((Review r) => { r.Id = "id"; return r; });

        var result = await _controller.CreateReview(dto);

        var created = result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        var reviewDto = created!.Value as ReviewDto;
        Assert.That(reviewDto!.UserId, Is.EqualTo("UnknownID"));
        Assert.That(reviewDto.UserName, Is.EqualTo("Unknown"));
    }

    // ── UpdateReview ──────────────────────────────────────────────────────

    [Test]
    public async Task UpdateReview_WithValidData_ShouldReturnNoContent()
    {
        SetupUserClaims("user123", "TestUser");
        var existing = new Review { Id = "r1", Rating = 3, Comment = "Old" };
        _mockRepo.Setup(r => r.GetReviewByIdAsync("r1")).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.UpdateReviewAsync("r1", It.IsAny<Review>())).ReturnsAsync(existing);

        var dto = new CreateReviewDto { MediaId = 1, MediaType = "Movie", Rating = 5, Comment = "Updated" };
        var result = await _controller.UpdateReview("r1", dto);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task UpdateReview_WithInvalidRating_ShouldReturnBadRequest()
    {
        SetupUserClaims("user123", "TestUser");
        var dto = new CreateReviewDto { MediaId = 1, MediaType = "Movie", Rating = 0, Comment = "Invalid" };

        var result = await _controller.UpdateReview("r1", dto);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UpdateReview_WhenNotFound_ShouldReturnNotFound()
    {
        SetupUserClaims("user123", "TestUser");
        _mockRepo.Setup(r => r.GetReviewByIdAsync("nonexistent")).ReturnsAsync((Review?)null);

        var dto = new CreateReviewDto { MediaId = 1, MediaType = "Movie", Rating = 4, Comment = "Test" };
        var result = await _controller.UpdateReview("nonexistent", dto);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task UpdateReview_WhenUpdateFails_ShouldReturnNotFound()
    {
        SetupUserClaims("user123", "TestUser");
        var existing = new Review { Id = "r1", Rating = 3, Comment = "Old" };
        _mockRepo.Setup(r => r.GetReviewByIdAsync("r1")).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.UpdateReviewAsync("r1", It.IsAny<Review>())).ReturnsAsync((Review?)null);

        var dto = new CreateReviewDto { MediaId = 1, MediaType = "Movie", Rating = 4, Comment = "Updated" };
        var result = await _controller.UpdateReview("r1", dto);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task UpdateReview_ShouldModifyRatingAndComment()
    {
        SetupUserClaims("user123", "TestUser");
        var existing = new Review { Id = "r1", Rating = 3, Comment = "Old" };
        _mockRepo.Setup(r => r.GetReviewByIdAsync("r1")).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.UpdateReviewAsync("r1", It.IsAny<Review>()))
            .ReturnsAsync((string _, Review r) => r);

        var dto = new CreateReviewDto { MediaId = 1, MediaType = "Movie", Rating = 5, Comment = "Updated" };
        await _controller.UpdateReview("r1", dto);

        _mockRepo.Verify(r => r.UpdateReviewAsync("r1", It.Is<Review>(rev =>
            rev.Rating == 5 && rev.Comment == "Updated")), Times.Once);
    }

    // ── DeleteReview ──────────────────────────────────────────────────────

    [Test]
    public async Task DeleteReview_WhenExists_ShouldReturnNoContent()
    {
        SetupUserClaims("user123", "TestUser");
        var existing = new Review { Id = "r1", MediaId = 1, MediaType = "Movie" };
        _mockRepo.Setup(r => r.GetReviewByIdAsync("r1")).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.DeleteReviewAsync("r1")).ReturnsAsync(true);

        var result = await _controller.DeleteReview("r1");

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeleteReview_WhenNotFound_ShouldReturnNotFound()
    {
        SetupUserClaims("user123", "TestUser");
        _mockRepo.Setup(r => r.DeleteReviewAsync("nonexistent")).ReturnsAsync(false);

        var result = await _controller.DeleteReview("nonexistent");

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    // ── FixUnknownReviews ─────────────────────────────────────────────────

    [Test]
    public async Task FixUnknownReviews_WithValidUser_ShouldReturnOk()
    {
        SetupUserClaims("user123", "TestUser");
        _mockRepo.Setup(r => r.FixUnknownReviewsAsync("user123", "TestUser")).ReturnsAsync(5);

        var result = await _controller.FixUnknownReviews();

        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));
    }

    [Test]
    public async Task FixUnknownReviews_WithUnknownUsername_ShouldReturnBadRequest()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user123"),
            new("unique_name", "Unknown")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        var result = await _controller.FixUnknownReviews();

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task FixUnknownReviews_WithNoClaims_ShouldReturnBadRequest()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };

        var result = await _controller.FixUnknownReviews();

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task FixUnknownReviews_WithZeroUpdated_ShouldStillReturnOk()
    {
        SetupUserClaims("user123", "TestUser");
        _mockRepo.Setup(r => r.FixUnknownReviewsAsync("user123", "TestUser")).ReturnsAsync(0);

        var result = await _controller.FixUnknownReviews();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    // ── DebugClaims ───────────────────────────────────────────────────────

    [Test]
    public void DebugClaims_WithClaims_ShouldReturnOk()
    {
        SetupUserClaims("user123", "TestUser");

        var result = _controller.DebugClaims();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    // ── Dto mapping correctness ───────────────────────────────────────────

    [Test]
    public async Task GetReviewsByMedia_ShouldMapAllFieldsCorrectly()
    {
        var now = DateTime.UtcNow;
        var reviews = new List<Review>
        {
            new() { Id = "r1", UserId = "u1", MediaId = 42, MediaType = "TvShow", UserName = "Alice", Rating = 4, Comment = "Good!", CreatedAt = now }
        };
        _mockRepo.Setup(r => r.GetReviewsByMediaAsync(42, "TvShow")).ReturnsAsync(reviews);

        var result = await _controller.GetReviewsByMedia("TvShow", 42);
        var okResult = result as OkObjectResult;
        var dtos = okResult!.Value as List<ReviewDto>;

        Assert.That(dtos![0].Id, Is.EqualTo("r1"));
        Assert.That(dtos[0].UserId, Is.EqualTo("u1"));
        Assert.That(dtos[0].MediaId, Is.EqualTo(42));
        Assert.That(dtos[0].MediaType, Is.EqualTo("TvShow"));
        Assert.That(dtos[0].UserName, Is.EqualTo("Alice"));
        Assert.That(dtos[0].Rating, Is.EqualTo(4));
        Assert.That(dtos[0].Comment, Is.EqualTo("Good!"));
        Assert.That(dtos[0].CreatedAt, Is.EqualTo(now));
    }
}