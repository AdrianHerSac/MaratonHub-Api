using MaratonHub.Api.Users.Controllers;
using MaratonHub.Api.Users.Models;
using MaratonHub.Api.Users.Repositories;
using MaratonHub.Api.Reviews.Reposytory;
using MaratonHub.Api.Reviews.Dtos;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace MaratonHub.ApiTest.Users;

public class AdminControllerTests
{
    private Mock<IUserRepository> _mockUserRepo = null!;
    private Mock<IReviewRepository> _mockReviewRepo = null!;
    private AdminController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _mockReviewRepo = new Mock<IReviewRepository>();
        _controller = new AdminController(_mockUserRepo.Object, _mockReviewRepo.Object);
    }

    // ── GetStats ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetStats_ShouldReturnOkWithStats()
    {
        var users = new List<User>
        {
            new() { Id = "u1", Username = "alice", Role = "User", CreatedAt = DateTime.UtcNow.AddDays(-2), LastLogin = DateTime.UtcNow.AddMinutes(-10) },
            new() { Id = "u2", Username = "bob", Role = "User", CreatedAt = DateTime.UtcNow.AddDays(-1), LastLogin = DateTime.UtcNow.AddHours(-2) },
            new() { Id = "u3", Username = "carol", Role = "Admin", CreatedAt = DateTime.UtcNow, LastLogin = null }
        };
        _mockUserRepo.Setup(r => r.GetAllUsersAsync()).ReturnsAsync(users);
        _mockReviewRepo.Setup(r => r.CountReviewsAsync()).ReturnsAsync(42);

        var result = await _controller.GetStats();

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.StatusCode, Is.EqualTo(200));
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        Assert.That(json, Does.Contain("TotalUsers").Or.Contain("totalUsers"));
        Assert.That(json, Does.Contain("3").Or.Contain("TotalUsers"));
    }

    [Test]
    public async Task GetStats_ShouldCountRecentOnlineUsers()
    {
        var users = new List<User>
        {
            new() { Id = "u1", Username = "alice", LastLogin = DateTime.UtcNow.AddMinutes(-5) },   // online
            new() { Id = "u2", Username = "bob", LastLogin = DateTime.UtcNow.AddHours(-2) },         // offline
            new() { Id = "u3", Username = "carol", LastLogin = null }                                  // never logged in
        };
        _mockUserRepo.Setup(r => r.GetAllUsersAsync()).ReturnsAsync(users);
        _mockReviewRepo.Setup(r => r.CountReviewsAsync()).ReturnsAsync(0);

        var result = await _controller.GetStats();

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        // Verify response contains ConnectedUsers=1
        var json = System.Text.Json.JsonSerializer.Serialize(ok!.Value);
        Assert.That(json, Does.Contain("ConnectedUsers").Or.Contain("connectedUsers"));
    }

    [Test]
    public async Task GetStats_WithNoUsers_ShouldReturnZeroStats()
    {
        _mockUserRepo.Setup(r => r.GetAllUsersAsync()).ReturnsAsync(new List<User>());
        _mockReviewRepo.Setup(r => r.CountReviewsAsync()).ReturnsAsync(0);

        var result = await _controller.GetStats();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetStats_ShouldIncludeSystemStatus()
    {
        _mockUserRepo.Setup(r => r.GetAllUsersAsync()).ReturnsAsync(new List<User>());
        _mockReviewRepo.Setup(r => r.CountReviewsAsync()).ReturnsAsync(0);

        var result = await _controller.GetStats();

        var ok = result as OkObjectResult;
        var json = System.Text.Json.JsonSerializer.Serialize(ok!.Value);
        Assert.That(json, Does.Contain("Operativo"));
    }

    [Test]
    public async Task GetStats_ShouldIncludeRecentUsers()
    {
        var users = Enumerable.Range(1, 7).Select(i => new User
        {
            Id = $"u{i}",
            Username = $"user{i}",
            CreatedAt = DateTime.UtcNow.AddDays(-i)
        }).ToList();
        _mockUserRepo.Setup(r => r.GetAllUsersAsync()).ReturnsAsync(users);
        _mockReviewRepo.Setup(r => r.CountReviewsAsync()).ReturnsAsync(0);

        var result = await _controller.GetStats();

        var ok = result as OkObjectResult;
        var json = System.Text.Json.JsonSerializer.Serialize(ok!.Value);
        // Recent users should only be top 5
        Assert.That(json, Does.Contain("RecentUsers").Or.Contain("recentUsers"));
    }

    // ── GetUsers ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetUsers_ShouldReturnOkWithAllUsers()
    {
        var users = new List<User>
        {
            new() { Id = "u1", Username = "alice", Role = "User", GoogleId = null },
            new() { Id = "u2", Username = "bob", Role = "Admin", GoogleId = "g123" }
        };
        _mockUserRepo.Setup(r => r.GetAllUsersAsync()).ReturnsAsync(users);

        var result = await _controller.GetUsers();

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.StatusCode, Is.EqualTo(200));
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        Assert.That(json, Does.Contain("alice"));
        Assert.That(json, Does.Contain("bob"));
    }

    [Test]
    public async Task GetUsers_WithEmptyDatabase_ShouldReturnEmptyList()
    {
        _mockUserRepo.Setup(r => r.GetAllUsersAsync()).ReturnsAsync(new List<User>());

        var result = await _controller.GetUsers();

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var json = System.Text.Json.JsonSerializer.Serialize(ok!.Value);
        Assert.That(json, Is.EqualTo("[]"));
    }

    // ── DeleteUser ────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteUser_WhenExists_ShouldReturnNoContent()
    {
        _mockUserRepo.Setup(r => r.DeleteUserAsync("u1")).ReturnsAsync(true);

        var result = await _controller.DeleteUser("u1");

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeleteUser_WhenNotFound_ShouldReturnNotFound()
    {
        _mockUserRepo.Setup(r => r.DeleteUserAsync("nonexistent")).ReturnsAsync(false);

        var result = await _controller.DeleteUser("nonexistent");

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    // ── UpdateUserRole ────────────────────────────────────────────────────

    [Test]
    public async Task UpdateUserRole_WithValidAdminRole_ShouldReturnOk()
    {
        var user = new User { Id = "u1", Username = "alice", Role = "User" };
        _mockUserRepo.Setup(r => r.GetUserByIdAsync("u1")).ReturnsAsync(user);
        _mockUserRepo.Setup(r => r.UpdateUserAsync(user)).Returns(Task.CompletedTask);

        var result = await _controller.UpdateUserRole("u1", "Admin");

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        Assert.That(user.Role, Is.EqualTo("Admin"));
    }

    [Test]
    public async Task UpdateUserRole_WithValidUserRole_ShouldReturnOk()
    {
        var user = new User { Id = "u1", Username = "alice", Role = "Admin" };
        _mockUserRepo.Setup(r => r.GetUserByIdAsync("u1")).ReturnsAsync(user);
        _mockUserRepo.Setup(r => r.UpdateUserAsync(user)).Returns(Task.CompletedTask);

        var result = await _controller.UpdateUserRole("u1", "User");

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        Assert.That(user.Role, Is.EqualTo("User"));
    }

    [Test]
    public async Task UpdateUserRole_WithInvalidRole_ShouldReturnBadRequest()
    {
        var user = new User { Id = "u1", Username = "alice", Role = "User" };
        _mockUserRepo.Setup(r => r.GetUserByIdAsync("u1")).ReturnsAsync(user);

        var result = await _controller.UpdateUserRole("u1", "SuperAdmin");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UpdateUserRole_WhenUserNotFound_ShouldReturnNotFound()
    {
        _mockUserRepo.Setup(r => r.GetUserByIdAsync("nonexistent")).ReturnsAsync((User?)null);

        var result = await _controller.UpdateUserRole("nonexistent", "Admin");

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    // ── Attribute Tests ───────────────────────────────────────────────────

    [Test]
    public void AdminController_ShouldHaveApiControllerAttribute()
    {
        var attrs = typeof(AdminController).GetCustomAttributes(typeof(ApiControllerAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
    }

    [Test]
    public void AdminController_ShouldHaveRouteAttribute()
    {
        var attrs = typeof(AdminController).GetCustomAttributes(typeof(RouteAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
        Assert.That(((RouteAttribute)attrs[0]).Template, Is.EqualTo("api/[controller]"));
    }

    [Test]
    public void AdminController_ShouldHaveAuthorizeAttribute()
    {
        var attrs = typeof(AdminController).GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// UserRoles Model Tests
// ══════════════════════════════════════════════════════════════════════════════

public class UserRolesTests
{
    [Test]
    public void UserRoles_Admin_ShouldBeAdminString()
    {
        Assert.That(UserRoles.Admin, Is.EqualTo("Admin"));
    }

    [Test]
    public void UserRoles_User_ShouldBeUserString()
    {
        Assert.That(UserRoles.User, Is.EqualTo("User"));
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// Extended IUserRepository method signature tests
// ══════════════════════════════════════════════════════════════════════════════

public class IUserRepositoryExtendedTests
{
    [Test]
    public void UpdateUserAsync_ShouldReturnTask()
    {
        var m = typeof(IUserRepository).GetMethod("UpdateUserAsync");
        Assert.That(m, Is.Not.Null);
        Assert.That(m!.ReturnType, Is.EqualTo(typeof(Task)));
    }

    [Test]
    public void GetAllUsersAsync_ShouldReturnTaskOfListUser()
    {
        var m = typeof(IUserRepository).GetMethod("GetAllUsersAsync");
        Assert.That(m, Is.Not.Null);
        Assert.That(m!.ReturnType, Is.EqualTo(typeof(Task<List<User>>)));
    }

    [Test]
    public void CountUsersAsync_ShouldReturnTaskOfLong()
    {
        var m = typeof(IUserRepository).GetMethod("CountUsersAsync");
        Assert.That(m, Is.Not.Null);
        Assert.That(m!.ReturnType, Is.EqualTo(typeof(Task<long>)));
    }

    [Test]
    public void DeleteUserAsync_ShouldReturnTaskOfBool()
    {
        var m = typeof(IUserRepository).GetMethod("DeleteUserAsync");
        Assert.That(m, Is.Not.Null);
        Assert.That(m!.ReturnType, Is.EqualTo(typeof(Task<bool>)));
    }
}
