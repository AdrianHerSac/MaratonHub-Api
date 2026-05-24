using MaratonHub.Api.Notifications.Controllers;
using MaratonHub.Api.Notifications.Dtos;
using MaratonHub.Api.Notifications.Models;
using MaratonHub.Api.Notifications.Repositories;
using MaratonHub.Api.Notifications.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Moq;
using System.Security.Claims;

namespace MaratonHub.ApiTest.Notifications;

// ══════════════════════════════════════════════════════════════════════════════
// Notification Model Tests
// ══════════════════════════════════════════════════════════════════════════════

public class NotificationModelTests
{
    [Test]
    public void Notification_ShouldInitializeWithDefaultValues()
    {
        var n = new Notification();
        Assert.That(n.Id, Is.Null);
        Assert.That(n.UserId, Is.EqualTo(string.Empty));
        Assert.That(n.Title, Is.EqualTo(string.Empty));
        Assert.That(n.Message, Is.EqualTo(string.Empty));
        Assert.That(n.Type, Is.EqualTo("info"));
        Assert.That(n.GroupId, Is.Null);
        Assert.That(n.Read, Is.False);
    }

    [Test]
    public void Notification_ShouldAllowSettingProperties()
    {
        var n = new Notification
        {
            Id = "n1",
            UserId = "u1",
            Title = "Test",
            Message = "Hello",
            Type = "success",
            GroupId = "g1",
            Read = true
        };
        Assert.That(n.Id, Is.EqualTo("n1"));
        Assert.That(n.UserId, Is.EqualTo("u1"));
        Assert.That(n.Title, Is.EqualTo("Test"));
        Assert.That(n.Message, Is.EqualTo("Hello"));
        Assert.That(n.Type, Is.EqualTo("success"));
        Assert.That(n.GroupId, Is.EqualTo("g1"));
        Assert.That(n.Read, Is.True);
    }

