using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGestion.Application.Notificaciones;
using SistemaGestion.Application.Notificaciones.Dtos;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.Controllers;

/// <summary>
/// Notificaciones del usuario autenticado (api-spec §18): listado propio
/// paginado, conteo de no leídas y marcar como leídas (BR-090/BR-091).
/// No requiere permisos granulares: cada usuario sólo accede a las
/// suyas, resueltas desde el token (gate de roadmap Phase 13).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly NotificacionesService _notificacionesService;

    public NotificationsController(NotificacionesService notificacionesService)
    {
        _notificacionesService = notificacionesService;
    }

    /// <summary>Listado paginado de las notificaciones del usuario.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = NotificacionesService.DefaultPageSize,
        [FromQuery] bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        var firebaseUid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _notificacionesService.GetListAsync(
            firebaseUid ?? string.Empty, page, pageSize, unreadOnly, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>Conteo de notificaciones no leídas (badge de la UI).</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(UnreadCountDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var firebaseUid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _notificacionesService.CountUnreadAsync(
            firebaseUid ?? string.Empty, cancellationToken);

        return ToHttpResult(result, count => Ok(new UnreadCountDto(count)));
    }

    /// <summary>Marca una notificación propia como leída (BR-091).</summary>
    [HttpPost("{id:int}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(int id, CancellationToken cancellationToken)
    {
        var firebaseUid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _notificacionesService.MarkReadAsync(
            firebaseUid ?? string.Empty, id, cancellationToken);

        return ToHttpResult(result, () => NoContent());
    }

    /// <summary>Marca todas las notificaciones del usuario como leídas.</summary>
    [HttpPost("read-all")]
    [ProducesResponseType(typeof(MarkAllReadDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var firebaseUid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _notificacionesService.MarkAllReadAsync(
            firebaseUid ?? string.Empty, cancellationToken);

        return ToHttpResult(result, updated => Ok(new MarkAllReadDto(updated)));
    }

    private IActionResult ToHttpResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value);
        }

        return ProblemFromError(result.Error);
    }

    private IActionResult ToHttpResult(Result result, Func<IActionResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess();
        }

        return ProblemFromError(result.Error);
    }

    private IActionResult ProblemFromError(Error error)
    {
        var problem = new ProblemDetails
        {
            Title = error.Code,
            Detail = error.Description,
            Status = error.Code switch
            {
                "Validation.Error" => StatusCodes.Status400BadRequest,
                "NotFound.Error" => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status500InternalServerError,
            },
            Instance = HttpContext.Request.Path,
        };

        return StatusCode(problem.Status.Value, problem);
    }

    /// <summary>Respuesta de GET /notifications/unread-count.</summary>
    public sealed record UnreadCountDto(int Count);

    /// <summary>Respuesta de POST /notifications/read-all.</summary>
    public sealed record MarkAllReadDto(int Updated);
}
