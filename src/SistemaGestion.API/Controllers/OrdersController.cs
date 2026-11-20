using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGestion.API.Authorization;
using SistemaGestion.Application.Pedidos;
using SistemaGestion.Application.Pedidos.Dtos;
using SistemaGestion.Domain.Authorization;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.Controllers;

/// <summary>
/// Pedidos (api-spec §16): consultas con filtros, detalle con el snapshot
/// del presupuesto aceptado (BR-071), historial inmutable de estados
/// (BR-075), cambio de estado validado por el dominio (BR-072) y edición
/// de la información de seguimiento (BR-076). Los pedidos se crean
/// aceptando un presupuesto (BR-065/BR-070).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/orders")]
public class OrdersController : ControllerBase
{
    private readonly PedidosService _pedidosService;

    private readonly IAuthorizationService _authorizationService;

    public OrdersController(
        PedidosService pedidosService,
        IAuthorizationService authorizationService)
    {
        _pedidosService = pedidosService;
        _authorizationService = authorizationService;
    }

    /// <summary>Listado paginado con filtros de cliente, estado y fechas.</summary>
    [HttpGet]
    [RequirePermission(Permisos.PedidosView)]
    [ProducesResponseType(typeof(PagedResult<OrderListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PedidosService.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] int? customerId = null,
        [FromQuery] EstadoPedido? status = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _pedidosService.GetListAsync(
            page, pageSize, search, customerId, status, dateFrom, dateTo, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>Detalle con los ítems del snapshot comercial (BR-071).</summary>
    [HttpGet("{id:int}")]
    [RequirePermission(Permisos.PedidosView)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _pedidosService.GetByIdAsync(id, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>Historial inmutable de cambios de estado (BR-075).</summary>
    [HttpGet("{id:int}/history")]
    [RequirePermission(Permisos.PedidosView)]
    [ProducesResponseType(typeof(IReadOnlyList<OrderHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory(int id, CancellationToken cancellationToken)
    {
        var result = await _pedidosService.GetHistoryAsync(id, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Edita la información de seguimiento editable: notas y entrega
    /// estimada (BR-076). Los estados finales no admiten cambios (BR-074).
    /// </summary>
    [HttpPatch("{id:int}")]
    [RequirePermission(Permisos.PedidosUpdate)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateOrderDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _pedidosService.UpdateAsync(id, dto, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Cambia el estado del pedido. El dominio valida la transición
    /// (BR-072); cada cambio registra historial (BR-075). Requiere
    /// PEDIDOS_UPDATE, o PEDIDOS_CANCEL para cancelar (BR-003).
    /// </summary>
    [HttpPost("{id:int}/status")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeStatus(
        int id,
        [FromBody] ChangeOrderStatusDto dto,
        CancellationToken cancellationToken)
    {
        // El permiso depende del estado destino concreto: una misma URL
        // admite dos permisos (mismo criterio que InventoryController).
        var permisoRequerido = dto.Status == EstadoPedido.Cancelado
            ? Permisos.PedidosCancel
            : Permisos.PedidosUpdate;

        var autorizacion = await _authorizationService.AuthorizeAsync(
            User, resource: null, new PermissionRequirement(permisoRequerido));

        if (!autorizacion.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Authorization.Denied",
                Detail = $"Se requiere el permiso {permisoRequerido} para este cambio de estado.",
                Status = StatusCodes.Status403Forbidden,
                Instance = HttpContext.Request.Path,
            });
        }

        var firebaseUid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _pedidosService.ChangeStatusAsync(
            firebaseUid ?? string.Empty, id, dto, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    private IActionResult ToHttpResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value);
        }

        var problem = new ProblemDetails
        {
            Title = result.Error.Code,
            Detail = result.Error.Description,
            Status = result.Error.Code switch
            {
                "Validation.Error" => StatusCodes.Status400BadRequest,
                "NotFound.Error" => StatusCodes.Status404NotFound,
                "Conflict.Error" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError,
            },
            Instance = HttpContext.Request.Path,
        };

        return StatusCode(problem.Status.Value, problem);
    }
}
