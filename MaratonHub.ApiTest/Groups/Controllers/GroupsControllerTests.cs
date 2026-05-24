using MaratonHub.Api.Groups.Controllers;
using MaratonHub.Api.Groups.Dtos;
using MaratonHub.Api.Groups.Models;
using MaratonHub.Api.Groups.Repositories;
using MaratonHub.Api.Notifications.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace MaratonHub.ApiTest.Groups.Controllers;

public class GroupsControllerTests
{
    private Mock<IGroupRepository> _mockGroupRepo = null!;
    private Mock<IGroupRatingRepository> _mockRatingRepo = null!;
    private Mock<IChatRepository> _mockChatRepo = null!;
    private Mock<INotificationService> _mockNotifService = null!;
    private GroupsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mockGroupRepo = new Mock<IGroupRepository>();
        _mockRatingRepo = new Mock<IGroupRatingRepository>();
        _mockChatRepo = new Mock<IChatRepository>();
        _mockNotifService = new Mock<INotificationService>();
        _controller = new GroupsController(
            _mockGroupRepo.Object,
            _mockRatingRepo.Object,
            _mockChatRepo.Object,
            _mockNotifService.Object);
    }

    private void SetupUserClaims(string userId = "user1", string userName = "TestUser")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("unique_name", userName)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private Group MakeGroup(string id, string creatorId = "user1", string creatorName = "TestUser") => new Group
    {
        Id = id,
        Name = "Test Group",
        Description = "A test group",
        CreatedById = creatorId,
        CreatedByName = creatorName,
        InviteCode = "INVITE01",
        Members = new List<GroupMember>
        {
            new() { UserId = creatorId, UserName = creatorName, Role = "Admin" }
        }
    };

    // ── GetMyGroups ────────────────────────────────────────────────────────

    [Test]
    public async Task GetMyGroups_ShouldReturnOkWithGroups()
    {
        SetupUserClaims();
        var groups = new List<Group> { MakeGroup("g1") };
        _mockGroupRepo.Setup(r => r.GetUserGroupsAsync("user1")).ReturnsAsync(groups);

        var result = await _controller.GetMyGroups();

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var dtos = ok!.Value as List<GroupSummaryDto>;
        Assert.That(dtos, Is.Not.Null);
        Assert.That(dtos!.Count, Is.EqualTo(1));
        Assert.That(dtos[0].Name, Is.EqualTo("Test Group"));
        Assert.That(dtos[0].MemberCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GetMyGroups_WithNoGroups_ShouldReturnEmptyList()
    {
        SetupUserClaims();
        _mockGroupRepo.Setup(r => r.GetUserGroupsAsync("user1")).ReturnsAsync(new List<Group>());

        var result = await _controller.GetMyGroups();

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var dtos = ok!.Value as List<GroupSummaryDto>;
        Assert.That(dtos!.Count, Is.EqualTo(0));
    }

    // ── GetGroup ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetGroup_WhenMember_ShouldReturnOk()
    {
        SetupUserClaims();
        var group = MakeGroup("g1");
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);

        var result = await _controller.GetGroup("g1");

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var dto = ok!.Value as GroupDto;
        Assert.That(dto, Is.Not.Null);
        Assert.That(dto!.Name, Is.EqualTo("Test Group"));
        Assert.That(dto.Members.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task GetGroup_WhenNotFound_ShouldReturnNotFound()
    {
        SetupUserClaims();
        _mockGroupRepo.Setup(r => r.GetByIdAsync("nonexistent")).ReturnsAsync((Group?)null);

        var result = await _controller.GetGroup("nonexistent");

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task GetGroup_WhenNotMember_ShouldReturnForbid()
    {
        SetupUserClaims("outsider", "Outsider");
        var group = MakeGroup("g1");
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);

        var result = await _controller.GetGroup("g1");

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    // ── CreateGroup ───────────────────────────────────────────────────────

    [Test]
    public async Task CreateGroup_WithValidName_ShouldReturnCreated()
    {
        SetupUserClaims();
        var dto = new CreateGroupDto { Name = "New Group", Description = "Desc" };
        _mockGroupRepo.Setup(r => r.CreateAsync(It.IsAny<Group>()))
            .ReturnsAsync((Group g) => { g.Id = "newGroupId"; return g; });

        var result = await _controller.CreateGroup(dto);

        var created = result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        Assert.That(created!.StatusCode, Is.EqualTo(201));
        var groupDto = created.Value as GroupDto;
        Assert.That(groupDto!.Name, Is.EqualTo("New Group"));
    }

    [Test]
    public async Task CreateGroup_WithEmptyName_ShouldReturnBadRequest()
    {
        SetupUserClaims();
        var dto = new CreateGroupDto { Name = "" };

        var result = await _controller.CreateGroup(dto);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateGroup_WithWhitespaceName_ShouldReturnBadRequest()
    {
        SetupUserClaims();
        var dto = new CreateGroupDto { Name = "   " };

        var result = await _controller.CreateGroup(dto);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateGroup_ShouldAddCreatorAsAdmin()
    {
        SetupUserClaims("creator1", "CreatorUser");
        var dto = new CreateGroupDto { Name = "Admin Group" };
        Group? capturedGroup = null;
        _mockGroupRepo.Setup(r => r.CreateAsync(It.IsAny<Group>()))
            .Callback<Group>(g => capturedGroup = g)
            .ReturnsAsync((Group g) => { g.Id = "gId"; return g; });

        await _controller.CreateGroup(dto);

        Assert.That(capturedGroup, Is.Not.Null);
        Assert.That(capturedGroup!.Members.Count, Is.EqualTo(1));
        Assert.That(capturedGroup.Members[0].Role, Is.EqualTo("Admin"));
        Assert.That(capturedGroup.Members[0].UserId, Is.EqualTo("creator1"));
    }

    [Test]
    public async Task CreateGroup_ShouldGenerateInviteCode()
    {
        SetupUserClaims();
        var dto = new CreateGroupDto { Name = "Group" };
        Group? capturedGroup = null;
        _mockGroupRepo.Setup(r => r.CreateAsync(It.IsAny<Group>()))
            .Callback<Group>(g => capturedGroup = g)
            .ReturnsAsync((Group g) => { g.Id = "gId"; return g; });

        await _controller.CreateGroup(dto);

        Assert.That(capturedGroup!.InviteCode, Is.Not.Null);
        Assert.That(capturedGroup.InviteCode!.Length, Is.EqualTo(8));
    }

    // ── UpdateGroup ───────────────────────────────────────────────────────

    [Test]
    public async Task UpdateGroup_WhenAdmin_ShouldReturnNoContent()
    {
        SetupUserClaims("user1");
        var group = MakeGroup("g1");
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);
        _mockGroupRepo.Setup(r => r.UpdateAsync("g1", group)).ReturnsAsync(group);

        var dto = new UpdateGroupDto { Name = "Updated Name", Description = "New Desc" };
        var result = await _controller.UpdateGroup("g1", dto);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        Assert.That(group.Name, Is.EqualTo("Updated Name"));
    }

    [Test]
    public async Task UpdateGroup_WhenNotFound_ShouldReturnNotFound()
    {
        SetupUserClaims();
        _mockGroupRepo.Setup(r => r.GetByIdAsync("x")).ReturnsAsync((Group?)null);

        var result = await _controller.UpdateGroup("x", new UpdateGroupDto { Name = "New" });

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task UpdateGroup_WhenNotAdmin_ShouldReturnForbid()
    {
        SetupUserClaims("member1");
        var group = MakeGroup("g1");
        group.Members.Add(new GroupMember { UserId = "member1", Role = "Member" });
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);

        var result = await _controller.UpdateGroup("g1", new UpdateGroupDto { Name = "New" });

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task UpdateGroup_WithNullName_ShouldNotChangeName()
    {
        SetupUserClaims("user1");
        var group = MakeGroup("g1");
        group.Name = "Original Name";
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);
        _mockGroupRepo.Setup(r => r.UpdateAsync("g1", group)).ReturnsAsync(group);

        var dto = new UpdateGroupDto { Name = null, Description = "New desc" };
        await _controller.UpdateGroup("g1", dto);

        Assert.That(group.Name, Is.EqualTo("Original Name"));
    }

    // ── DeleteGroup ───────────────────────────────────────────────────────

    [Test]
    public async Task DeleteGroup_WhenAdmin_ShouldReturnNoContent()
    {
        SetupUserClaims("user1");
        var group = MakeGroup("g1");
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);
        _mockGroupRepo.Setup(r => r.DeleteAsync("g1")).ReturnsAsync(true);

        var result = await _controller.DeleteGroup("g1");

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeleteGroup_WhenNotFound_ShouldReturnNotFound()
    {
        SetupUserClaims();
        _mockGroupRepo.Setup(r => r.GetByIdAsync("x")).ReturnsAsync((Group?)null);

        var result = await _controller.DeleteGroup("x");

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task DeleteGroup_WhenNotAdmin_ShouldReturnForbid()
    {
        SetupUserClaims("member1");
        var group = MakeGroup("g1");
        group.Members.Add(new GroupMember { UserId = "member1", Role = "Member" });
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);

        var result = await _controller.DeleteGroup("g1");

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    // ── JoinGroup ─────────────────────────────────────────────────────────

    [Test]
    public async Task JoinGroup_WithValidCode_ShouldReturnOk()
    {
        SetupUserClaims("newuser", "NewUser");
        var group = MakeGroup("g1");
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);
        _mockGroupRepo.Setup(r => r.UpdateAsync("g1", group)).ReturnsAsync(group);
        _mockNotifService.Setup(n => n.CreateAndSendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var dto = new JoinGroupDto { InviteCode = "INVITE01" };
        var result = await _controller.JoinGroup("g1", dto);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        Assert.That(group.Members.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task JoinGroup_WithInvalidCode_ShouldReturnBadRequest()
    {
        SetupUserClaims("newuser");
        var group = MakeGroup("g1");
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);

        var dto = new JoinGroupDto { InviteCode = "WRONGCODE" };
        var result = await _controller.JoinGroup("g1", dto);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task JoinGroup_WhenAlreadyMember_ShouldReturnBadRequest()
    {
        SetupUserClaims("user1");
        var group = MakeGroup("g1");
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);

        var dto = new JoinGroupDto { InviteCode = "INVITE01" };
        var result = await _controller.JoinGroup("g1", dto);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task JoinGroup_WhenGroupNotFound_ShouldReturnNotFound()
    {
        SetupUserClaims();
        _mockGroupRepo.Setup(r => r.GetByIdAsync("x")).ReturnsAsync((Group?)null);

        var result = await _controller.JoinGroup("x", new JoinGroupDto { InviteCode = "code" });

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    // ── JoinGroupByCode ───────────────────────────────────────────────────

    [Test]
    public async Task JoinGroupByCode_WithValidCode_ShouldReturnOk()
    {
        SetupUserClaims("newuser2", "NewUser2");
        var group = MakeGroup("g1");
        _mockGroupRepo.Setup(r => r.GetByInviteCodeAsync("INVITE01")).ReturnsAsync(group);
        _mockGroupRepo.Setup(r => r.UpdateAsync("g1", group)).ReturnsAsync(group);
        _mockNotifService.Setup(n => n.CreateAndSendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var result = await _controller.JoinGroupByCode("INVITE01");

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task JoinGroupByCode_WhenInvalidCode_ShouldReturnNotFound()
    {
        SetupUserClaims();
        _mockGroupRepo.Setup(r => r.GetByInviteCodeAsync("INVALID")).ReturnsAsync((Group?)null);

        var result = await _controller.JoinGroupByCode("INVALID");

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task JoinGroupByCode_WhenAlreadyMember_ShouldReturnBadRequest()
    {
        SetupUserClaims("user1");
        var group = MakeGroup("g1");
        _mockGroupRepo.Setup(r => r.GetByInviteCodeAsync("INVITE01")).ReturnsAsync(group);

        var result = await _controller.JoinGroupByCode("INVITE01");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    // ── GenerateInviteCode ────────────────────────────────────────────────

    [Test]
    public async Task GenerateInviteCode_WhenAdmin_ShouldReturnOkWithNewCode()
    {
        SetupUserClaims("user1");
        var group = MakeGroup("g1");
        group.InviteCode = "OLDCODE1";
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);
        _mockGroupRepo.Setup(r => r.UpdateAsync("g1", group)).ReturnsAsync(group);

        var result = await _controller.GenerateInviteCode("g1");

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(group.InviteCode, Is.Not.EqualTo("OLDCODE1"));
        Assert.That(group.InviteCode!.Length, Is.EqualTo(8));
    }

    [Test]
    public async Task GenerateInviteCode_WhenNotFound_ShouldReturnNotFound()
    {
        SetupUserClaims();
        _mockGroupRepo.Setup(r => r.GetByIdAsync("x")).ReturnsAsync((Group?)null);

        var result = await _controller.GenerateInviteCode("x");

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task GenerateInviteCode_WhenNotAdmin_ShouldReturnForbid()
    {
        SetupUserClaims("member1");
        var group = MakeGroup("g1");
        group.Members.Add(new GroupMember { UserId = "member1", Role = "Member" });
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);

        var result = await _controller.GenerateInviteCode("g1");

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    // ── LeaveGroup ────────────────────────────────────────────────────────

    [Test]
    public async Task LeaveGroup_WhenMember_ShouldReturnOk()
    {
        SetupUserClaims("member1");
        var group = MakeGroup("g1");
        group.Members.Add(new GroupMember { UserId = "member1", Role = "Member" });
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);
        _mockGroupRepo.Setup(r => r.UpdateAsync("g1", group)).ReturnsAsync(group);

        var result = await _controller.LeaveGroup("g1");

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        Assert.That(group.Members.Any(m => m.UserId == "member1"), Is.False);
    }

    [Test]
    public async Task LeaveGroup_WhenNotMember_ShouldReturnBadRequest()
    {
        SetupUserClaims("outsider");
        var group = MakeGroup("g1");
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);

        var result = await _controller.LeaveGroup("g1");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task LeaveGroup_WhenSoleAdmin_ShouldReturnBadRequest()
    {
        SetupUserClaims("user1");
        var group = MakeGroup("g1");
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);

        var result = await _controller.LeaveGroup("g1");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task LeaveGroup_WhenGroupNotFound_ShouldReturnNotFound()
    {
        SetupUserClaims();
        _mockGroupRepo.Setup(r => r.GetByIdAsync("x")).ReturnsAsync((Group?)null);

        var result = await _controller.LeaveGroup("x");

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    // ── PromoteMember ─────────────────────────────────────────────────────

    [Test]
    public async Task PromoteMember_WhenAdmin_ShouldReturnNoContent()
    {
        SetupUserClaims("user1");
        var group = MakeGroup("g1");
        group.Members.Add(new GroupMember { UserId = "member1", Role = "Member" });
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);
        _mockGroupRepo.Setup(r => r.UpdateAsync("g1", group)).ReturnsAsync(group);

        var result = await _controller.PromoteMember("g1", "member1");

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        Assert.That(group.Members.First(m => m.UserId == "member1").Role, Is.EqualTo("Admin"));
    }

    [Test]
    public async Task PromoteMember_WhenGroupNotFound_ShouldReturnNotFound()
    {
        SetupUserClaims();
        _mockGroupRepo.Setup(r => r.GetByIdAsync("x")).ReturnsAsync((Group?)null);

        var result = await _controller.PromoteMember("x", "anyone");

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task PromoteMember_WhenCurrentUserNotAdmin_ShouldReturnForbid()
    {
        SetupUserClaims("member1");
        var group = MakeGroup("g1");
        group.Members.Add(new GroupMember { UserId = "member1", Role = "Member" });
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);

        var result = await _controller.PromoteMember("g1", "member1");

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task PromoteMember_WhenTargetNotMember_ShouldReturnNotFound()
    {
        SetupUserClaims("user1");
        var group = MakeGroup("g1");
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);

        var result = await _controller.PromoteMember("g1", "nonexistent");

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    // ── RemoveMember ──────────────────────────────────────────────────────

    [Test]
    public async Task RemoveMember_WhenAdmin_ShouldReturnNoContent()
    {
        SetupUserClaims("user1");
        var group = MakeGroup("g1");
        group.Members.Add(new GroupMember { UserId = "member1", Role = "Member" });
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);
        _mockGroupRepo.Setup(r => r.UpdateAsync("g1", group)).ReturnsAsync(group);

        var result = await _controller.RemoveMember("g1", "member1");

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        Assert.That(group.Members.Any(m => m.UserId == "member1"), Is.False);
    }

    [Test]
    public async Task RemoveMember_WhenTargetIsAdmin_ShouldReturnBadRequest()
    {
        SetupUserClaims("user1");
        var group = MakeGroup("g1");
        group.Members.Add(new GroupMember { UserId = "admin2", Role = "Admin" });
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);

        var result = await _controller.RemoveMember("g1", "admin2");

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task RemoveMember_WhenGroupNotFound_ShouldReturnNotFound()
    {
        SetupUserClaims();
        _mockGroupRepo.Setup(r => r.GetByIdAsync("x")).ReturnsAsync((Group?)null);

        var result = await _controller.RemoveMember("x", "anyone");

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task RemoveMember_WhenTargetNotMember_ShouldReturnNotFound()
    {
        SetupUserClaims("user1");
        var group = MakeGroup("g1");
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);

        var result = await _controller.RemoveMember("g1", "nobody");

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    // ── GetGroupRatings ───────────────────────────────────────────────────

    [Test]
    public async Task GetGroupRatings_WhenMember_ShouldReturnOkWithRatings()
    {
        SetupUserClaims("user1");
        var group = MakeGroup("g1");
        var ratings = new List<GroupRating>
        {
            new() { Id = "r1", GroupId = "g1", UserId = "user1", UserName = "TestUser", MediaId = 1, MediaType = "Movie", MediaTitle = "Test", Rating = 4, CreatedAt = DateTime.UtcNow }
        };
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);
        _mockRatingRepo.Setup(r => r.GetGroupRatingsAsync("g1")).ReturnsAsync(ratings);

        var result = await _controller.GetGroupRatings("g1");

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var dtos = ok!.Value as List<GroupRatingDto>;
        Assert.That(dtos!.Count, Is.EqualTo(1));
        Assert.That(dtos[0].MediaTitle, Is.EqualTo("Test"));
    }

    [Test]
    public async Task GetGroupRatings_WhenNotMember_ShouldReturnForbid()
    {
        SetupUserClaims("outsider");
        var group = MakeGroup("g1");
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);

        var result = await _controller.GetGroupRatings("g1");

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task GetGroupRatings_WhenGroupNotFound_ShouldReturnNotFound()
    {
        SetupUserClaims();
        _mockGroupRepo.Setup(r => r.GetByIdAsync("x")).ReturnsAsync((Group?)null);

        var result = await _controller.GetGroupRatings("x");

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    // ── CreateGroupRating ─────────────────────────────────────────────────

    [Test]
    public async Task CreateGroupRating_WithValidData_ShouldReturnCreated()
    {
        SetupUserClaims("user1", "TestUser");
        var group = MakeGroup("g1");
        var dto = new CreateGroupRatingDto { MediaId = 1, MediaType = "Movie", MediaTitle = "Test Movie", Rating = 4 };
        var createdRating = new GroupRating { Id = "r1", GroupId = "g1", Rating = 4, MediaTitle = "Test Movie" };
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);
        _mockRatingRepo.Setup(r => r.CreateAsync(It.IsAny<GroupRating>())).ReturnsAsync(createdRating);
        _mockNotifService.Setup(n => n.CreateAndSendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var result = await _controller.CreateGroupRating("g1", dto);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
    }

    [Test]
    public async Task CreateGroupRating_WithRatingBelow1_ShouldReturnBadRequest()
    {
        SetupUserClaims();
        var dto = new CreateGroupRatingDto { MediaId = 1, MediaType = "Movie", MediaTitle = "Test", Rating = 0 };

        var result = await _controller.CreateGroupRating("g1", dto);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateGroupRating_WithRatingAbove5_ShouldReturnBadRequest()
    {
        SetupUserClaims();
        var dto = new CreateGroupRatingDto { MediaId = 1, MediaType = "Movie", MediaTitle = "Test", Rating = 6 };

        var result = await _controller.CreateGroupRating("g1", dto);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateGroupRating_WhenGroupNotFound_ShouldReturnNotFound()
    {
        SetupUserClaims();
        var dto = new CreateGroupRatingDto { MediaId = 1, MediaType = "Movie", MediaTitle = "Test", Rating = 3 };
        _mockGroupRepo.Setup(r => r.GetByIdAsync("x")).ReturnsAsync((Group?)null);

        var result = await _controller.CreateGroupRating("x", dto);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task CreateGroupRating_WhenNotMember_ShouldReturnForbid()
    {
        SetupUserClaims("outsider");
        var group = MakeGroup("g1");
        var dto = new CreateGroupRatingDto { MediaId = 1, MediaType = "Movie", MediaTitle = "Test", Rating = 3 };
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);

        var result = await _controller.CreateGroupRating("g1", dto);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    // ── GetGroupAverageRating ─────────────────────────────────────────────

    [Test]
    public async Task GetGroupAverageRating_WhenMember_ShouldReturnOk()
    {
        SetupUserClaims("user1");
        var group = MakeGroup("g1");
        var avg = new GroupMediaAverageDto { AverageRating = 4.2, TotalRatings = 5 };
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);
        _mockRatingRepo.Setup(r => r.GetGroupAverageAsync("g1", 1, "Movie")).ReturnsAsync(avg);

        var result = await _controller.GetGroupAverageRating("g1", "Movie", 1);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetGroupAverageRating_WhenNoRatings_ShouldReturnZero()
    {
        SetupUserClaims("user1");
        var group = MakeGroup("g1");
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);
        _mockRatingRepo.Setup(r => r.GetGroupAverageAsync("g1", 1, "Movie")).ReturnsAsync((GroupMediaAverageDto?)null);

        var result = await _controller.GetGroupAverageRating("g1", "Movie", 1);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
    }

    [Test]
    public async Task GetGroupAverageRating_WhenGroupNotFound_ShouldReturnNotFound()
    {
        SetupUserClaims();
        _mockGroupRepo.Setup(r => r.GetByIdAsync("x")).ReturnsAsync((Group?)null);

        var result = await _controller.GetGroupAverageRating("x", "Movie", 1);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    // ── GetChatMessages ───────────────────────────────────────────────────

    [Test]
    public async Task GetChatMessages_WhenMember_ShouldReturnOkWithMessages()
    {
        SetupUserClaims("user1");
        var group = MakeGroup("g1");
        var messages = new List<ChatMessage>
        {
            new() { Id = "m1", GroupId = "g1", UserId = "user1", UserName = "TestUser", Message = "Hello!", SentAt = DateTime.UtcNow }
        };
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);
        _mockChatRepo.Setup(r => r.GetMessagesAsync("g1", 50)).ReturnsAsync(messages);

        var result = await _controller.GetChatMessages("g1", 50);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var dtos = ok!.Value as List<ChatMessageDto>;
        Assert.That(dtos!.Count, Is.EqualTo(1));
        Assert.That(dtos[0].Message, Is.EqualTo("Hello!"));
    }

    [Test]
    public async Task GetChatMessages_WhenGroupNotFound_ShouldReturnNotFound()
    {
        SetupUserClaims();
        _mockGroupRepo.Setup(r => r.GetByIdAsync("x")).ReturnsAsync((Group?)null);

        var result = await _controller.GetChatMessages("x", 50);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task GetChatMessages_WhenNotMember_ShouldReturnForbid()
    {
        SetupUserClaims("outsider");
        var group = MakeGroup("g1");
        _mockGroupRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(group);

        var result = await _controller.GetChatMessages("g1", 50);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    // ── SearchGroups ──────────────────────────────────────────────────────

    [Test]
    public async Task SearchGroups_WithQuery_ShouldReturnNonMemberGroups()
    {
        SetupUserClaims("user1");
        var group = new Group
        {
            Id = "g2",
            Name = "Other Group",
            CreatedById = "other",
            CreatedByName = "Other",
            Members = new List<GroupMember> { new() { UserId = "other", Role = "Admin" } }
        };
        _mockGroupRepo.Setup(r => r.SearchGroupsAsync("Other")).ReturnsAsync(new List<Group> { group });

        var result = await _controller.SearchGroups("Other");

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var dtos = ok!.Value as List<GroupSummaryDto>;
        Assert.That(dtos!.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task SearchGroups_ShouldExcludeGroupsUserIsAlreadyIn()
    {
        SetupUserClaims("user1");
        var myGroup = MakeGroup("g1");
        _mockGroupRepo.Setup(r => r.SearchGroupsAsync("test")).ReturnsAsync(new List<Group> { myGroup });

        var result = await _controller.SearchGroups("test");

        var ok = result as OkObjectResult;
        var dtos = ok!.Value as List<GroupSummaryDto>;
        Assert.That(dtos!.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task SearchGroups_WithEmptyQuery_ShouldReturnEmptyList()
    {
        SetupUserClaims();

        var result = await _controller.SearchGroups("");

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
    }

    [Test]
    public async Task SearchGroups_WithWhitespaceQuery_ShouldReturnEmptyList()
    {
        SetupUserClaims();

        var result = await _controller.SearchGroups("   ");

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
    }
}
