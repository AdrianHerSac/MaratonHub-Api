namespace MaratonHub.Api.Reviews.Dtos;

public class TopRatedAppMediaDto
{
    public int MediaId { get; set; }
    public string MediaType { get; set; } = string.Empty;
    public double Average { get; set; }
    public int Percentage { get; set; }
    public int TotalReviews { get; set; }
}
