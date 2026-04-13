namespace MaratonHub.Api.TheMovieDB.Dtos;

public class MovieDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Overview { get; set; } = string.Empty;
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public double VoteAverage { get; set; }
    public int VoteCount { get; set; }
    public string? OriginalLanguage { get; set; }
    public List<GenreDto> Genres { get; set; } = new();
}

