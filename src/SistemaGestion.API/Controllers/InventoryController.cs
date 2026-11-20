using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGestion.API.Authorization;
using SistemaGestion.Application.Inventario;
using SistemaGestion.Application.Inventario.Dtos;
using SistemaGestion.Domain.Authorization;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.Controllers;

/// <summary>
/// Inventario (api-spec §14, functional-spec §11): stock actual, detalle
/// por producto, historial de movimientos (BR-051) y registro de
/// movimientos. El permiso del POST depende del tipo de movimiento:
/// STOCK_ENTRY para entradas (Ingreso/Devolución) y STOCK_ADJUST para el
/// resto. Las operaciones son atómicas y sin stock negativo (BR-050/052/053).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/inventory")]
public class InventoryController : ControllerBase
{
    private readonly InventarioService _inventarioService;

    private readonly IAuthorizationService _authorizationService;

    public InventoryController(
        InventarioService inventarioService,
        IAuthorizationService authorizationService)
    {
        _inventarioService = inventarioService;
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Lista el stock actual de productos activos con búsqueda y filtro
    /// de stock bajo (sólo campos de inventario).
    /// </summary>
    [HttpGet]
    [RequirePermission(Permisos.StockView)]
    [ProducesResponseType(typeof(PagedResult<StockListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = InventarioService.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] bool? lowStock = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _inventarioService.GetStockListAsync(
            page, pageSize, search, lowStock, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Devuelve el stock de un producto concreto.
    /// </summary>
    [HttpGet("products/{productId:int}")]
    [RequirePermission(Permisos.StockView)]
    [ProducesResponseType(typeof(StockDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductStock(int productId, CancellationToken cancellationToken)
    {
        var result = await _inventarioService.GetProductStockAsync(productId, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Historial paginado de movimientos (más recientes primero), filtrable
    /// por producto. Requiere STOCK_VIEW.
    /// </summary>
    [HttpGet("movements")]
    [RequirePermission(Permisos.StockView)]
    [ProducesResponseType(typeof(PagedResult<MovimientoStockDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMovements(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = InventarioService.DefaultPageSize,
        [FromQuery] int? productId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _inventarioService.GetMovementsAsync(
            page, pageSize, productId, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Registra un movimiento de stock. El permiso depende del tipo:
    /// STOCK_ENTRY (Ingreso/Devolución) o STOCK_ADJUST (Egreso/Ajuste/
    /// Reserva/Liberación). Devuelve 409 si el stock cambió de forma
    /// concurrente (BR-053); nunca permite stock negativo (BR-052).
    /// </summary>
    [HttpPost("movements")]
    [ProducesResponseType(typeof(MovimientoStockDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateMovement(
        [FromBody] CreateMovimientoStockDto dto,
        CancellationToken cancellationToken)
    {
        // El permiso requiere evaluar el tipo concreto del movimiento:
        // se decide aquí porque una misma URL admite dos permisos (BR-003).
        var permisoRequerido = RequiredPermission(dto.Type);
        var autorizacion = await _authorizationService.AuthorizeAsync(
            User, resource: null, new PermissionRequirement(permisoRequerido));

        if (!autorizacion.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Authorization.Denied",
                Detail = $"Se requiere el permiso {permisoRequerido} "
                    + $"para registrar movimientos de tipo '{dto.Type}'.",
                Status = StatusCodes.Status403Forbidden,
                Instance = HttpContext.Request.Path,
            });
        }

        var firebaseUid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _inventarioService.RegisterMovementAsync(
            firebaseUid ?? string.Empty, dto, cancellationToken);

        return ToHttpResult(
            result,
            movimiento => Created($"/api/v1/inventory/movements", movimiento));
    }

    /// <summary>Permiso granular requerido según el tipo de movimiento.</summary>
    internal static string RequiredPermission(TipoMovimientoStock tipo) => tipo switch
    {
        TipoMovimientoStock.Ingreso or TipoMovimientoStock.Devolucion => Permisos.StockEntry,
        _ => Permisos.StockAdjust,
    };

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
