namespace MaratonHub.Api.UserMedia.Models;

public class SeriesDetail
{
    public int TotalSeasons { get; set; }
    public int TotalEpisodes { get; set; }
    public int EpisodeRuntimeMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Overview { get; set; } = string.Empty;
    public string FirstAirDate { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public int? NextEpisodeToAir { get; set; }
    public int Popularity { get; set; }
    
    public int TimeToken { get; set; }
}
