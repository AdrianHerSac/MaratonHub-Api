using MaratonHub.Api.Reviews;
using MaratonHub.Api.Reviews.Dtos;
using MaratonHub.Api.Reviews.Models;
using MaratonHub.Api.Reviews.Reposytory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace MaratonHub.Api.Reviews.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewRepository _reviewRepository;

    public ReviewsController(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    [HttpGet("average/{mediaType}/{mediaId}")]
    public async Task<IActionResult> GetAverageRating(string mediaType, int mediaId)
    {
        var result = await _reviewRepository.GetAverageRatingAsync(mediaId, mediaType);
        return Ok(result);
    }

    [HttpGet("{mediaType}/{mediaId}")]
    public async Task<IActionResult> GetReviewsByMedia(string mediaType, int mediaId)
    {
        var reviews = await _reviewRepository.GetReviewsByMediaAsync(mediaId, mediaType);
        var reviewDtos = reviews.Select(r => new ReviewDto
        {
            Id = r.Id,
            MediaId = r.MediaId,
            MediaType = r.MediaType,
            UserName = r.UserName,
            Rating = r.Rating,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt
        }).ToList();

        return Ok(reviewDtos);
    }

    [HttpGet("user/{userName}")]
    public async Task<IActionResult> GetReviewsByUser(string userName)
    {
        var reviews = await _reviewRepository.GetReviewsByUserAsync(userName);
        var reviewDtos = reviews.Select(r => new ReviewDto
        {
            Id = r.Id,
            MediaId = r.MediaId,
            MediaType = r.MediaType,
            UserName = r.UserName,
            Rating = r.Rating,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt
        }).ToList();

        return Ok(reviewDtos);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto dto)
    {
        if (dto.Rating < 1 || dto.Rating > 5)
            return BadRequest("Rating must be between 1 and 5");

        var username = User.Claims.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.UniqueName)?.Value 
            ?? "Unknown";

        var review = new Review
        {
            MediaId = dto.MediaId,
            MediaType = dto.MediaType,
            UserName = username,
            Rating = dto.Rating,
            Comment = dto.Comment
        };

        var created = await _reviewRepository.CreateReviewAsync(review);

        var reviewDto = new ReviewDto
        {
            Id = created.Id,
            MediaId = created.MediaId,
            MediaType = created.MediaType,
            UserName = created.UserName,
            Rating = created.Rating,
            Comment = created.Comment,
            CreatedAt = created.CreatedAt
        };

        return CreatedAtAction(nameof(GetReviewsByMedia), new { mediaType = created.MediaType, mediaId = created.MediaId }, reviewDto);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateReview(string id, [FromBody] CreateReviewDto dto)
    {
        if (dto.Rating < 1 || dto.Rating > 5)
            return BadRequest("Rating must be between 1 and 5");

        var existing = await _reviewRepository.GetReviewByIdAsync(id);
        if (existing == null)
            return NotFound();

        existing.Rating = dto.Rating;
        existing.Comment = dto.Comment;

        var updated = await _reviewRepository.UpdateReviewAsync(id, existing);
        if (updated == null)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteReview(string id)
    {
        var deleted = await _reviewRepository.DeleteReviewAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
