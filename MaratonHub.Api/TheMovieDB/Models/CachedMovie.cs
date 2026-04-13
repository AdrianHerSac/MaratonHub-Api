using MaratonHub.Api.TheMovieDB.Dtos;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MaratonHub.Api.TheMovieDB.Models;

public class CachedMovie
{
    [BsonId]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    /// <summary>Identificador de la lista: "trending_movies", "popular_movies"</summary>
    public string CacheKey { get; set; } = string.Empty;

    public DateTime CachedAt { get; set; } = DateTime.UtcNow;

    public List<MovieDto> Movies { get; set; } = new();
}
