using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MaratonHub.Api.Reviews.Models;

public class Review
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public int MediaId { get; set; }
    
    public string MediaType { get; set; } = string.Empty; 
    
    public string UserName { get; set; } = string.Empty;
    
    public int Rating { get; set; } 
    
    public string Comment { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
