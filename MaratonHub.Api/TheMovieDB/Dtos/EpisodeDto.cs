namespace MaratonHub.Api.TheMovieDB.Dtos;

public class EpisodeDto
{
    public int Id { get; set; }
    public int EpisodeNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Overview { get; set; } = string.Empty;
    public string? StillPath { get; set; }
    public DateTime? AirDate { get; set; }
    public double VoteAverage { get; set; }
}
