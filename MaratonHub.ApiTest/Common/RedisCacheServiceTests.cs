using MaratonHub.Api.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MaratonHub.ApiTest.Common;

public class IRedisCacheServiceTests
{
    [Test]
    public void ShouldBeAnInterface()
    {
        Assert.That(typeof(IRedisCacheService).IsInterface, Is.True);
    }

    [Test]
    public void ShouldDefineGetAsync()
    {
        var method = typeof(IRedisCacheService).GetMethod("GetAsync");
        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType.Name, Does.Contain("Task"));
        Assert.That(method.IsGenericMethod, Is.True);
    }

    [Test]
    public void ShouldDefineSetAsync()
    {
        var method = typeof(IRedisCacheService).GetMethod("SetAsync");
        Assert.That(method, Is.Not.Null);
        Assert.That(method!.IsGenericMethod, Is.True);
    }

    [Test]
    public void ShouldDefineRemoveAsync()
    {
        var method = typeof(IRedisCacheService).GetMethod("RemoveAsync");
        Assert.That(method, Is.Not.Null);
        Assert.That(method!.IsGenericMethod, Is.False);
    }

    [Test]
    public void GetAsync_ShouldHaveKeyParameter()
    {
        var method = typeof(IRedisCacheService).GetMethod("GetAsync");
        var parameters = method!.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(1));
        Assert.That(parameters[0].Name, Is.EqualTo("key"));
        Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void SetAsync_ShouldHaveThreeParameters()
    {
        var method = typeof(IRedisCacheService).GetMethod("SetAsync");
        var parameters = method!.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(3));
        Assert.That(parameters[0].Name, Is.EqualTo("key"));
        Assert.That(parameters[1].Name, Is.EqualTo("value"));
        Assert.That(parameters[2].Name, Is.EqualTo("expiry"));
    }

    [Test]
    public void SetAsync_ExpiryParameter_ShouldBeOptional()
    {
        var method = typeof(IRedisCacheService).GetMethod("SetAsync");
        var parameters = method!.GetParameters();
        Assert.That(parameters[2].IsOptional, Is.True);
    }

    [Test]
    public void RemoveAsync_ShouldHaveKeyParameter()
    {
        var method = typeof(IRedisCacheService).GetMethod("RemoveAsync");
        var parameters = method!.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(1));
        Assert.That(parameters[0].Name, Is.EqualTo("key"));
    }

    [Test]
    public void GetAsync_ShouldHaveClassConstraint()
    {
        var method = typeof(IRedisCacheService).GetMethod("GetAsync");
        var genericArgs = method!.GetGenericArguments();
        Assert.That(genericArgs.Length, Is.EqualTo(1));
        var constraints = genericArgs[0].GetGenericParameterConstraints();
        // "where T : class" — the constraint is System.Object (reference type)
        Assert.That(genericArgs[0].GenericParameterAttributes.HasFlag(
            System.Reflection.GenericParameterAttributes.ReferenceTypeConstraint), Is.True);
    }

    [Test]
    public void ShouldHaveExactly3Methods()
    {
        var methods = typeof(IRedisCacheService).GetMethods();
        Assert.That(methods.Length, Is.EqualTo(3));
    }
}

public class RedisCacheServiceTests
{
    private Mock<IConfiguration> _mockConfig = null!;
    private Mock<ILogger<RedisCacheService>> _mockLogger = null!;

    [SetUp]
    public void SetUp()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<RedisCacheService>>();
    }

    [Test]
    public void RedisCacheService_ShouldImplementIRedisCacheService()
    {
        Assert.That(typeof(RedisCacheService).GetInterface(nameof(IRedisCacheService)), Is.Not.Null);
    }

    [Test]
    public void Constructor_WhenConnectionFails_LogsWarningAndLeavesDatabaseNull()
    {
        // Use an invalid hostname/port and 1ms timeout to force an immediate exception
        _mockConfig.Setup(c => c["RedisSettings:ConnectionString"]).Returns("255.255.255.255:9999,connectTimeout=1");

        var service = new RedisCacheService(_mockConfig.Object, _mockLogger.Object);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Redis connection failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetAsync_WhenDatabaseIsNull_ReturnsNull()
    {
        _mockConfig.Setup(c => c["RedisSettings:ConnectionString"]).Returns("255.255.255.255:9999,connectTimeout=1");
        var service = new RedisCacheService(_mockConfig.Object, _mockLogger.Object);

        var result = await service.GetAsync<object>("any_key");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task SetAsync_WhenDatabaseIsNull_DoesNotThrow()
    {
        _mockConfig.Setup(c => c["RedisSettings:ConnectionString"]).Returns("255.255.255.255:9999,connectTimeout=1");
        var service = new RedisCacheService(_mockConfig.Object, _mockLogger.Object);

        Assert.DoesNotThrowAsync(async () => await service.SetAsync("any_key", new { Name = "test" }));
    }

    [Test]
    public async Task RemoveAsync_WhenDatabaseIsNull_DoesNotThrow()
    {
        _mockConfig.Setup(c => c["RedisSettings:ConnectionString"]).Returns("255.255.255.255:9999,connectTimeout=1");
        var service = new RedisCacheService(_mockConfig.Object, _mockLogger.Object);

        Assert.DoesNotThrowAsync(async () => await service.RemoveAsync("any_key"));
    }
}