using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MaratonHub.Api.Groups.Models;

public class Group
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string CreatedById { get; set; } = string.Empty;

    public string CreatedByName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? InviteCode { get; set; }

    public List<GroupMember> Members { get; set; } = new();
}

public class GroupMember
{
    public string UserId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string Role { get; set; } = "Member";

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
