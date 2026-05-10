namespace MaratonHub.Api.TheMovieDB.Dtos;

public class CastDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Character { get; set; } = string.Empty;
    public string? ProfilePath { get; set; }
}
