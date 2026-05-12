using MaratonHub.Api.Reviews.Dtos;
using MaratonHub.Api.Reviews.Models;
using MongoDB.Driver;

namespace MaratonHub.Api.Reviews.Reposytory;

public class ReviewRepository : IReviewRepository
{
    private readonly IMongoCollection<Review> _reviews;

    public ReviewRepository(IMongoDatabase database)
    {
        _reviews = database.GetCollection<Review>("reviews");
    }

    public async Task<List<Review>> GetReviewsByMediaAsync(int mediaId, string mediaType)
    {
        return await _reviews
            .Find(r => r.MediaId == mediaId && r.MediaType == mediaType)
            .SortByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Review>> GetReviewsByUserAsync(string userName)
    {
        return await _reviews
            .Find(r => r.UserName == userName)
            .SortByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Review?> GetReviewByIdAsync(string id)
    {
        return await _reviews.Find(r => r.Id == id).FirstOrDefaultAsync();
    }

    public async Task<Review> CreateReviewAsync(Review review)
    {
        review.CreatedAt = DateTime.UtcNow;
        await _reviews.InsertOneAsync(review);
        return review;
    }

    public async Task<Review?> UpdateReviewAsync(string id, Review review)
    {
        var result = await _reviews.ReplaceOneAsync(r => r.Id == id, review);
        return result.ModifiedCount > 0 ? review : null;
    }

    public async Task<bool> DeleteReviewAsync(string id)
    {
        var result = await _reviews.DeleteOneAsync(r => r.Id == id);
        return result.DeletedCount > 0;
    }

    public async Task<RatingAverageDto> GetAverageRatingAsync(int mediaId, string mediaType)
    {
        var reviews = await _reviews
            .Find(r => r.MediaId == mediaId && r.MediaType == mediaType)
            .ToListAsync();

        if (reviews.Count == 0)
            return new RatingAverageDto { Average = 0, Percentage = 0, TotalReviews = 0 };

        var average = reviews.Average(r => r.Rating);
        var percentage = (int)Math.Round(average / 5.0 * 100);

        return new RatingAverageDto
        {
            Average = Math.Round(average, 1),
            Percentage = percentage,
            TotalReviews = reviews.Count
        };
    }

    public async Task<long> FixUnknownReviewsAsync(string userId, string realUsername)
    {
        // Caso 1: Tiene el UserId correcto pero el nombre es "Unknown"
        var filter1 = Builders<Review>.Filter.And(
            Builders<Review>.Filter.Eq(r => r.UserId, userId),
            Builders<Review>.Filter.Eq(r => r.UserName, "Unknown")
        );

        // Caso 2: Es una review antigua (UserId vacío) y el nombre es "Unknown"
        // Este caso sirve para arreglar las reviews actuales de Adrian
        var filter2 = Builders<Review>.Filter.And(
            Builders<Review>.Filter.Or(
                Builders<Review>.Filter.Eq(r => r.UserId, ""),
                Builders<Review>.Filter.Exists(r => r.UserId, false)
            ),
            Builders<Review>.Filter.Eq(r => r.UserName, "Unknown")
        );

        var finalFilter = Builders<Review>.Filter.Or(filter1, filter2);

        var update = Builders<Review>.Update
            .Set(r => r.UserName, realUsername)
            .Set(r => r.UserId, userId); // También les ponemos el ID si no lo tenían

        var result = await _reviews.UpdateManyAsync(finalFilter, update);
        return result.ModifiedCount;
    }

    public async Task<long> CountReviewsAsync()
    {
        return await _reviews.CountDocumentsAsync(_ => true);
    }
}
