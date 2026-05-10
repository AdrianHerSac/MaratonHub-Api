namespace MaratonHub.Api.TheMovieDB.Dtos;

public class VideoDto
{
    public string Id { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty; // YouTube video key
    public string Name { get; set; } = string.Empty;
    public string Site { get; set; } = string.Empty; // e.g., "YouTube"
    public string Type { get; set; } = string.Empty; // e.g., "Trailer"
}
