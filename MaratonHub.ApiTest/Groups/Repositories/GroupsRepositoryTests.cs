using MaratonHub.Api.Groups.Models;
using MaratonHub.Api.Groups.Repositories;
using MaratonHub.Api.Groups.Dtos;
using MongoDB.Driver;

namespace MaratonHub.ApiTest.Groups.Repositories;

// ══════════════════════════════════════════════════════════════════════════════
// Group Model Tests
// ══════════════════════════════════════════════════════════════════════════════

public class GroupModelTests
{
    [Test]
    public void Group_ShouldInitializeWithDefaultValues()
    {
        var g = new Group();
        Assert.That(g.Id, Is.Null);
        Assert.That(g.Name, Is.EqualTo(string.Empty));
        Assert.That(g.Description, Is.Null);
        Assert.That(g.CreatedById, Is.EqualTo(string.Empty));
        Assert.That(g.CreatedByName, Is.EqualTo(string.Empty));
        Assert.That(g.InviteCode, Is.Null);
        Assert.That(g.Members, Is.Not.Null);
        Assert.That(g.Members.Count, Is.EqualTo(0));
    }

    [Test]
    public void Group_ShouldAllowSettingProperties()
    {
        var g = new Group
        {
            Id = "g1",
            Name = "My Group",
            Description = "Desc",
            CreatedById = "user1",
            CreatedByName = "UserOne",
            InviteCode = "ABCD1234",
            Members = new List<GroupMember> { new GroupMember { UserId = "u1", Role = "Admin" } }
        };
        Assert.That(g.Id, Is.EqualTo("g1"));
        Assert.That(g.Name, Is.EqualTo("My Group"));
        Assert.That(g.InviteCode, Is.EqualTo("ABCD1234"));
        Assert.That(g.Members.Count, Is.EqualTo(1));
    }

    [Test]
    public void Group_ShouldHaveBsonIdAttribute()
    {
        var prop = typeof(Group).GetProperty("Id");
        var attrs = prop!.GetCustomAttributes(typeof(MongoDB.Bson.Serialization.Attributes.BsonIdAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
    }

    [Test]
    public void Group_CreatedAt_ShouldDefaultToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var g = new Group();
        var after = DateTime.UtcNow.AddSeconds(1);
        Assert.That(g.CreatedAt, Is.InRange(before, after));
    }
}

public class GroupMemberModelTests
{
    [Test]
    public void GroupMember_ShouldInitializeWithDefaultValues()
    {
        var m = new GroupMember();
        Assert.That(m.UserId, Is.EqualTo(string.Empty));
        Assert.That(m.UserName, Is.EqualTo(string.Empty));
        Assert.That(m.Role, Is.EqualTo("Member"));
    }

    [Test]
    public void GroupMember_ShouldAllowSettingProperties()
    {
        var m = new GroupMember { UserId = "u1", UserName = "User1", Role = "Admin" };
        Assert.That(m.UserId, Is.EqualTo("u1"));
        Assert.That(m.UserName, Is.EqualTo("User1"));
        Assert.That(m.Role, Is.EqualTo("Admin"));
    }

    [Test]
    public void GroupMember_JoinedAt_ShouldDefaultToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var m = new GroupMember();
        var after = DateTime.UtcNow.AddSeconds(1);
        Assert.That(m.JoinedAt, Is.InRange(before, after));
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// GroupRating Model Tests
// ══════════════════════════════════════════════════════════════════════════════

public class GroupRatingModelTests
{
    [Test]
    public void GroupRating_ShouldInitializeWithDefaultValues()
    {
        var r = new GroupRating();
        Assert.That(r.Id, Is.Null);
        Assert.That(r.GroupId, Is.EqualTo(string.Empty));
        Assert.That(r.UserId, Is.EqualTo(string.Empty));
        Assert.That(r.UserName, Is.EqualTo(string.Empty));
        Assert.That(r.MediaId, Is.EqualTo(0));
        Assert.That(r.MediaType, Is.EqualTo(string.Empty));
        Assert.That(r.MediaTitle, Is.EqualTo(string.Empty));
        Assert.That(r.PosterPath, Is.Null);
        Assert.That(r.Rating, Is.EqualTo(0));
        Assert.That(r.Comment, Is.Null);
    }

