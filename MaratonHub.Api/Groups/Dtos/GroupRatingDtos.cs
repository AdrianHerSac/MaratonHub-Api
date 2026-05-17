namespace MaratonHub.Api.Groups.Dtos;

public class CreateGroupRatingDto
{
    public int MediaId { get; set; }
    public string MediaType { get; set; } = string.Empty;
    public string MediaTitle { get; set; } = string.Empty;
    public string? PosterPath { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

public class GroupRatingDto
{
    public string Id { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int MediaId { get; set; }
    public string MediaType { get; set; } = string.Empty;
    public string MediaTitle { get; set; } = string.Empty;
    public string? PosterPath { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GroupMediaAverageDto
{
    public int MediaId { get; set; }
    public string MediaType { get; set; } = string.Empty;
    public string MediaTitle { get; set; } = string.Empty;
    public string? PosterPath { get; set; }
    public double AverageRating { get; set; }
    public int TotalRatings { get; set; }
}