    [Test]
    public void Notification_ShouldHaveBsonIdAttribute()
    {
        var prop = typeof(Notification).GetProperty("Id");
        var attrs = prop!.GetCustomAttributes(typeof(MongoDB.Bson.Serialization.Attributes.BsonIdAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
    }

    [Test]
    public void Notification_CreatedAt_ShouldDefaultToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var n = new Notification();
        var after = DateTime.UtcNow.AddSeconds(1);
        Assert.That(n.CreatedAt, Is.InRange(before, after));
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// NotificationDto Tests
// ══════════════════════════════════════════════════════════════════════════════

public class NotificationDtoTests
{
    [Test]
    public void NotificationDto_ShouldInitializeWithDefaultValues()
    {
        var dto = new NotificationDto();
        Assert.That(dto.Id, Is.EqualTo(string.Empty));
        Assert.That(dto.UserId, Is.EqualTo(string.Empty));
        Assert.That(dto.Title, Is.EqualTo(string.Empty));
        Assert.That(dto.Message, Is.EqualTo(string.Empty));
        Assert.That(dto.Type, Is.EqualTo("info"));
        Assert.That(dto.GroupId, Is.Null);
        Assert.That(dto.Read, Is.False);
    }

    [Test]
    public void NotificationDto_ShouldAllowSettingAllProperties()
    {
        var now = DateTime.UtcNow;
        var dto = new NotificationDto
        {
            Id = "n1",
            UserId = "u1",
            Title = "Test",
            Message = "Message",
            Type = "warning",
            GroupId = "g1",
            Read = true,
            CreatedAt = now
        };
        Assert.That(dto.Id, Is.EqualTo("n1"));
        Assert.That(dto.Type, Is.EqualTo("warning"));
        Assert.That(dto.Read, Is.True);
        Assert.That(dto.CreatedAt, Is.EqualTo(now));
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// NotificationsController Tests
// ══════════════════════════════════════════════════════════════════════════════

public class NotificationsControllerTests
{
    private Mock<INotificationRepository> _mockRepo = null!;
    private NotificationsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<INotificationRepository>();
        _controller = new NotificationsController(_mockRepo.Object);
        SetupUserClaims("user1");
    }

    private void SetupUserClaims(string userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    // ── GetNotifications ──────────────────────────────────────────────────

    [Test]
    public async Task GetNotifications_ShouldReturnOkWithDtos()
    {
        var notifications = new List<Notification>
        {
            new() { Id = "n1", UserId = "user1", Title = "Test", Message = "Hello", Type = "info" },
            new() { Id = "n2", UserId = "user1", Title = "Test2", Message = "World", Type = "success", Read = true }
        };
        _mockRepo.Setup(r => r.GetUserNotificationsAsync("user1", 50)).ReturnsAsync(notifications);

        var result = await _controller.GetNotifications();

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var dtos = ok!.Value as List<NotificationDto>;
        Assert.That(dtos, Is.Not.Null);
        Assert.That(dtos!.Count, Is.EqualTo(2));
        Assert.That(dtos[0].Title, Is.EqualTo("Test"));
        Assert.That(dtos[1].Read, Is.True);
    }

    [Test]
    public async Task GetNotifications_WithEmptyList_ShouldReturnOkWithEmptyList()
    {
        _mockRepo.Setup(r => r.GetUserNotificationsAsync("user1", 50)).ReturnsAsync(new List<Notification>());

        var result = await _controller.GetNotifications();

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var dtos = ok!.Value as List<NotificationDto>;
        Assert.That(dtos!.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task GetNotifications_ShouldMapAllFields()
    {
        var now = DateTime.UtcNow;
        var notifications = new List<Notification>
        {
            new() { Id = "n1", UserId = "user1", Title = "T", Message = "M", Type = "warning", GroupId = "g1", Read = false, CreatedAt = now }
        };
        _mockRepo.Setup(r => r.GetUserNotificationsAsync("user1", 50)).ReturnsAsync(notifications);

        var result = await _controller.GetNotifications();
        var ok = result as OkObjectResult;
        var dtos = ok!.Value as List<NotificationDto>;

        Assert.That(dtos![0].Id, Is.EqualTo("n1"));
        Assert.That(dtos[0].UserId, Is.EqualTo("user1"));
        Assert.That(dtos[0].Title, Is.EqualTo("T"));
        Assert.That(dtos[0].Message, Is.EqualTo("M"));
        Assert.That(dtos[0].Type, Is.EqualTo("warning"));
        Assert.That(dtos[0].GroupId, Is.EqualTo("g1"));
        Assert.That(dtos[0].Read, Is.False);
        Assert.That(dtos[0].CreatedAt, Is.EqualTo(now));
    }

    // ── GetUnreadCount ────────────────────────────────────────────────────

    [Test]
    public async Task GetUnreadCount_ShouldReturnOkWithCount()
    {
        _mockRepo.Setup(r => r.GetUnreadCountAsync("user1")).ReturnsAsync(5);

        var result = await _controller.GetUnreadCount();

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var json = System.Text.Json.JsonSerializer.Serialize(ok!.Value);
        Assert.That(json, Does.Contain("5"));
    }

    [Test]
    public async Task GetUnreadCount_WithZeroCount_ShouldReturnOkWithZero()
    {
        _mockRepo.Setup(r => r.GetUnreadCountAsync("user1")).ReturnsAsync(0);

        var result = await _controller.GetUnreadCount();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    // ── MarkAsRead ────────────────────────────────────────────────────────

    [Test]
    public async Task MarkAsRead_WhenExists_ShouldReturnNoContent()
    {
        _mockRepo.Setup(r => r.MarkAsReadAsync("n1")).ReturnsAsync(true);

        var result = await _controller.MarkAsRead("n1");

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task MarkAsRead_WhenNotFound_ShouldReturnNotFound()
    {
        _mockRepo.Setup(r => r.MarkAsReadAsync("nonexistent")).ReturnsAsync(false);

        var result = await _controller.MarkAsRead("nonexistent");

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    // ── MarkAllAsRead ─────────────────────────────────────────────────────

    [Test]
    public async Task MarkAllAsRead_ShouldReturnNoContent()
    {
        _mockRepo.Setup(r => r.MarkAllAsReadAsync("user1")).ReturnsAsync(true);

        var result = await _controller.MarkAllAsRead();

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task MarkAllAsRead_ShouldCallRepositoryWithUserId()
    {
        _mockRepo.Setup(r => r.MarkAllAsReadAsync("user1")).ReturnsAsync(true);

        await _controller.MarkAllAsRead();

        _mockRepo.Verify(r => r.MarkAllAsReadAsync("user1"), Times.Once);
    }

    // ── DeleteNotification ────────────────────────────────────────────────

    [Test]
    public async Task DeleteNotification_WhenExists_ShouldReturnNoContent()
    {
        _mockRepo.Setup(r => r.DeleteAsync("n1")).ReturnsAsync(true);

        var result = await _controller.DeleteNotification("n1");

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeleteNotification_WhenNotFound_ShouldReturnNotFound()
    {
        _mockRepo.Setup(r => r.DeleteAsync("nonexistent")).ReturnsAsync(false);

        var result = await _controller.DeleteNotification("nonexistent");

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    // ── ClearAll ──────────────────────────────────────────────────────────

    [Test]
    public async Task ClearAll_ShouldReturnNoContent()
    {
        _mockRepo.Setup(r => r.ClearAllAsync("user1")).ReturnsAsync(true);

        var result = await _controller.ClearAll();

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task ClearAll_ShouldCallRepositoryWithUserId()
    {
        _mockRepo.Setup(r => r.ClearAllAsync("user1")).ReturnsAsync(true);

        await _controller.ClearAll();

        _mockRepo.Verify(r => r.ClearAllAsync("user1"), Times.Once);
    }

    // ── Controller Attribute Tests ────────────────────────────────────────

    [Test]
    public void NotificationsController_ShouldHaveApiControllerAttribute()
    {
        var attrs = typeof(NotificationsController).GetCustomAttributes(typeof(ApiControllerAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
    }

    [Test]
    public void NotificationsController_ShouldHaveAuthorizeAttribute()
    {
        var attrs = typeof(NotificationsController).GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
    }

    [Test]
    public void NotificationsController_ShouldHaveRouteAttribute()
    {
        var attrs = typeof(NotificationsController).GetCustomAttributes(typeof(RouteAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
        Assert.That(((RouteAttribute)attrs[0]).Template, Is.EqualTo("api/[controller]"));
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// INotificationRepository Tests
// ══════════════════════════════════════════════════════════════════════════════

public class INotificationRepositoryTests
{
    [Test]
    public void ShouldDefineAllMethods()
    {
        var t = typeof(INotificationRepository);
        Assert.That(t.GetMethod("GetUserNotificationsAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("GetUnreadCountAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("CreateAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("MarkAsReadAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("MarkAllAsReadAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("DeleteAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("ClearAllAsync"), Is.Not.Null);
    }

    [Test]
    public void GetUnreadCountAsync_ShouldReturnTaskOfLong()
    {
        var m = typeof(INotificationRepository).GetMethod("GetUnreadCountAsync");
        Assert.That(m!.ReturnType, Is.EqualTo(typeof(Task<long>)));
    }

    [Test]
    public void NotificationRepository_ShouldImplementINotificationRepository()
    {
        Assert.That(typeof(NotificationRepository).GetInterface(nameof(INotificationRepository)), Is.Not.Null);
    }

    [Test]
    public void NotificationRepository_ShouldHaveConstructorWithIMongoDatabase()
    {
        var ctors = typeof(NotificationRepository).GetConstructors();
        Assert.That(ctors.Length, Is.EqualTo(1));
        var p = ctors[0].GetParameters();
        Assert.That(p.Length, Is.EqualTo(1));
        Assert.That(p[0].ParameterType, Is.EqualTo(typeof(IMongoDatabase)));
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// INotificationService Tests
// ══════════════════════════════════════════════════════════════════════════════

public class INotificationServiceTests
{
    [Test]
    public void ShouldDefineCreateAndSendAsync()
    {
        var m = typeof(INotificationService).GetMethod("CreateAndSendAsync");
        Assert.That(m, Is.Not.Null);
        var p = m!.GetParameters();
        Assert.That(p.Length, Is.EqualTo(5));
        Assert.That(p[0].Name, Is.EqualTo("userId"));
        Assert.That(p[1].Name, Is.EqualTo("title"));
        Assert.That(p[2].Name, Is.EqualTo("message"));
        Assert.That(p[3].Name, Is.EqualTo("type"));
        Assert.That(p[4].Name, Is.EqualTo("groupId"));
    }

    [Test]
    public void CreateAndSendAsync_TypeAndGroupId_ShouldBeOptional()
    {
        var m = typeof(INotificationService).GetMethod("CreateAndSendAsync");
        var p = m!.GetParameters();
        Assert.That(p[3].IsOptional, Is.True);
        Assert.That(p[4].IsOptional, Is.True);
    }

    [Test]
    public void NotificationService_ShouldImplementINotificationService()
    {
        Assert.That(typeof(NotificationService).GetInterface(nameof(INotificationService)), Is.Not.Null);
    }
}
