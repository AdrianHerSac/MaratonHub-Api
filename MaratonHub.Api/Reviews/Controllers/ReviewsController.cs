using MaratonHub.Api.Reviews;
using MaratonHub.Api.Reviews.Dtos;
using MaratonHub.Api.Reviews.Models;
using MaratonHub.Api.Reviews.Reposytory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace MaratonHub.Api.Reviews.Controllers;

/// <summary>
/// Controlador de reviews
/// </summary>
/// <response code="200">Reviews obtenidas exitosamente</response>
/// <response code="404">Media no encontrada</response>
/// <response code="401">Usuario no autorizado</response>
/// <response code="400">Rating inválido o error en la petición</response>
[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewRepository _reviewRepository;

    public ReviewsController(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    // ENDPOINT TEMPORAL DE DEBUG - ver los claims que llegan en el JWT
    [HttpGet("debug-claims")]
    [Authorize]
    public IActionResult DebugClaims()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        var identity = User.Identity?.Name;
        return Ok(new { claims, identityName = identity });
    }

    // <sumary> GET /api/reviews/top-rated/{mediaType}
    // Obtiene las medias mejor valoradas de un tipo
    // </sumary>
    // <response code="200">Top rated obtenido exitosamente</response>
    [HttpGet("top-rated/{mediaType}")]
    public async Task<IActionResult> GetTopRated(string mediaType, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        string dbMediaType = mediaType.ToLower() switch
        {
            "movie" => "Movie",
            "tv" => "TvShow",
            "person" => "Person",
            _ => mediaType
        };

        var result = await _reviewRepository.GetTopRatedMediaAsync(dbMediaType, page, pageSize);

        foreach (var item in result)
        {
            item.MediaType = mediaType;
        }

        return Ok(result);
    }

    // <sumary> GET /api/reviews/average/{mediaType}/{mediaId}
    // Obtiene el promedio de las reviews de una media
    // </sumary>
    // <response code="200">Promedio obtenido exitosamente</response>
    // <response code="404">Media no encontrada</response>
    [HttpGet("average/{mediaType}/{mediaId}")]
    public async Task<IActionResult> GetAverageRating(string mediaType, int mediaId)
    {
        var result = await _reviewRepository.GetAverageRatingAsync(mediaId, mediaType);
        return Ok(result);
    }

    // <sumary> GET /api/reviews/{mediaType}/{mediaId}
    // Obtiene las reviews de una media
    // </sumary>
    // <response code="200">Reviews obtenidas exitosamente</response>
    // <response code="404">Media no encontrada</response>
    [HttpGet("{mediaType}/{mediaId}")]
    public async Task<IActionResult> GetReviewsByMedia(string mediaType, int mediaId)
    {
        var reviews = await _reviewRepository.GetReviewsByMediaAsync(mediaId, mediaType);
        var reviewDtos = reviews.Select(r => new ReviewDto
        {
            Id = r.Id,
            UserId = r.UserId,
            MediaId = r.MediaId,
            MediaType = r.MediaType,
            UserName = r.UserName,
            Rating = r.Rating,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt
        }).ToList();

        return Ok(reviewDtos);
    }

    // <sumary> GET /api/reviews/user/{userName}
    // Obtiene las reviews de un usuario
    // </sumary>
    // <response code="200">Reviews obtenidas exitosamente</response>
    // <response code="404">Usuario no encontrado</response>
    [HttpGet("user/{userName}")]
    public async Task<IActionResult> GetReviewsByUser(string userName)
    {
        var reviews = await _reviewRepository.GetReviewsByUserAsync(userName);
        var reviewDtos = reviews.Select(r => new ReviewDto
        {
            Id = r.Id,
            UserId = r.UserId,
            MediaId = r.MediaId,
            MediaType = r.MediaType,
            UserName = r.UserName,
            Rating = r.Rating,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt
        }).ToList();

        return Ok(reviewDtos);
    }

    // <sumary> POST /api/reviews
    // Crea una review nueva
    // </sumary>
    // <response code="201">Review creada exitosamente</response>
    // <response code="400">Rating inválido o error en la petición</response>
    // <response code="401">Usuario no autorizado</response>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto dto)
    {
        if (dto.Rating < 1 || dto.Rating > 5)
            return BadRequest("Rating must be between 1 and 5");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? User.FindFirstValue("sub") 
            ?? "UnknownID";

        var username = User.FindFirstValue("unique_name") 
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue("name")
            ?? User.Identity?.Name 
            ?? "Unknown";

        var review = new Review
        {
            UserId = userId,
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
            UserId = created.UserId,
            MediaId = created.MediaId,
            MediaType = created.MediaType,
            UserName = created.UserName,
            Rating = created.Rating,
            Comment = created.Comment,
            CreatedAt = created.CreatedAt
        };

        return CreatedAtAction(nameof(GetReviewsByMedia), new { mediaType = created.MediaType, mediaId = created.MediaId }, reviewDto);
    }

    // <sumary> PUT /api/reviews/{id}
    // Actualiza una review
    // </sumary>
    // <response code="204">Review actualizada exitosamente</response>
    // <response code="400">Rating inválido o error en la petición</response>
    // <response code="401">Usuario no autorizado</response>
    // <response code="404">Review no encontrada</response>
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

    // <sumary> DELETE /api/reviews/{id}
    // Elimina una review
    // </sumary>
    // <response code="204">Review eliminada exitosamente</response>
    // <response code="401">Usuario no autorizado</response>
    // <response code="404">Review no encontrada</response>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteReview(string id)
    {
        var deleted = await _reviewRepository.DeleteReviewAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    // <sumary> POST /api/reviews/fix-unknown
    // Repara las reviews que tienen el nombre "Unknown"
    // </sumary>
    // <response code="200">Reviews reparadas exitosamente</response>
    // <response code="400">No se pudo determinar el usuario</response>
    // <response code="401">Usuario no autorizado</response>
    [HttpPost("fix-unknown")]
    [Authorize]
    public async Task<IActionResult> FixUnknownReviews()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var username = User.FindFirstValue("unique_name") 
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue("name")
            ?? User.Identity?.Name;

        if (string.IsNullOrEmpty(username) || username == "Unknown" || string.IsNullOrEmpty(userId))
            return BadRequest("No se pudo determinar el usuario.");

        // Intentamos reparar primero por UserId (si ya existe alguno pero tiene nombre Unknown)
        // Y por ahora, permitimos reparar los "Unknown" sin ID para limpiar la base de datos de Adrian
        var updated = await _reviewRepository.FixUnknownReviewsAsync(userId, username);
        return Ok(new { message = $"Se actualizaron {updated} reviews a '{username}'" });
    }
}
