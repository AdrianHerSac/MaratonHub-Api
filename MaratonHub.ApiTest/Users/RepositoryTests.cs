using MaratonHub.Api.Users.Repositories;
using MaratonHub.Api.Users.Models;
using MongoDB.Driver;

namespace MaratonHub.ApiTest.Users;

public class UserRepositoryTests
{
    [Test]
    public void UserRepository_ShouldImplementIUserRepository()
    {
        Assert.That(typeof(UserRepository).GetInterface(nameof(IUserRepository)), Is.Not.Null);
    }

    [Test]
    public void UserRepository_ShouldHaveConstructorWithIMongoDatabase()
    {
        var ctors = typeof(UserRepository).GetConstructors();
        Assert.That(ctors.Length, Is.EqualTo(1));
        var p = ctors[0].GetParameters();
        Assert.That(p.Length, Is.EqualTo(1));
        Assert.That(p[0].ParameterType, Is.EqualTo(typeof(IMongoDatabase)));
    }
}

public class IUserRepositoryMethodSignatureTests
{
    [Test]
    public void GetUserByIdAsync_ShouldHaveCorrectSignature()
    {
        var m = typeof(IUserRepository).GetMethod("GetUserByIdAsync");
        Assert.That(m, Is.Not.Null);
        var p = m!.GetParameters();
        Assert.That(p.Length, Is.EqualTo(1));
        Assert.That(p[0].Name, Is.EqualTo("id"));
        Assert.That(p[0].ParameterType, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void GetUserByUsernameAsync_ShouldHaveCorrectSignature()
    {
        var m = typeof(IUserRepository).GetMethod("GetUserByUsernameAsync");
        var p = m!.GetParameters();
        Assert.That(p.Length, Is.EqualTo(1));
        Assert.That(p[0].Name, Is.EqualTo("username"));
        Assert.That(p[0].ParameterType, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void GetUserByGoogleIdAsync_ShouldHaveCorrectSignature()
    {
        var m = typeof(IUserRepository).GetMethod("GetUserByGoogleIdAsync");
        var p = m!.GetParameters();
        Assert.That(p.Length, Is.EqualTo(1));
        Assert.That(p[0].Name, Is.EqualTo("googleId"));
        Assert.That(p[0].ParameterType, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void CreateUserAsync_ShouldTakeUserAndReturnUser()
    {
        var m = typeof(IUserRepository).GetMethod("CreateUserAsync");
        var p = m!.GetParameters();
        Assert.That(p.Length, Is.EqualTo(1));
        Assert.That(p[0].ParameterType, Is.EqualTo(typeof(User)));
        Assert.That(m.ReturnType, Is.EqualTo(typeof(Task<User>)));
    }
}

public class UserModelBsonTests
{
    [Test]
    public void User_ShouldHaveBsonIdAttribute()
    {
        var prop = typeof(User).GetProperty("Id");
        var attrs = prop!.GetCustomAttributes(typeof(MongoDB.Bson.Serialization.Attributes.BsonIdAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
    }

    [Test]
    public void User_ShouldHaveBsonRepresentationAttribute()
    {
        var prop = typeof(User).GetProperty("Id");
        var attrs = prop!.GetCustomAttributes(typeof(MongoDB.Bson.Serialization.Attributes.BsonRepresentationAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
    }

    [Test]
    public void User_AllProperties_ShouldBeSettable()
    {
        var user = new User
        {
            Id = "test123", Username = "testuser",
            PasswordHash = "hashed123", GoogleId = "google123",
            CreatedAt = new DateTime(2024, 1, 1)
        };
        Assert.That(user.Id, Is.EqualTo("test123"));
        Assert.That(user.Username, Is.EqualTo("testuser"));
        Assert.That(user.GoogleId, Is.EqualTo("google123"));
        Assert.That(user.CreatedAt, Is.EqualTo(new DateTime(2024, 1, 1)));
    }

    [Test]
    public void User_GoogleId_ShouldBeNullable()
    {
        var user = new User { GoogleId = null };
        Assert.That(user.GoogleId, Is.Null);
    }
}