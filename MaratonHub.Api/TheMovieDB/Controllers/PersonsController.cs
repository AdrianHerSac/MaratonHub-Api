using MaratonHub.Api.TheMovieDB.Services;
using Microsoft.AspNetCore.Mvc;

namespace MaratonHub.Api.TheMovieDB.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonsController : ControllerBase
{
    private readonly ITheMovieDBService _tmdbService;

    public PersonsController(ITheMovieDBService tmdbService)
    {
        _tmdbService = tmdbService;
    }

    [HttpGet("popular")]
    public async Task<IActionResult> GetPopular()
    {
        var persons = await _tmdbService.GetPopularPersonsAsync();
        return Ok(persons);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Query parameter is required");

        var persons = await _tmdbService.SearchPersonsAsync(query);
        return Ok(persons);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetails(int id)
    {
        var person = await _tmdbService.GetPersonDetailsAsync(id);
        if (person == null)
            return NotFound();

        return Ok(person);
    }
}
