using MaratonHub.Api.Common;

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
    [Test]
    public void RedisCacheService_ShouldImplementIRedisCacheService()
    {
        Assert.That(typeof(RedisCacheService).GetInterface(nameof(IRedisCacheService)), Is.Not.Null);
    }

    [Test]
    public void RedisCacheService_ShouldHaveConstructorWithConfigurationAndLogger()
    {
        var ctors = typeof(RedisCacheService).GetConstructors();
        Assert.That(ctors.Length, Is.EqualTo(1));
        var p = ctors[0].GetParameters();
        Assert.That(p.Length, Is.EqualTo(2));
        Assert.That(p[0].ParameterType.Name, Does.Contain("IConfiguration"));
        Assert.That(p[1].ParameterType.Name, Does.Contain("ILogger"));
    }
}