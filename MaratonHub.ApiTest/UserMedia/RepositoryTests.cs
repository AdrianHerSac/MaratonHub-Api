using MaratonHub.Api.UserMedia;
using MaratonHub.Api.UserMedia.Models;
using MongoDB.Driver;

namespace MaratonHub.ApiTest.UserMedia;

public class MediaRepositoryTests
{
    [Test]
    public void MediaRepository_ShouldImplementIMediaRepository()
    {
        Assert.That(typeof(MediaRepository).GetInterface(nameof(IMediaRepository)), Is.Not.Null);
    }

    [Test]
    public void MediaRepository_ShouldHaveConstructorWithIMongoDatabase()
    {
        var ctors = typeof(MediaRepository).GetConstructors();
        Assert.That(ctors.Length, Is.EqualTo(1));
        var p = ctors[0].GetParameters();
        Assert.That(p.Length, Is.EqualTo(1));
        Assert.That(p[0].ParameterType, Is.EqualTo(typeof(IMongoDatabase)));
    }
}

public class MediaServiceImplTests
{
    [Test]
    public void MediaService_ShouldImplementIMediaService()
    {
        Assert.That(typeof(MediaService).GetInterface(nameof(IMediaService)), Is.Not.Null);
    }

    [Test]
    public void MediaService_ShouldHaveConstructorWithIMediaRepository()
    {
        var ctors = typeof(MediaService).GetConstructors();
        Assert.That(ctors.Length, Is.EqualTo(1));
        var p = ctors[0].GetParameters();
        Assert.That(p.Length, Is.EqualTo(1));
        Assert.That(p[0].ParameterType.Name, Is.EqualTo("IMediaRepository"));
    }
}

public class MediaModelBsonTests
{
    [Test]
    public void Media_ExternalApiId_ShouldHaveBsonElementAttribute()
    {
        var prop = typeof(Media).GetProperty("ExternalApiId");
        var attrs = prop!.GetCustomAttributes(
            typeof(MongoDB.Bson.Serialization.Attributes.BsonElementAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
        var attr = (MongoDB.Bson.Serialization.Attributes.BsonElementAttribute)attrs[0];
        Assert.That(attr.ElementName, Is.EqualTo("tmdb_id"));
    }

    [Test]
    public void Media_SeriesInfo_ShouldHaveBsonIgnoreIfNullAttribute()
    {
        var prop = typeof(Media).GetProperty("SeriesInfo");
        var attrs = prop!.GetCustomAttributes(
            typeof(MongoDB.Bson.Serialization.Attributes.BsonIgnoreIfNullAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
    }

    [Test]
    public void Media_Id_ShouldHaveBsonRepresentationAttribute()
    {
        var prop = typeof(Media).GetProperty("Id");
        var attrs = prop!.GetCustomAttributes(
            typeof(MongoDB.Bson.Serialization.Attributes.BsonRepresentationAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
    }
}

public class IMediaRepositoryMethodSignatureTests
{
    [Test]
    public void GetAllAsync_ShouldReturnTaskOfListOfMedia()
    {
        var m = typeof(IMediaRepository).GetMethod("GetAllAsync");
        Assert.That(m!.ReturnType, Is.EqualTo(typeof(Task<List<Media>>)));
    }

    [Test]
    public void GetByIdAsync_ShouldTakeString()
    {
        var m = typeof(IMediaRepository).GetMethod("GetByIdAsync");
        var p = m!.GetParameters();
        Assert.That(p.Length, Is.EqualTo(1));
        Assert.That(p[0].ParameterType, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void GetByExternalApiIdAsync_ShouldTakeIntAndString()
    {
        var m = typeof(IMediaRepository).GetMethod("GetByExternalApiIdAsync");
        var p = m!.GetParameters();
        Assert.That(p.Length, Is.EqualTo(2));
        Assert.That(p[0].ParameterType, Is.EqualTo(typeof(int)));
        Assert.That(p[1].ParameterType, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void DeleteAsync_ShouldReturnBool()
    {
        var m = typeof(IMediaRepository).GetMethod("DeleteAsync");
        Assert.That(m!.ReturnType, Is.EqualTo(typeof(Task<bool>)));
    }

    [Test]
    public void ExistsAsync_ShouldReturnBool()
    {
        var m = typeof(IMediaRepository).GetMethod("ExistsAsync");
        Assert.That(m!.ReturnType, Is.EqualTo(typeof(Task<bool>)));
        var p = m.GetParameters();
        Assert.That(p.Length, Is.EqualTo(2));
    }
}