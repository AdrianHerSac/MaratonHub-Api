namespace MaratonHub.Api.TheMovieDB.Dtos;

public class TvShowDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Overview { get; set; } = string.Empty;
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public DateTime? FirstAirDate { get; set; }
    public double VoteAverage { get; set; }
    public int VoteCount { get; set; }
    public string? OriginalLanguage { get; set; }
    public List<GenreDto> Genres { get; set; } = new();

    // Campos específicos de series (solo disponibles en el endpoint de detalles)
    public int? NumberOfSeasons { get; set; }
    public int? NumberOfEpisodes { get; set; }
    public string? Status { get; set; }
}
