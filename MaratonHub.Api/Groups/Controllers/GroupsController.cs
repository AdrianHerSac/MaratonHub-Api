using MaratonHub.Api.Groups.Dtos;
using MaratonHub.Api.Groups.Models;
using MaratonHub.Api.Groups.Repositories;
using MaratonHub.Api.Notifications.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MaratonHub.Api.Groups.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupRatingRepository _groupRatingRepository;
    private readonly IChatRepository _chatRepository;
    private readonly INotificationService _notificationService;

    public GroupsController(
        IGroupRepository groupRepository,
        IGroupRatingRepository groupRatingRepository,
        IChatRepository chatRepository,
        INotificationService notificationService)
    {
        _groupRepository = groupRepository;
        _groupRatingRepository = groupRatingRepository;
        _chatRepository = chatRepository;
        _notificationService = notificationService;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "";

    private string GetUserName() =>
        User.FindFirstValue("unique_name") ?? User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Unknown";

    [HttpGet]
    public async Task<IActionResult> GetMyGroups()
    {
        var userId = GetUserId();
        var groups = await _groupRepository.GetUserGroupsAsync(userId);
        var dtos = groups.Select(g => new GroupSummaryDto
        {
            Id = g.Id!,
            Name = g.Name,
            Description = g.Description,
            CreatedByName = g.CreatedByName,
            MemberCount = g.Members.Count,
            CreatedAt = g.CreatedAt
        }).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGroup(string id)
    {
        var group = await _groupRepository.GetByIdAsync(id);
        if (group == null) return NotFound("Grupo no encontrado.");

        var userId = GetUserId();
        if (!group.Members.Any(m => m.UserId == userId))
            return Forbid();

        var dto = new GroupDto
        {
            Id = group.Id!,
            Name = group.Name,
            Description = group.Description,
            CreatedById = group.CreatedById,
            CreatedByName = group.CreatedByName,
            CreatedAt = group.CreatedAt,
            InviteCode = group.InviteCode,
            Members = group.Members.Select(m => new GroupMemberDto
            {
                UserId = m.UserId,
                UserName = m.UserName,
                Role = m.Role,
                JoinedAt = m.JoinedAt
            }).ToList()
        };
        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("El nombre del grupo es obligatorio.");

        var userId = GetUserId();
        var userName = GetUserName();

        var group = new Group
        {
            Name = dto.Name,
            Description = dto.Description,
            CreatedById = userId,
            CreatedByName = userName,
            InviteCode = Guid.NewGuid().ToString("N")[..8].ToUpper(),
            Members = new List<GroupMember>
            {
                new GroupMember
                {
                    UserId = userId,
                    UserName = userName,
                    Role = "Admin",
                    JoinedAt = DateTime.UtcNow
                }
            }
        };

        var created = await _groupRepository.CreateAsync(group);

        return CreatedAtAction(nameof(GetGroup), new { id = created.Id }, new GroupDto
        {
            Id = created.Id!,
            Name = created.Name,
            Description = created.Description,
            CreatedById = created.CreatedById,
            CreatedByName = created.CreatedByName,
            CreatedAt = created.CreatedAt,
            InviteCode = created.InviteCode,
            Members = created.Members.Select(m => new GroupMemberDto
            {
                UserId = m.UserId,
                UserName = m.UserName,
                Role = m.Role,
                JoinedAt = m.JoinedAt
            }).ToList()
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateGroup(string id, [FromBody] UpdateGroupDto dto)
    {
        var group = await _groupRepository.GetByIdAsync(id);
        if (group == null) return NotFound("Grupo no encontrado.");

        var userId = GetUserId();
        var member = group.Members.FirstOrDefault(m => m.UserId == userId);
        if (member == null || member.Role != "Admin")
            return Forbid();

        if (!string.IsNullOrWhiteSpace(dto.Name))
            group.Name = dto.Name;
        if (dto.Description != null)
            group.Description = dto.Description;

        await _groupRepository.UpdateAsync(id, group);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGroup(string id)
    {
        var group = await _groupRepository.GetByIdAsync(id);
        if (group == null) return NotFound("Grupo no encontrado.");

        var userId = GetUserId();
        var member = group.Members.FirstOrDefault(m => m.UserId == userId);
        if (member == null || member.Role != "Admin")
            return Forbid();

        await _groupRepository.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/join")]
    public async Task<IActionResult> JoinGroup(string id, [FromBody] JoinGroupDto dto)
    {
        var group = await _groupRepository.GetByIdAsync(id);
        if (group == null) return NotFound("Grupo no encontrado.");

        if (string.IsNullOrWhiteSpace(dto.InviteCode) || group.InviteCode != dto.InviteCode)
            return BadRequest("Código de invitación inválido.");

        var userId = GetUserId();
        var userName = GetUserName();

        if (group.Members.Any(m => m.UserId == userId))
            return BadRequest("Ya eres miembro de este grupo.");

        group.Members.Add(new GroupMember
        {
            UserId = userId,
            UserName = userName,
            Role = "Member",
            JoinedAt = DateTime.UtcNow
        });

        await _groupRepository.UpdateAsync(id, group);

        await _notificationService.CreateAndSendAsync(
            group.CreatedById,
            $"Nuevo miembro en {group.Name}",
            $"{userName} se ha unido al grupo.",
            "success",
            id);

        return Ok(new { message = "Te has unido al grupo exitosamente." });
    }

    [HttpPost("join/{inviteCode}")]
    public async Task<IActionResult> JoinGroupByCode(string inviteCode)
    {
        var group = await _groupRepository.GetByInviteCodeAsync(inviteCode);
        if (group == null) return NotFound("Código de invitación inválido o grupo no encontrado.");

        var userId = GetUserId();
        var userName = GetUserName();

        if (group.Members.Any(m => m.UserId == userId))
            return BadRequest("Ya eres miembro de este grupo.");

        group.Members.Add(new GroupMember
        {
            UserId = userId,
            UserName = userName,
            Role = "Member",
            JoinedAt = DateTime.UtcNow
        });

        await _groupRepository.UpdateAsync(group.Id!, group);

        await _notificationService.CreateAndSendAsync(
            group.CreatedById,
            $"Nuevo miembro en {group.Name}",
            $"{userName} se ha unido al grupo.",
            "success",
            group.Id!);

        return Ok(new { message = "Te has unido al grupo exitosamente." });
    }

    [HttpPost("{id}/generate-invite")]
    public async Task<IActionResult> GenerateInviteCode(string id)
    {
        var group = await _groupRepository.GetByIdAsync(id);
        if (group == null) return NotFound("Grupo no encontrado.");

        var userId = GetUserId();
        var member = group.Members.FirstOrDefault(m => m.UserId == userId);
        if (member == null || member.Role != "Admin")
            return Forbid();

        group.InviteCode = Guid.NewGuid().ToString("N")[..8].ToUpper();
        await _groupRepository.UpdateAsync(id, group);

        return Ok(new { inviteCode = group.InviteCode });
    }

    [HttpPost("{id}/leave")]
    public async Task<IActionResult> LeaveGroup(string id)
    {
        var group = await _groupRepository.GetByIdAsync(id);
        if (group == null) return NotFound("Grupo no encontrado.");

        var userId = GetUserId();
        var member = group.Members.FirstOrDefault(m => m.UserId == userId);
        if (member == null) return BadRequest("No eres miembro de este grupo.");

        if (member.Role == "Admin" && group.Members.Count(m => m.Role == "Admin") == 1)
            return BadRequest("No puedes abandonar el grupo siendo el único administrador. Promueve a otro miembro primero.");

        group.Members.Remove(member);
        await _groupRepository.UpdateAsync(id, group);
        return Ok(new { message = "Has abandonado el grupo." });
    }

    [HttpPost("{id}/members/{userId}/promote")]
    public async Task<IActionResult> PromoteMember(string id, string targetUserId)
    {
        var group = await _groupRepository.GetByIdAsync(id);
        if (group == null) return NotFound("Grupo no encontrado.");

        var currentUserId = GetUserId();
        var currentMember = group.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (currentMember == null || currentMember.Role != "Admin")
            return Forbid();

        var targetMember = group.Members.FirstOrDefault(m => m.UserId == targetUserId);
        if (targetMember == null) return NotFound("Miembro no encontrado.");

        targetMember.Role = "Admin";
        await _groupRepository.UpdateAsync(id, group);
        return NoContent();
    }

    [HttpDelete("{id}/members/{targetUserId}")]
    public async Task<IActionResult> RemoveMember(string id, string targetUserId)
    {
        var group = await _groupRepository.GetByIdAsync(id);
        if (group == null) return NotFound("Grupo no encontrado.");

        var currentUserId = GetUserId();
        var currentMember = group.Members.FirstOrDefault(m => m.UserId == currentUserId);
        if (currentMember == null || currentMember.Role != "Admin")
            return Forbid();

        var targetMember = group.Members.FirstOrDefault(m => m.UserId == targetUserId);
        if (targetMember == null) return NotFound("Miembro no encontrado.");

        if (targetMember.Role == "Admin")
            return BadRequest("No puedes eliminar a un administrador.");

        group.Members.Remove(targetMember);
        await _groupRepository.UpdateAsync(id, group);
        return NoContent();
    }

    [HttpGet("{id}/ratings")]
    public async Task<IActionResult> GetGroupRatings(string id)
    {
        var group = await _groupRepository.GetByIdAsync(id);
        if (group == null) return NotFound("Grupo no encontrado.");

        var userId = GetUserId();
        if (!group.Members.Any(m => m.UserId == userId))
            return Forbid();

        var ratings = await _groupRatingRepository.GetGroupRatingsAsync(id);
        var dtos = ratings.Select(r => new GroupRatingDto
        {
            Id = r.Id!,
            GroupId = r.GroupId,
            UserId = r.UserId,
            UserName = r.UserName,
            MediaId = r.MediaId,
            MediaType = r.MediaType,
            MediaTitle = r.MediaTitle,
            PosterPath = r.PosterPath,
            Rating = r.Rating,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt
        }).ToList();

        return Ok(dtos);
    }

    [HttpPost("{id}/ratings")]
    public async Task<IActionResult> CreateGroupRating(string id, [FromBody] CreateGroupRatingDto dto)
    {
        if (dto.Rating < 1 || dto.Rating > 5)
            return BadRequest("Rating must be between 1 and 5");

        var group = await _groupRepository.GetByIdAsync(id);
        if (group == null) return NotFound("Grupo no encontrado.");

        var userId = GetUserId();
        var userName = GetUserName();

        if (!group.Members.Any(m => m.UserId == userId))
            return Forbid();

        var rating = new GroupRating
        {
            GroupId = id,
            UserId = userId,
            UserName = userName,
            MediaId = dto.MediaId,
            MediaType = dto.MediaType,
            MediaTitle = dto.MediaTitle,
            PosterPath = dto.PosterPath,
            Rating = dto.Rating,
            Comment = dto.Comment
        };

        var created = await _groupRatingRepository.CreateAsync(rating);

        foreach (var member in group.Members.Where(m => m.UserId != userId))
        {
            await _notificationService.CreateAndSendAsync(
                member.UserId,
                $"Nueva valoración en {group.Name}",
                $"{userName} valoró {dto.MediaTitle} con {dto.Rating} ★",
                "info",
                id);
        }

        return CreatedAtAction(nameof(GetGroupRatings), new { id }, new GroupRatingDto
        {
            Id = created.Id!,
            GroupId = created.GroupId,
            UserId = created.UserId,
            UserName = created.UserName,
            MediaId = created.MediaId,
            MediaType = created.MediaType,
            MediaTitle = created.MediaTitle,
            PosterPath = created.PosterPath,
            Rating = created.Rating,
            Comment = created.Comment,
            CreatedAt = created.CreatedAt
        });
    }

    [HttpDelete("{id}/ratings/{ratingId}")]
    public async Task<IActionResult> DeleteGroupRating(string id, string ratingId)
    {
        var group = await _groupRepository.GetByIdAsync(id);
        if (group == null) return NotFound("Grupo no encontrado.");

        var rating = await _groupRatingRepository.GetByIdAsync(ratingId);
        if (rating == null) return NotFound("Valoración no encontrada.");

        var userId = GetUserId();
        var member = group.Members.FirstOrDefault(m => m.UserId == userId);

        var isSiteAdmin = User.IsInRole("Admin") || GetUserName().Equals("Adrian", StringComparison.OrdinalIgnoreCase);
        var isGroupAdmin = member != null && member.Role == "Admin";
        var isCreator = rating.UserId == userId;

        if (!isSiteAdmin && !isGroupAdmin && !isCreator)
            return Forbid();

        var deleted = await _groupRatingRepository.DeleteAsync(ratingId);
        if (!deleted) return BadRequest("No se pudo eliminar la valoración.");

        return NoContent();
    }

    [HttpGet("{id}/ratings/average/{mediaType}/{mediaId}")]
    public async Task<IActionResult> GetGroupAverageRating(string id, string mediaType, int mediaId)
    {
        var group = await _groupRepository.GetByIdAsync(id);
        if (group == null) return NotFound("Grupo no encontrado.");

        var userId = GetUserId();
        if (!group.Members.Any(m => m.UserId == userId))
            return Forbid();

        var average = await _groupRatingRepository.GetGroupAverageAsync(id, mediaId, mediaType);
        if (average == null)
            return Ok(new { averageRating = 0, totalRatings = 0 });

        return Ok(new
        {
            averageRating = average.AverageRating,
            totalRatings = average.TotalRatings
        });
    }

    [HttpGet("{id}/chat")]
    public async Task<IActionResult> GetChatMessages(string id, [FromQuery] int limit = 50)
    {
        var group = await _groupRepository.GetByIdAsync(id);
        if (group == null) return NotFound("Grupo no encontrado.");

        var userId = GetUserId();
        if (!group.Members.Any(m => m.UserId == userId))
            return Forbid();

        var messages = await _chatRepository.GetMessagesAsync(id, limit);
        var dtos = messages.Select(m => new ChatMessageDto
        {
            Id = m.Id!,
            GroupId = m.GroupId,
            UserId = m.UserId,
            UserName = m.UserName,
            Message = m.Message,
            SentAt = m.SentAt
        }).Reverse().ToList();

        return Ok(dtos);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchGroups([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new List<GroupSummaryDto>());

        var groups = await _groupRepository.SearchGroupsAsync(q);
        var userId = GetUserId();
        var dtos = groups.Where(g => !g.Members.Any(m => m.UserId == userId)).Select(g => new GroupSummaryDto
        {
            Id = g.Id!,
            Name = g.Name,
            Description = g.Description,
            CreatedByName = g.CreatedByName,
            MemberCount = g.Members.Count,
            CreatedAt = g.CreatedAt
        }).ToList();

        return Ok(dtos);
    }
}
