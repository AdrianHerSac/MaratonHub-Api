using MaratonHub.Api.Reviews.Dtos;
using MaratonHub.Api.Reviews.Models;

namespace MaratonHub.Api.Reviews.Reposytory;

public interface IReviewRepository
{
    Task<List<Review>> GetReviewsByMediaAsync(int mediaId, string mediaType);
    Task<List<Review>> GetReviewsByUserAsync(string userName);
    Task<Review?> GetReviewByIdAsync(string id);
    Task<Review> CreateReviewAsync(Review review);
    Task<Review?> UpdateReviewAsync(string id, Review review);
    Task<bool> DeleteReviewAsync(string id);
    Task<RatingAverageDto> GetAverageRatingAsync(int mediaId, string mediaType);
}
