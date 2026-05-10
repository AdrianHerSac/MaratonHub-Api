using MaratonHub.Api.UserMedia.Models;
using MaratonHub.Api.UserMedia;
using MaratonHub.Api.UserMedia.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MediaModel = MaratonHub.Api.UserMedia.Models.Media;

namespace MaratonHub.ApiTest.UserMedia;

public class MediaModelTests
{
    [Test]
    public void Media_ShouldInitializeWithDefaultValues()
    {
        var media = new MediaModel();
        Assert.That(media.Id, Is.Null);
        Assert.That(media.ExternalApiId, Is.EqualTo(0));
        Assert.That(media.Title, Is.EqualTo(string.Empty));
        Assert.That(media.MediaType, Is.EqualTo(string.Empty));
        Assert.That(media.PosterPath, Is.EqualTo(string.Empty));
        Assert.That(media.SeriesInfo, Is.Null);
    }

    [Test]
    public void Media_ShouldAllowSettingProperties()
    {
        var media = new MediaModel
        {
            Id = "media123", ExternalApiId = 100, Title = "Test Movie",
            MediaType = "Movie", PosterPath = "/poster.jpg",
            SeriesInfo = new SeriesDetail { TotalSeasons = 3 }
        };
        Assert.That(media.Id, Is.EqualTo("media123"));
        Assert.That(media.ExternalApiId, Is.EqualTo(100));
        Assert.That(media.SeriesInfo!.TotalSeasons, Is.EqualTo(3));
    }

