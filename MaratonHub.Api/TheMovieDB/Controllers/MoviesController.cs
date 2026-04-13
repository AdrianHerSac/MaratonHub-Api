using MaratonHub.Api.TheMovieDB.Services;
using Microsoft.AspNetCore.Mvc;

namespace MaratonHub.Api.TheMovieDB.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoviesController : ControllerBase
{
    private readonly ITheMovieDBService _tmdbService;

    public MoviesController(ITheMovieDBService tmdbService)
    {
        _tmdbService = tmdbService;
    }

    [HttpGet("trending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTrending()
    {
        var movies = await _tmdbService.GetTrendingMoviesAsync();
        return Ok(movies);
    }

    [HttpGet("popular")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPopular()
    {
        var movies = await _tmdbService.GetPopularMoviesAsync();
        return Ok(movies);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Query parameter is required");

        var movies = await _tmdbService.SearchMoviesAsync(query);
        return Ok(movies);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetails(int id)
    {
        var movie = await _tmdbService.GetMovieDetailsAsync(id);
        if (movie == null)
            return NotFound();

        return Ok(movie);
    }
    
    [HttpGet("terror")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHorrorMovies()
        => Ok(await _tmdbService.GetMoviesByGenreAsync(27));

    [HttpGet("comedia")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetComedyMovies()
        => Ok(await _tmdbService.GetMoviesByGenreAsync(35));

    [HttpGet("accion")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActionMovies()
        => Ok(await _tmdbService.GetMoviesByGenreAsync(28));

    [HttpGet("animacion")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnimationMovies()
        => Ok(await _tmdbService.GetMoviesByGenreAsync(16));

    [HttpGet("ciencia-ficcion")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSciFiMovies()
        => Ok(await _tmdbService.GetMoviesByGenreAsync(878));

    [HttpGet("genero/{genreId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByGenre(int genreId)
        => Ok(await _tmdbService.GetMoviesByGenreAsync(genreId));

}
