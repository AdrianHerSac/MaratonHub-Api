using MaratonHub.Api.UserMedia;
using Microsoft.AspNetCore.Mvc;
using MediaModel = MaratonHub.Api.UserMedia.Models.Media;

namespace MaratonHub.Api.UserMedia.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MediaController : ControllerBase
{
    private readonly IMediaService _mediaService;

    public MediaController(IMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    // GET: api/media
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var media = await _mediaService.GetAllAsync();
        return Ok(media);
    }

    // GET: api/media/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var media = await _mediaService.GetByIdAsync(id);
        if (media == null) return NotFound();
        return Ok(media);
    }

    // GET: api/media/type/{mediaType}
    [HttpGet("type/{mediaType}")]
    public async Task<IActionResult> GetByMediaType(string mediaType)
    {
        var media = await _mediaService.GetByMediaTypeAsync(mediaType);
        return Ok(media);
    }

    // GET: api/media/tmdb/{tmdbId}/{mediaType}
    [HttpGet("tmdb/{tmdbId}/{mediaType}")]
    public async Task<IActionResult> GetByTmdbId(int tmdbId, string mediaType)
    {
        var media = await _mediaService.GetByTmdbIdAsync(tmdbId, mediaType);
        if (media == null) return NotFound();
        return Ok(media);
    }

    // GET: api/media/search?title=...
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return BadRequest("El parámetro 'title' es requerido.");
        var media = await _mediaService.SearchByTitleAsync(title);
        return Ok(media);
    }

    // POST: api/media
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MediaModel media)
    {
        var created = await _mediaService.CreateAsync(media);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT: api/media/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] MediaModel media)
    {
        var updated = await _mediaService.UpdateAsync(id, media);
        if (updated == null) return NotFound();
        return Ok(updated);
    }

    // DELETE: api/media/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _mediaService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
