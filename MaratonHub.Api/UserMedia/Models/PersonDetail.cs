using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaratonHub.Api.UserMedia.Models;

public class PersonDetail
{
    [Key, ForeignKey("Media")]
    public int MediaId { get; set; }
    public Media Media { get; set; } = null!;
    
    public string Biography { get; set; } = string.Empty;
    public string PlaceOfBirth { get; set; } = string.Empty;
    public string ProfilePath { get; set; } = string.Empty;
    public string KnownForDepartment { get; set; } = string.Empty;
    
    public int Popularity { get; set; }
}