    [Test]
    public void Media_ShouldHaveBsonIdAttribute()
    {
        var prop = typeof(MediaModel).GetProperty("Id");
        var attrs = prop!.GetCustomAttributes(typeof(MongoDB.Bson.Serialization.Attributes.BsonIdAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
    }
}

public class SeriesDetailTests
{
    [Test]
    public void SeriesDetail_ShouldInitializeWithDefaultValues()
    {
        var d = new SeriesDetail();
        Assert.That(d.TotalSeasons, Is.EqualTo(0));
        Assert.That(d.TotalEpisodes, Is.EqualTo(0));
        Assert.That(d.EpisodeRuntimeMinutes, Is.EqualTo(0));
        Assert.That(d.Status, Is.EqualTo(string.Empty));
        Assert.That(d.Overview, Is.EqualTo(string.Empty));
        Assert.That(d.Network, Is.EqualTo(string.Empty));
        Assert.That(d.Genre, Is.EqualTo(string.Empty));
        Assert.That(d.NextEpisodeToAir, Is.Null);
    }

    [Test]
    public void SeriesDetail_ShouldAllowSettingAllProperties()
    {
        var d = new SeriesDetail
        {
            TotalSeasons = 5, TotalEpisodes = 62, EpisodeRuntimeMinutes = 45,
            Status = "Ended", Overview = "Great show", FirstAirDate = "2020-01-01",
            Network = "Netflix", Genre = "Drama", Language = "English",
            Country = "USA", NextEpisodeToAir = 10, Popularity = 100, TimeToken = 1
        };
        Assert.That(d.TotalSeasons, Is.EqualTo(5));
        Assert.That(d.Network, Is.EqualTo("Netflix"));
        Assert.That(d.Popularity, Is.EqualTo(100));
    }
}

public class FilmDetailTests
{
    [Test]
    public void FilmDetail_ShouldInitializeWithDefaults()
    {
        var f = new FilmDetail();
        Assert.That(f.MediaId, Is.EqualTo(0));
        Assert.That(f.RuntimeMinutes, Is.EqualTo(0));
        Assert.That(f.Tagline, Is.EqualTo(string.Empty));
        Assert.That(f.Overview, Is.EqualTo(string.Empty));
        Assert.That(f.Budget, Is.EqualTo(0));
        Assert.That(f.Revenue, Is.EqualTo(0));
    }

    [Test]
    public void FilmDetail_ShouldAllowSettingProperties()
    {
        var f = new FilmDetail { RuntimeMinutes = 120, Tagline = "Test", Budget = 1000000, Revenue = 5000000 };
        Assert.That(f.RuntimeMinutes, Is.EqualTo(120));
        Assert.That(f.Budget, Is.EqualTo(1000000));
    }
}

public class PersonDetailTests
{
    [Test]
    public void PersonDetail_ShouldInitializeWithDefaults()
    {
        var p = new PersonDetail();
        Assert.That(p.Biography, Is.EqualTo(string.Empty));
        Assert.That(p.PlaceOfBirth, Is.EqualTo(string.Empty));
        Assert.That(p.ProfilePath, Is.EqualTo(string.Empty));
        Assert.That(p.KnownForDepartment, Is.EqualTo(string.Empty));
    }
}

public class UserMediaModelTests
{
    [Test]
    public void UserMedia_ShouldInitializeWithDefaultValues()
    {
        var um = new MaratonHub.Api.UserMedia.Models.UserMedia();
        Assert.That(um.Id, Is.EqualTo(0));
        Assert.That(um.UserId, Is.EqualTo(0));
        Assert.That(um.Rating, Is.EqualTo(0));
        Assert.That(um.Review, Is.EqualTo(string.Empty));
        Assert.That(um.Status, Is.EqualTo("Pending"));
    }

    [Test]
    public void UserMedia_CreatedAt_ShouldDefaultToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var um = new MaratonHub.Api.UserMedia.Models.UserMedia();
        var after = DateTime.UtcNow.AddSeconds(1);
        Assert.That(um.CreatedAt, Is.InRange(before, after));
    }
}

public class UserMediaUserModelTests
{
    [Test]
    public void User_ShouldInitializeWithDefaults()
    {
        var u = new MaratonHub.Api.UserMedia.Models.User();
        Assert.That(u.Username, Is.EqualTo(string.Empty));
        Assert.That(u.Email, Is.EqualTo(string.Empty));
        Assert.That(u.IsActive, Is.True);
        Assert.That(u.IsVerified, Is.False);
        Assert.That(u.StreamingPlatforms, Is.Not.Null);
        Assert.That(u.Ratings, Is.Not.Null);
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// MediaController Tests (con Moq)
// ══════════════════════════════════════════════════════════════════════════════

public class MediaControllerTests
{
    private Mock<IMediaService> _mockService = null!;
    private MediaController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mockService = new Mock<IMediaService>();
        _controller = new MediaController(_mockService.Object);
    }

    [Test]
    public void MediaController_ShouldExist()
    {
        var type = Type.GetType("MaratonHub.Api.UserMedia.Controllers.MediaController, MaratonHub.Api");
        Assert.That(type, Is.Not.Null);
    }

    [Test]
    public void MediaController_ShouldHaveApiControllerAttribute()
    {
        var attrs = typeof(MediaController).GetCustomAttributes(typeof(ApiControllerAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
    }

    [Test]
    public async Task GetAll_ShouldReturnOk()
    {
        var media = new List<MediaModel> { new() { Id = "1", Title = "Movie" } };
        _mockService.Setup(s => s.GetAllAsync()).ReturnsAsync(media);
        var result = await _controller.GetAll();
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var ok = result as OkObjectResult;
        var list = ok!.Value as List<MediaModel>;
        Assert.That(list!.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task GetById_WhenExists_ShouldReturnOk()
    {
        var m = new MediaModel { Id = "1", Title = "Test" };
        _mockService.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(m);
        var result = await _controller.GetById("1");
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetById_WhenNotFound_ShouldReturnNotFound()
    {
        _mockService.Setup(s => s.GetByIdAsync("x")).ReturnsAsync((MediaModel?)null);
        var result = await _controller.GetById("x");
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetByMediaType_ShouldReturnOk()
    {
        _mockService.Setup(s => s.GetByMediaTypeAsync("Movie")).ReturnsAsync(new List<MediaModel>());
        var result = await _controller.GetByMediaType("Movie");
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetByTmdbId_WhenExists_ShouldReturnOk()
    {
        var m = new MediaModel { Id = "1", ExternalApiId = 42 };
        _mockService.Setup(s => s.GetByTmdbIdAsync(42, "Movie")).ReturnsAsync(m);
        var result = await _controller.GetByTmdbId(42, "Movie");
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetByTmdbId_WhenNotFound_ShouldReturnNotFound()
    {
        _mockService.Setup(s => s.GetByTmdbIdAsync(999, "Movie")).ReturnsAsync((MediaModel?)null);
        var result = await _controller.GetByTmdbId(999, "Movie");
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Search_WithValidTitle_ShouldReturnOk()
    {
        _mockService.Setup(s => s.SearchByTitleAsync("test")).ReturnsAsync(new List<MediaModel>());
        var result = await _controller.Search("test");
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Search_WithEmptyTitle_ShouldReturnBadRequest()
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

    [Test]
    public async Task Create_ShouldReturnCreatedAtAction()
    {
        var m = new MediaModel { Title = "New", MediaType = "Movie" };
        _mockService.Setup(s => s.CreateAsync(It.IsAny<MediaModel>()))
            .ReturnsAsync((MediaModel media) => { media.Id = "newId"; return media; });
        var result = await _controller.Create(m);
        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
        var created = result as CreatedAtActionResult;
        Assert.That(created!.StatusCode, Is.EqualTo(201));
    }

    [Test]
    public async Task Update_WhenExists_ShouldReturnOk()
    {
        var m = new MediaModel { Title = "Updated" };
        _mockService.Setup(s => s.UpdateAsync("1", It.IsAny<MediaModel>())).ReturnsAsync(m);
        var result = await _controller.Update("1", m);
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Update_WhenNotFound_ShouldReturnNotFound()
    {
        _mockService.Setup(s => s.UpdateAsync("x", It.IsAny<MediaModel>())).ReturnsAsync((MediaModel?)null);
        var result = await _controller.Update("x", new MediaModel());
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Delete_WhenExists_ShouldReturnNoContent()
    {
        _mockService.Setup(s => s.DeleteAsync("1")).ReturnsAsync(true);
        var result = await _controller.Delete("1");
        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Delete_WhenNotFound_ShouldReturnNotFound()
    {
        _mockService.Setup(s => s.DeleteAsync("x")).ReturnsAsync(false);
        var result = await _controller.Delete("x");
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// MediaService Tests (con Moq)
// ══════════════════════════════════════════════════════════════════════════════

public class MediaServiceMockTests
{
    private Mock<IMediaRepository> _mockRepo = null!;
    private MediaService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<IMediaRepository>();
        _service = new MediaService(_mockRepo.Object);
    }

    [Test]
    public async Task GetAllAsync_ShouldDelegateToRepository()
    {
        var list = new List<MediaModel> { new() { Id = "1" } };
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(list);
        var result = await _service.GetAllAsync();
        Assert.That(result.Count, Is.EqualTo(1));
        _mockRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_ShouldDelegateToRepository()
    {
        var m = new MediaModel { Id = "1" };
        _mockRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(m);
        var result = await _service.GetByIdAsync("1");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo("1"));
    }

    [Test]
    public async Task GetByTmdbIdAsync_ShouldCallGetByExternalApiIdAsync()
    {
        _mockRepo.Setup(r => r.GetByExternalApiIdAsync(42, "Movie")).ReturnsAsync(new MediaModel());
        await _service.GetByTmdbIdAsync(42, "Movie");
        _mockRepo.Verify(r => r.GetByExternalApiIdAsync(42, "Movie"), Times.Once);
    }

    [Test]
    public async Task GetByMediaTypeAsync_ShouldDelegateToRepository()
    {
        _mockRepo.Setup(r => r.GetByMediaTypeAsync("tv")).ReturnsAsync(new List<MediaModel>());
        await _service.GetByMediaTypeAsync("tv");
        _mockRepo.Verify(r => r.GetByMediaTypeAsync("tv"), Times.Once);
    }

    [Test]
    public async Task SearchByTitleAsync_ShouldDelegateToRepository()
    {
        _mockRepo.Setup(r => r.SearchByTitleAsync("test")).ReturnsAsync(new List<MediaModel>());
        await _service.SearchByTitleAsync("test");
        _mockRepo.Verify(r => r.SearchByTitleAsync("test"), Times.Once);
    }

    [Test]
    public async Task CreateAsync_ShouldDelegateToRepository()
    {
        var m = new MediaModel { Title = "New" };
        _mockRepo.Setup(r => r.CreateAsync(m)).ReturnsAsync(m);
        var result = await _service.CreateAsync(m);
        Assert.That(result.Title, Is.EqualTo("New"));
    }

    [Test]
    public async Task UpdateAsync_ShouldDelegateToRepository()
    {
        var m = new MediaModel { Title = "Updated" };
        _mockRepo.Setup(r => r.UpdateAsync("1", m)).ReturnsAsync(m);
        var result = await _service.UpdateAsync("1", m);
        Assert.That(result!.Title, Is.EqualTo("Updated"));
    }

    [Test]
    public async Task DeleteAsync_ShouldDelegateToRepository()
    {
        _mockRepo.Setup(r => r.DeleteAsync("1")).ReturnsAsync(true);
        var result = await _service.DeleteAsync("1");
        Assert.That(result, Is.True);
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// Interface Tests
// ══════════════════════════════════════════════════════════════════════════════

public class IMediaRepositoryTests
{
    [Test]
    public void ShouldDefineAllMethods()
    {
        var t = typeof(IMediaRepository);
        Assert.That(t.GetMethod("GetAllAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("GetByIdAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("GetByExternalApiIdAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("GetByMediaTypeAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("SearchByTitleAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("CreateAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("UpdateAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("DeleteAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("UpsertByTmdbIdAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("ExistsAsync"), Is.Not.Null);
    }
}

public class IMediaServiceTests
{
    [Test]
    public void ShouldDefineAllMethods()
    {
        var t = typeof(IMediaService);
        Assert.That(t.GetMethod("GetAllAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("GetByIdAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("GetByTmdbIdAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("GetByMediaTypeAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("SearchByTitleAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("CreateAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("UpdateAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("DeleteAsync"), Is.Not.Null);
    }
}