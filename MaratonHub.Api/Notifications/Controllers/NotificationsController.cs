using MaratonHub.Api.Notifications.Dtos;
using MaratonHub.Api.Notifications.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MaratonHub.Api.Notifications.Controllers;

/// <summary>
/// API controller para la gestión de notificaciones.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationRepository _repository;

    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="NotificationsController"/>.
    /// </summary>
    public NotificationsController(INotificationRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Obtiene el ID del usuario actual desde el token de autenticación.
    /// </summary>
    /// <returns>El ID del usuario como string.</returns>
    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "";

    /// <summary>
    /// Obtiene todas las notificaciones del usuario actual.
    /// </summary>
    /// <returns>Una lista de notificaciones.</returns>
    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        var userId = GetUserId();
        var notifications = await _repository.GetUserNotificationsAsync(userId);
        var dtos = notifications.Select(n => new NotificationDto
        {
            Id = n.Id!,
            UserId = n.UserId,
            Title = n.Title,
            Message = n.Message,
            Type = n.Type,
            GroupId = n.GroupId,
            Read = n.Read,
            CreatedAt = n.CreatedAt
        }).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// Obtiene el número de notificaciones no leídas del usuario actual.
    /// </summary>
    /// <returns>Un objeto con el conteo de notificaciones no leídas.</returns>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetUserId();
        var count = await _repository.GetUnreadCountAsync(userId);
        return Ok(new { count });
    }

    /// <summary>
    /// Marca una notificación específica como leída.
    /// </summary>
    /// <param name="id">ID de la notificación.</param>
    /// <returns>Resultado de la operación.</returns>
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(string id)
    {
        var result = await _repository.MarkAsReadAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Marca todas las notificaciones del usuario actual como leídas.
    /// </summary>
    /// <returns>Resultado de la operación.</returns>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetUserId();
        await _repository.MarkAllAsReadAsync(userId);
        return NoContent();
    }

    /// <summary>
    /// Elimina una notificación específica.
    /// </summary>
    /// <param name="id">ID de la notificación.</param>
    /// <returns>Resultado de la operación.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(string id)
    {
        var result = await _repository.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Elimina todas las notificaciones del usuario actual.
    /// </summary>
    /// <returns>Resultado de la operación.</returns>
    [HttpDelete]
    public async Task<IActionResult> ClearAll()
    {
        var userId = GetUserId();
        await _repository.ClearAllAsync(userId);
        return NoContent();
    }
}
