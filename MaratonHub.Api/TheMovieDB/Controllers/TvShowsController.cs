using MaratonHub.Api.TheMovieDB.Services;
using Microsoft.AspNetCore.Mvc;

namespace MaratonHub.Api.TheMovieDB.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TvShowsController : ControllerBase
{
    private readonly ITheMovieDBService _tmdbService;

    public TvShowsController(ITheMovieDBService tmdbService)
    {
        _tmdbService = tmdbService;
    }

    [HttpGet("trending")]
    public async Task<IActionResult> GetTrending()
    {
        var shows = await _tmdbService.GetTrendingTvShowsAsync();
        return Ok(shows);
    }

    [HttpGet("popular")]
    public async Task<IActionResult> GetPopular()
    {
        var shows = await _tmdbService.GetPopularTvShowsAsync();
        return Ok(shows);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Query parameter is required");

        var shows = await _tmdbService.SearchTvShowsAsync(query);
        return Ok(shows);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetails(int id)
    {
        var show = await _tmdbService.GetTvShowDetailsAsync(id);
        if (show == null)
            return NotFound();

        return Ok(show);
    }
}
