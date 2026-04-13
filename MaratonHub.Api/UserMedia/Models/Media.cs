using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MaratonHub.Api.UserMedia.Models;

public class Media
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; } 

    [BsonElement("tmdb_id")]
    public int ExternalApiId { get; set; } 

    public string Title { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty; 
    public string PosterPath { get; set; } = string.Empty;

    [BsonIgnoreIfNull]
    public SeriesDetail? SeriesInfo { get; set; }
}
