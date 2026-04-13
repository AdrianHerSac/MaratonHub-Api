using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaratonHub.Api.UserMedia.Models;

public class FilmDetail
{
    [Key, ForeignKey("Media")]
    public int MediaId { get; set; }
    public Media Media { get; set; } = null!;
    
    public int RuntimeMinutes { get; set; }
    public string Tagline { get; set; } = string.Empty;
    public string Overview { get; set; } = string.Empty;
    public string ReleaseDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public int Budget { get; set; }
    public int Revenue { get; set; }
    public int Popularity { get; set; }
    public int VoteAverage { get; set; }
}