    [Test]
    public void GroupRating_ShouldAllowSettingProperties()
    {
        var r = new GroupRating
        {
            Id = "r1",
            GroupId = "g1",
            UserId = "u1",
            UserName = "User1",
            MediaId = 42,
            MediaType = "Movie",
            MediaTitle = "Test Movie",
            PosterPath = "/poster.jpg",
            Rating = 4,
            Comment = "Good!"
        };
        Assert.That(r.GroupId, Is.EqualTo("g1"));
        Assert.That(r.MediaId, Is.EqualTo(42));
        Assert.That(r.Rating, Is.EqualTo(4));
    }

    [Test]
    public void GroupRating_ShouldHaveBsonIdAttribute()
    {
        var prop = typeof(GroupRating).GetProperty("Id");
        var attrs = prop!.GetCustomAttributes(typeof(MongoDB.Bson.Serialization.Attributes.BsonIdAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
    }

    [Test]
    public void GroupRating_CreatedAt_ShouldDefaultToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var r = new GroupRating();
        var after = DateTime.UtcNow.AddSeconds(1);
        Assert.That(r.CreatedAt, Is.InRange(before, after));
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// ChatMessage Model Tests
// ══════════════════════════════════════════════════════════════════════════════

public class ChatMessageModelTests
{
    [Test]
    public void ChatMessage_ShouldInitializeWithDefaultValues()
    {
        var m = new ChatMessage();
        Assert.That(m.Id, Is.Null);
        Assert.That(m.GroupId, Is.EqualTo(string.Empty));
        Assert.That(m.UserId, Is.EqualTo(string.Empty));
        Assert.That(m.UserName, Is.EqualTo(string.Empty));
        Assert.That(m.Message, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ChatMessage_ShouldAllowSettingProperties()
    {
        var m = new ChatMessage
        {
            Id = "m1",
            GroupId = "g1",
            UserId = "u1",
            UserName = "User1",
            Message = "Hello!",
        };
        Assert.That(m.GroupId, Is.EqualTo("g1"));
        Assert.That(m.Message, Is.EqualTo("Hello!"));
    }

    [Test]
    public void ChatMessage_SentAt_ShouldDefaultToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var m = new ChatMessage();
        var after = DateTime.UtcNow.AddSeconds(1);
        Assert.That(m.SentAt, Is.InRange(before, after));
    }

    [Test]
    public void ChatMessage_ShouldHaveBsonIdAttribute()
    {
        var prop = typeof(ChatMessage).GetProperty("Id");
        var attrs = prop!.GetCustomAttributes(typeof(MongoDB.Bson.Serialization.Attributes.BsonIdAttribute), false);
        Assert.That(attrs.Length, Is.EqualTo(1));
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// DTO Tests
// ══════════════════════════════════════════════════════════════════════════════

public class GroupDtoTests
{
    [Test]
    public void GroupDto_ShouldInitializeWithDefaultValues()
    {
        var dto = new GroupDto();
        Assert.That(dto.Id, Is.EqualTo(string.Empty));
        Assert.That(dto.Name, Is.EqualTo(string.Empty));
        Assert.That(dto.Description, Is.Null);
        Assert.That(dto.CreatedById, Is.EqualTo(string.Empty));
        Assert.That(dto.InviteCode, Is.Null);
        Assert.That(dto.Members, Is.Not.Null);
    }

    [Test]
    public void GroupDto_MemberCount_ShouldReflectMembersCount()
    {
        var dto = new GroupDto();
        dto.Members.Add(new GroupMemberDto { UserId = "u1" });
        dto.Members.Add(new GroupMemberDto { UserId = "u2" });
        Assert.That(dto.MemberCount, Is.EqualTo(2));
    }

    [Test]
    public void CreateGroupDto_ShouldInitializeWithDefaultValues()
    {
        var dto = new CreateGroupDto();
        Assert.That(dto.Name, Is.EqualTo(string.Empty));
        Assert.That(dto.Description, Is.Null);
    }

    [Test]
    public void UpdateGroupDto_ShouldAllowNullValues()
    {
        var dto = new UpdateGroupDto { Name = null, Description = null };
        Assert.That(dto.Name, Is.Null);
        Assert.That(dto.Description, Is.Null);
    }

    [Test]
    public void JoinGroupDto_ShouldAllowNullInviteCode()
    {
        var dto = new JoinGroupDto { InviteCode = null };
        Assert.That(dto.InviteCode, Is.Null);
    }

    [Test]
    public void GroupSummaryDto_ShouldInitializeWithDefaultValues()
    {
        var dto = new GroupSummaryDto();
        Assert.That(dto.Id, Is.EqualTo(string.Empty));
        Assert.That(dto.Name, Is.EqualTo(string.Empty));
        Assert.That(dto.Description, Is.Null);
        Assert.That(dto.CreatedByName, Is.EqualTo(string.Empty));
        Assert.That(dto.MemberCount, Is.EqualTo(0));
    }

    [Test]
    public void GroupMemberDto_ShouldDefaultToMemberRole()
    {
        var dto = new GroupMemberDto();
        Assert.That(dto.Role, Is.EqualTo("Member"));
    }
}

public class GroupRatingDtoTests
{
    [Test]
    public void CreateGroupRatingDto_ShouldInitializeWithDefaultValues()
    {
        var dto = new CreateGroupRatingDto();
        Assert.That(dto.MediaId, Is.EqualTo(0));
        Assert.That(dto.MediaType, Is.EqualTo(string.Empty));
        Assert.That(dto.MediaTitle, Is.EqualTo(string.Empty));
        Assert.That(dto.PosterPath, Is.Null);
        Assert.That(dto.Rating, Is.EqualTo(0));
        Assert.That(dto.Comment, Is.Null);
    }

    [Test]
    public void GroupRatingDto_ShouldInitializeWithDefaultValues()
    {
        var dto = new GroupRatingDto();
        Assert.That(dto.Id, Is.EqualTo(string.Empty));
        Assert.That(dto.GroupId, Is.EqualTo(string.Empty));
        Assert.That(dto.UserId, Is.EqualTo(string.Empty));
        Assert.That(dto.UserName, Is.EqualTo(string.Empty));
        Assert.That(dto.MediaId, Is.EqualTo(0));
        Assert.That(dto.MediaType, Is.EqualTo(string.Empty));
        Assert.That(dto.MediaTitle, Is.EqualTo(string.Empty));
        Assert.That(dto.PosterPath, Is.Null);
        Assert.That(dto.Rating, Is.EqualTo(0));
        Assert.That(dto.Comment, Is.Null);
    }

    [Test]
    public void GroupMediaAverageDto_ShouldInitializeWithDefaultValues()
    {
        var dto = new MaratonHub.Api.Groups.Dtos.GroupMediaAverageDto();
        Assert.That(dto.MediaId, Is.EqualTo(0));
        Assert.That(dto.MediaType, Is.EqualTo(string.Empty));
        Assert.That(dto.MediaTitle, Is.EqualTo(string.Empty));
        Assert.That(dto.PosterPath, Is.Null);
        Assert.That(dto.AverageRating, Is.EqualTo(0));
        Assert.That(dto.TotalRatings, Is.EqualTo(0));
    }
}

public class ChatDtoTests
{
    [Test]
    public void ChatMessageDto_ShouldInitializeWithDefaultValues()
    {
        var dto = new ChatMessageDto();
        Assert.That(dto.Id, Is.EqualTo(string.Empty));
        Assert.That(dto.GroupId, Is.EqualTo(string.Empty));
        Assert.That(dto.UserId, Is.EqualTo(string.Empty));
        Assert.That(dto.UserName, Is.EqualTo(string.Empty));
        Assert.That(dto.Message, Is.EqualTo(string.Empty));
    }

    [Test]
    public void SendMessageDto_ShouldInitializeWithDefaultValues()
    {
        var dto = new SendMessageDto();
        Assert.That(dto.Message, Is.EqualTo(string.Empty));
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// Repository Interface Tests
// ══════════════════════════════════════════════════════════════════════════════

public class IGroupRepositoryTests
{
    [Test]
    public void ShouldDefineAllMethods()
    {
        var t = typeof(IGroupRepository);
        Assert.That(t.GetMethod("GetByIdAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("GetUserGroupsAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("CreateAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("UpdateAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("DeleteAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("GetByInviteCodeAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("SearchGroupsAsync"), Is.Not.Null);
    }

    [Test]
    public void GroupRepository_ShouldImplementIGroupRepository()
    {
        Assert.That(typeof(GroupRepository).GetInterface(nameof(IGroupRepository)), Is.Not.Null);
    }

    [Test]
    public void GroupRepository_ShouldHaveConstructorWithIMongoDatabase()
    {
        var ctors = typeof(GroupRepository).GetConstructors();
        Assert.That(ctors.Length, Is.EqualTo(1));
        var p = ctors[0].GetParameters();
        Assert.That(p.Length, Is.EqualTo(1));
        Assert.That(p[0].ParameterType, Is.EqualTo(typeof(IMongoDatabase)));
    }
}

public class IGroupRatingRepositoryTests
{
    [Test]
    public void ShouldDefineAllMethods()
    {
        var t = typeof(IGroupRatingRepository);
        Assert.That(t.GetMethod("GetGroupRatingsAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("GetByIdAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("CreateAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("DeleteAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("GetGroupRatingsByMediaAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("GetGroupAverageAsync"), Is.Not.Null);
    }

    [Test]
    public void GroupRatingRepository_ShouldImplementIGroupRatingRepository()
    {
        Assert.That(typeof(GroupRatingRepository).GetInterface(nameof(IGroupRatingRepository)), Is.Not.Null);
    }

    [Test]
    public void GroupRatingRepository_ShouldHaveConstructorWithIMongoDatabase()
    {
        var ctors = typeof(GroupRatingRepository).GetConstructors();
        Assert.That(ctors.Length, Is.EqualTo(1));
        var p = ctors[0].GetParameters();
        Assert.That(p.Length, Is.EqualTo(1));
        Assert.That(p[0].ParameterType, Is.EqualTo(typeof(IMongoDatabase)));
    }
}

public class IChatRepositoryTests
{
    [Test]
    public void ShouldDefineAllMethods()
    {
        var t = typeof(IChatRepository);
        Assert.That(t.GetMethod("GetMessagesAsync"), Is.Not.Null);
        Assert.That(t.GetMethod("SaveMessageAsync"), Is.Not.Null);
    }

    [Test]
    public void ChatRepository_ShouldImplementIChatRepository()
    {
        Assert.That(typeof(ChatRepository).GetInterface(nameof(IChatRepository)), Is.Not.Null);
    }

    [Test]
    public void ChatRepository_ShouldHaveConstructorWithIMongoDatabase()
    {
        var ctors = typeof(ChatRepository).GetConstructors();
        Assert.That(ctors.Length, Is.EqualTo(1));
        var p = ctors[0].GetParameters();
        Assert.That(p.Length, Is.EqualTo(1));
        Assert.That(p[0].ParameterType, Is.EqualTo(typeof(IMongoDatabase)));
    }
}
