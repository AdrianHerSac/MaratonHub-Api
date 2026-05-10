using MaratonHub.Api.Reviews.Models;
using MaratonHub.Api.Reviews.Dtos;
using MaratonHub.Api.Reviews.Reposytory;
using MongoDB.Driver;

namespace MaratonHub.ApiTest.Reviews;

public class ReviewRepositoryTests
{
    [Test]
    public void ReviewRepository_ShouldImplementIReviewRepository()
    {
        var type = typeof(ReviewRepository);
        Assert.That(type.GetInterface(nameof(IReviewRepository)), Is.Not.Null);
    }

    [Test]
    public void IReviewRepository_ShouldDefineAllMethods()
    {
        var type = typeof(IReviewRepository);

        Assert.That(type.GetMethod("GetReviewsByMediaAsync"), Is.Not.Null);
        Assert.That(type.GetMethod("GetReviewsByUserAsync"), Is.Not.Null);
        Assert.That(type.GetMethod("GetReviewByIdAsync"), Is.Not.Null);
        Assert.That(type.GetMethod("CreateReviewAsync"), Is.Not.Null);
        Assert.That(type.GetMethod("UpdateReviewAsync"), Is.Not.Null);
        Assert.That(type.GetMethod("DeleteReviewAsync"), Is.Not.Null);
        Assert.That(type.GetMethod("GetAverageRatingAsync"), Is.Not.Null);
        Assert.That(type.GetMethod("FixUnknownReviewsAsync"), Is.Not.Null);
    }

    [Test]
    public void ReviewRepository_ShouldHaveConstructorWithIMongoDatabase()
    {
        var constructors = typeof(ReviewRepository).GetConstructors();
        Assert.That(constructors.Length, Is.EqualTo(1));

        var constructor = constructors[0];
        var parameters = constructor.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(1));
        Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(IMongoDatabase)));
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// Method Signature Tests
// ══════════════════════════════════════════════════════════════════════════════

public class IReviewRepositoryMethodSignatureTests
{
    [Test]
    public void GetReviewsByMediaAsync_ShouldHaveCorrectSignature()
    {
        var method = typeof(IReviewRepository).GetMethod("GetReviewsByMediaAsync");
        Assert.That(method, Is.Not.Null);

        var parameters = method!.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(2));
        Assert.That(parameters[0].Name, Is.EqualTo("mediaId"));
        Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(int)));
        Assert.That(parameters[1].Name, Is.EqualTo("mediaType"));
        Assert.That(parameters[1].ParameterType, Is.EqualTo(typeof(string)));

        // Verifica tipo de retorno: Task<List<Review>>
        Assert.That(method.ReturnType.IsGenericType, Is.True);
        Assert.That(method.ReturnType.GetGenericTypeDefinition(), Is.EqualTo(typeof(Task<>)));
        var innerType = method.ReturnType.GenericTypeArguments[0];
        Assert.That(innerType.GetGenericTypeDefinition(), Is.EqualTo(typeof(List<>)));
        Assert.That(innerType.GenericTypeArguments[0], Is.EqualTo(typeof(Review)));
    }

    [Test]
    public void GetReviewsByUserAsync_ShouldHaveCorrectSignature()
    {
        var method = typeof(IReviewRepository).GetMethod("GetReviewsByUserAsync");
        Assert.That(method, Is.Not.Null);

        var parameters = method!.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(1));
        Assert.That(parameters[0].Name, Is.EqualTo("userName"));
        Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void GetReviewByIdAsync_ShouldReturnNullableReview()
    {
        var method = typeof(IReviewRepository).GetMethod("GetReviewByIdAsync");
        Assert.That(method, Is.Not.Null);

        var parameters = method!.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(1));
        Assert.That(parameters[0].Name, Is.EqualTo("id"));
        Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void CreateReviewAsync_ShouldReturnTaskOfReview()
    {
        var method = typeof(IReviewRepository).GetMethod("CreateReviewAsync");
        Assert.That(method, Is.Not.Null);

        var parameters = method!.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(1));
        Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(Review)));

        var returnType = method.ReturnType.GenericTypeArguments[0];
        Assert.That(returnType, Is.EqualTo(typeof(Review)));
    }

    [Test]
    public void UpdateReviewAsync_ShouldHaveIdAndReviewParams()
    {
        var method = typeof(IReviewRepository).GetMethod("UpdateReviewAsync");
        Assert.That(method, Is.Not.Null);

        var parameters = method!.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(2));
        Assert.That(parameters[0].Name, Is.EqualTo("id"));
        Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(string)));
        Assert.That(parameters[1].Name, Is.EqualTo("review"));
        Assert.That(parameters[1].ParameterType, Is.EqualTo(typeof(Review)));
    }

    [Test]
    public void DeleteReviewAsync_ShouldReturnTaskOfBool()
    {
        var method = typeof(IReviewRepository).GetMethod("DeleteReviewAsync");
        Assert.That(method, Is.Not.Null);

        var parameters = method!.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(1));
        Assert.That(parameters[0].Name, Is.EqualTo("id"));
        Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(string)));

        var returnType = method.ReturnType.GenericTypeArguments[0];
        Assert.That(returnType, Is.EqualTo(typeof(bool)));
    }

    [Test]
    public void GetAverageRatingAsync_ShouldReturnTaskOfRatingAverageDto()
    {
        var method = typeof(IReviewRepository).GetMethod("GetAverageRatingAsync");
        Assert.That(method, Is.Not.Null);

        var parameters = method!.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(2));
        Assert.That(parameters[0].Name, Is.EqualTo("mediaId"));
        Assert.That(parameters[1].Name, Is.EqualTo("mediaType"));

        var returnType = method.ReturnType.GenericTypeArguments[0];
        Assert.That(returnType, Is.EqualTo(typeof(RatingAverageDto)));
    }

    [Test]
    public void FixUnknownReviewsAsync_ShouldReturnTaskOfLong()
    {
        var method = typeof(IReviewRepository).GetMethod("FixUnknownReviewsAsync");
        Assert.That(method, Is.Not.Null);

        var parameters = method!.GetParameters();
        Assert.That(parameters.Length, Is.EqualTo(2));
        Assert.That(parameters[0].Name, Is.EqualTo("userId"));
        Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(string)));
        Assert.That(parameters[1].Name, Is.EqualTo("realUsername"));
        Assert.That(parameters[1].ParameterType, Is.EqualTo(typeof(string)));

        var returnType = method.ReturnType.GenericTypeArguments[0];
        Assert.That(returnType, Is.EqualTo(typeof(long)));
    }
}