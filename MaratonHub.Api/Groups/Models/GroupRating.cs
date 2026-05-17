using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MaratonHub.Api.Groups.Models;

public class GroupRating
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string GroupId { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public int MediaId { get; set; }

    public string MediaType { get; set; } = string.Empty;

    public string MediaTitle { get; set; } = string.Empty;

    public string? PosterPath { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
