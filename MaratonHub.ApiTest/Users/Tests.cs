using MaratonHub.Api.Users.Dtos;
using MaratonHub.Api.Users.Models;
using MaratonHub.Api.Users.Repositories;
using MaratonHub.Api.Users.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace MaratonHub.ApiTest.Users;

public class UserModelTests
{
    [Test]
    public void User_ShouldInitializeWithDefaultValues()
    {
        var user = new User();
        Assert.That(user.Id, Is.Null);
        Assert.That(user.Username, Is.EqualTo(string.Empty));
        Assert.That(user.PasswordHash, Is.EqualTo(string.Empty));
        Assert.That(user.GoogleId, Is.Null);
    }

    [Test]
    public void User_ShouldAllowSettingProperties()
    {
        var user = new User
        {
            Id = "user123", Username = "testuser",
            PasswordHash = "hashed", GoogleId = "google123",
            CreatedAt = new DateTime(2024, 1, 1)
        };
        Assert.That(user.Id, Is.EqualTo("user123"));
        Assert.That(user.Username, Is.EqualTo("testuser"));
        Assert.That(user.GoogleId, Is.EqualTo("google123"));
    }

    [Test]
    public void User_CreatedAt_ShouldDefaultToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var user = new User();
        var after = DateTime.UtcNow.AddSeconds(1);
        Assert.That(user.CreatedAt, Is.InRange(before, after));
    }

    [Test]
    public void User_ShouldHaveBsonIdAttribute()
    {
        var prop = typeof(User).GetProperty("Id");
        var attrs = prop!.GetCustomAttributes(typeof(MongoDB.Bson.Serialization.Attributes.BsonIdAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
    }

    [Test]
    public void User_GoogleId_ShouldBeNullable()
    {
        var user = new User { GoogleId = null };
        Assert.That(user.GoogleId, Is.Null);
    }
}

public class RegisterDtoTests
{
    [Test]
    public void RegisterDto_ShouldInitializeWithDefaults()
    {
        var dto = new RegisterDto();
        Assert.That(dto.Username, Is.EqualTo(string.Empty));
        Assert.That(dto.Password, Is.EqualTo(string.Empty));
    }

    [Test]
    public void RegisterDto_ShouldAllowSettingProperties()
    {
        var dto = new RegisterDto { Username = "user", Password = "pass" };
        Assert.That(dto.Username, Is.EqualTo("user"));
        Assert.That(dto.Password, Is.EqualTo("pass"));
    }
}

public class LoginDtoTests
{
    [Test]
    public void LoginDto_ShouldInitializeWithDefaults()
    {
        var dto = new LoginDto();
        Assert.That(dto.Username, Is.EqualTo(string.Empty));
        Assert.That(dto.Password, Is.EqualTo(string.Empty));
    }
}

public class GoogleLoginDtoTests
{
    [Test]
    public void GoogleLoginDto_ShouldInitializeWithDefaults()
    {
        var dto = new GoogleLoginDto();
        Assert.That(dto.IdToken, Is.EqualTo(string.Empty));
    }
}

public class AuthResponseDtoTests
{
    [Test]
    public void AuthResponseDto_ShouldInitializeWithDefaults()
    {
        var dto = new AuthResponseDto();
        Assert.That(dto.Token, Is.EqualTo(string.Empty));
        Assert.That(dto.Username, Is.EqualTo(string.Empty));
    }

    [Test]
    public void AuthResponseDto_ShouldAllowSettingProperties()
    {
        var dto = new AuthResponseDto { Token = "jwt", Username = "user" };
        Assert.That(dto.Token, Is.EqualTo("jwt"));
        Assert.That(dto.Username, Is.EqualTo("user"));
    }
}

public class IUserRepositoryTests
{
    [Test]
    public void ShouldDefineAllMethods()
    {
        var t = typeof(IUserRepository);
        Assert.That(t.GetMethod("GetUserByIdAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("GetUserByUsernameAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("GetUserByGoogleIdAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("CreateUserAsync"), Is.Not.Null);
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// AuthController Tests (con Moq)
// ══════════════════════════════════════════════════════════════════════════════

public class AuthControllerTests
{
    private Mock<IUserRepository> _mockRepo = null!;
    private AuthController _controller = null!;

    private static IConfiguration BuildConfig()
    {
        var inMemory = new Dictionary<string, string?>
        {
            { "JwtSettings:Secret", "Sup3rS3cr3tK3yF0rMarat0nHubApiThatIsL0ngEn0ugh" },
            { "JwtSettings:Issuer", "MaratonHub" },
            { "JwtSettings:Audience", "MaratonHubUsers" },
            { "GoogleAuth:ClientId", "test-client-id" }
        };
        return new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();
    }

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<IUserRepository>();
        _controller = new AuthController(_mockRepo.Object, BuildConfig());
    }

    [Test]
    public void AuthController_ShouldExist()
    {
        var type = Type.GetType("MaratonHub.Api.Users.Controllers.AuthController, MaratonHub.Api");
        Assert.That(type, Is.Not.Null);
    }

    [Test]
    public void AuthController_ShouldHaveApiControllerAttribute()
    {
        var attrs = typeof(AuthController).GetCustomAttributes(typeof(ApiControllerAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
    }

    [Test]
    public void AuthController_ShouldHaveRouteAttribute()
    {
        var attrs = typeof(AuthController).GetCustomAttributes(typeof(RouteAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
        Assert.That(((RouteAttribute)attrs[0]).Template, Is.EqualTo("api/[controller]"));
    }

    // ── Register ──────────────────────────────────────────────────────────

    [Test]
    public async Task Register_WithValidData_ShouldReturnOkWithToken()
    {
        _mockRepo.Setup(r => r.GetUserByUsernameAsync("newuser")).ReturnsAsync((User?)null);
        _mockRepo.Setup(r => r.CreateUserAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => { u.Id = "generatedId"; return u; });

        var dto = new RegisterDto { Username = "newuser", Password = "password123" };
        var result = await _controller.Register(dto);

        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));
    }

    [Test]
    public async Task Register_WithEmptyUsername_ShouldReturnBadRequest()
    {
        var dto = new RegisterDto { Username = "", Password = "password123" };
        var result = await _controller.Register(dto);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Register_WithEmptyPassword_ShouldReturnBadRequest()
    {
        var dto = new RegisterDto { Username = "user", Password = "" };
        var result = await _controller.Register(dto);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Register_WithWhitespaceUsername_ShouldReturnBadRequest()
    {
        var dto = new RegisterDto { Username = "   ", Password = "password" };
        var result = await _controller.Register(dto);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Register_WithExistingUsername_ShouldReturnBadRequest()
    {
        _mockRepo.Setup(r => r.GetUserByUsernameAsync("existing"))
            .ReturnsAsync(new User { Id = "1", Username = "existing" });

        var dto = new RegisterDto { Username = "existing", Password = "password123" };
        var result = await _controller.Register(dto);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Register_ShouldHashPassword()
    {
        _mockRepo.Setup(r => r.GetUserByUsernameAsync("newuser")).ReturnsAsync((User?)null);
        User? capturedUser = null;
        _mockRepo.Setup(r => r.CreateUserAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .ReturnsAsync((User u) => { u.Id = "id"; return u; });

        var dto = new RegisterDto { Username = "newuser", Password = "password123" };
        await _controller.Register(dto);

        Assert.That(capturedUser, Is.Not.Null);
        Assert.That(capturedUser!.PasswordHash, Is.Not.EqualTo("password123"));
        Assert.That(capturedUser.PasswordHash, Is.Not.Empty);
        Assert.That(BCrypt.Net.BCrypt.Verify("password123", capturedUser.PasswordHash), Is.True);
    }

    // ── Login ─────────────────────────────────────────────────────────────

    [Test]
    public async Task Login_WithValidCredentials_ShouldReturnOkWithToken()
    {
        var hashedPw = BCrypt.Net.BCrypt.HashPassword("password123");
        var user = new User { Id = "1", Username = "testuser", PasswordHash = hashedPw };
        _mockRepo.Setup(r => r.GetUserByUsernameAsync("testuser")).ReturnsAsync(user);

        var dto = new LoginDto { Username = "testuser", Password = "password123" };
        var result = await _controller.Login(dto);

        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));
    }

    [Test]
    public async Task Login_WithInvalidPassword_ShouldReturnUnauthorized()
    {
        var hashedPw = BCrypt.Net.BCrypt.HashPassword("correctpassword");
        var user = new User { Id = "1", Username = "testuser", PasswordHash = hashedPw };
        _mockRepo.Setup(r => r.GetUserByUsernameAsync("testuser")).ReturnsAsync(user);

        var dto = new LoginDto { Username = "testuser", Password = "wrongpassword" };
        var result = await _controller.Login(dto);
        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task Login_WithNonExistentUser_ShouldReturnUnauthorized()
    {
        _mockRepo.Setup(r => r.GetUserByUsernameAsync("nobody")).ReturnsAsync((User?)null);

        var dto = new LoginDto { Username = "nobody", Password = "password" };
        var result = await _controller.Login(dto);
        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task Login_WithEmptyPasswordHash_ShouldReturnUnauthorized()
    {
        var user = new User { Id = "1", Username = "testuser", PasswordHash = "" };
        _mockRepo.Setup(r => r.GetUserByUsernameAsync("testuser")).ReturnsAsync(user);

        var dto = new LoginDto { Username = "testuser", Password = "password" };
        var result = await _controller.Login(dto);
        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // ── GoogleLogin ───────────────────────────────────────────────────────

    [Test]
    public async Task GoogleLogin_WithInvalidToken_ShouldReturnUnauthorized()
    {
        var dto = new GoogleLoginDto { IdToken = "invalid_token" };
        var result = await _controller.GoogleLogin(dto);
        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // ── JWT generation ────────────────────────────────────────────────────

    [Test]
    public async Task Register_ShouldReturnResponseWithUsername()
    {
        _mockRepo.Setup(r => r.GetUserByUsernameAsync("newuser")).ReturnsAsync((User?)null);
        _mockRepo.Setup(r => r.CreateUserAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => { u.Id = "id"; return u; });

        var dto = new RegisterDto { Username = "newuser", Password = "password123" };
        var result = await _controller.Register(dto);

        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        // AuthResponseDto will contain token and username
        var responseJson = System.Text.Json.JsonSerializer.Serialize(okResult!.Value);
        Assert.That(responseJson, Does.Contain("newuser"));
    }
}