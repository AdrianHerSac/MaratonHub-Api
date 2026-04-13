namespace MaratonHub.Api.Reviews.Dtos;

public class ReviewDto
{
    public string? Id { get; set; }
    public int MediaId { get; set; }
    public string MediaType { get; set; } = string.Empty; // "Movie", "TvShow", "Person"
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; } // 1-5
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateReviewDto
{
    public int MediaId { get; set; }
    public string MediaType { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}

public class RatingAverageDto
{
    public double Average { get; set; }    // 0.0 – 5.0
    public int Percentage { get; set; }    // 0 – 100
    public int TotalReviews { get; set; }
}
