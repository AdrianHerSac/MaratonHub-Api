using MaratonHub.Api.Reviews.Controllers;
using MaratonHub.Api.Reviews.Dtos;
using MaratonHub.Api.Reviews.Models;
using MaratonHub.Api.Reviews.Reposytory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;
using MaratonHub.Api.Groups.Dtos;

namespace MaratonHub.ApiTest.Reviews;

public class ReviewsControllerExtendedTests
{
    private Mock<IReviewRepository> _mockRepo = null!;
    private ReviewsController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _mockRepo = new Mock<IReviewRepository>();

        var mockHubContext = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MaratonHub.Api.Groups.Hubs.ChatHub>>();
        var mockClients = new Mock<Microsoft.AspNetCore.SignalR.IHubClients>();
        var mockClientProxy = new Mock<Microsoft.AspNetCore.SignalR.IClientProxy>();
        mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);

        _controller = new ReviewsController(_mockRepo.Object, mockHubContext.Object);

        // Mock HttpContext with User Claims
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user123"),
            new Claim("unique_name", "testuser"),
            new Claim(ClaimTypes.Name, "testuser")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Test]
    public void DebugClaims_ReturnsOk()
    {
        var result = _controller.DebugClaims();
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetTopRated_ReturnsOkWithMappedType()
    {
        var mockReviews = new List<TopRatedAppMediaDto> { new TopRatedAppMediaDto { MediaId = 1 } };
        _mockRepo.Setup(r => r.GetTopRatedMediaAsync("Movie", 1, 20)).ReturnsAsync(mockReviews);

        var result = await _controller.GetTopRated("movie");
        
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        var list = okResult!.Value as List<TopRatedAppMediaDto>;
        Assert.That(list!.Count, Is.EqualTo(1));
        Assert.That(list[0].MediaType, Is.EqualTo("movie"));
    }

    [Test]
    public async Task GetAverageRating_ReturnsOk()
    {
        var dto = new RatingAverageDto { Average = 4.5 };
        _mockRepo.Setup(r => r.GetAverageRatingAsync(1, "movie")).ReturnsAsync(dto);
        var result = await _controller.GetAverageRating("movie", 1);
        var okResult = result as OkObjectResult;
        Assert.That(okResult!.Value, Is.EqualTo(dto));
    }

    [Test]
    public async Task GetBatchAverageRating_ReturnsOk()
    {
        var items = new List<MediaIdentifier>();
        var averages = new Dictionary<string, RatingAverageDto>();
        _mockRepo.Setup(r => r.GetBatchAveragesAsync(items)).ReturnsAsync(averages);
        
        var result = await _controller.GetBatchAverageRating(items);
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetReviewsByMedia_ReturnsOkWithDtos()
    {
        var reviews = new List<Review> { new Review { Id = "1", Rating = 5 } };
        _mockRepo.Setup(r => r.GetReviewsByMediaAsync(1, "movie")).ReturnsAsync(reviews);

        var result = await _controller.GetReviewsByMedia("movie", 1);
        var okResult = result as OkObjectResult;
        var dtos = okResult!.Value as List<ReviewDto>;
        Assert.That(dtos!.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task GetReviewsByUser_ReturnsOkWithDtos()
    {
        var reviews = new List<Review> { new Review { Id = "1", Rating = 5 } };
        _mockRepo.Setup(r => r.GetReviewsByUserAsync("testuser")).ReturnsAsync(reviews);

        var result = await _controller.GetReviewsByUser("testuser");
        var okResult = result as OkObjectResult;
        var dtos = okResult!.Value as List<ReviewDto>;
        Assert.That(dtos!.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task CreateReview_WithValidDto_ReturnsCreatedAtAction()
    {
        var dto = new CreateReviewDto { MediaId = 1, MediaType = "movie", Rating = 4, Comment = "Good" };
        var created = new Review { Id = "new_id", MediaId = 1, MediaType = "movie", Rating = 4, Comment = "Good" };
        
        _mockRepo.Setup(r => r.CreateReviewAsync(It.IsAny<Review>())).ReturnsAsync(created);

        var result = await _controller.CreateReview(dto);
        var createdResult = result as CreatedAtActionResult;
        Assert.That(createdResult, Is.Not.Null);
        var reviewDto = createdResult!.Value as ReviewDto;
        Assert.That(reviewDto!.Id, Is.EqualTo("new_id"));
    }

    [Test]
    public async Task CreateReview_WithInvalidRating_ReturnsBadRequest()
    {
        var dto = new CreateReviewDto { Rating = 6 };
        var result = await _controller.CreateReview(dto);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UpdateReview_WithValidDto_ReturnsNoContent()
    {
        var dto = new CreateReviewDto { Rating = 4, Comment = "Updated" };
        var existing = new Review { Id = "1", Rating = 3 };
        
        _mockRepo.Setup(r => r.GetReviewByIdAsync("1")).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.UpdateReviewAsync("1", existing)).ReturnsAsync(existing);

        var result = await _controller.UpdateReview("1", dto);
        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task UpdateReview_WhenNotFound_ReturnsNotFound()
    {
        var dto = new CreateReviewDto { Rating = 4 };
        _mockRepo.Setup(r => r.GetReviewByIdAsync("1")).ReturnsAsync((Review?)null);

        var result = await _controller.UpdateReview("1", dto);
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task UpdateReview_WithInvalidRating_ReturnsBadRequest()
    {
        var dto = new CreateReviewDto { Rating = 0 };
        var result = await _controller.UpdateReview("1", dto);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task DeleteReview_WhenExists_ReturnsNoContent()
    {
        var existing = new Review { Id = "1", MediaId = 1, MediaType = "Movie" };
        _mockRepo.Setup(r => r.GetReviewByIdAsync("1")).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.DeleteReviewAsync("1")).ReturnsAsync(true);
        var result = await _controller.DeleteReview("1");
        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeleteReview_WhenNotFound_ReturnsNotFound()
    {
        _mockRepo.Setup(r => r.DeleteReviewAsync("1")).ReturnsAsync(false);
        var result = await _controller.DeleteReview("1");
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task FixUnknownReviews_ReturnsOk()
    {
        _mockRepo.Setup(r => r.FixUnknownReviewsAsync("user123", "testuser")).ReturnsAsync(5);
        var result = await _controller.FixUnknownReviews();
        
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }
}
