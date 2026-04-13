namespace MaratonHub.Api.TheMovieDB.Dtos;

public class PersonDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ProfilePath { get; set; }
    public double Popularity { get; set; }
    public string? KnownForDepartment { get; set; }
    public string? Biography { get; set; }
    public DateTime? Birthday { get; set; }
    public string? PlaceOfBirth { get; set; }
}
