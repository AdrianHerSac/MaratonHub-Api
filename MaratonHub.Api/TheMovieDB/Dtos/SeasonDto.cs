namespace MaratonHub.Api.TheMovieDB.Dtos;

public class SeasonDto
{
    public int Id { get; set; }
    public int SeasonNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Overview { get; set; } = string.Empty;
    public string? PosterPath { get; set; }
    public int EpisodeCount { get; set; }
    public DateTime? AirDate { get; set; }
    
    // Loaded dynamically when asking for a specific season
    public List<EpisodeDto>? Episodes { get; set; }
}
