using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGestion.API.Authorization;
using SistemaGestion.Application.Facturacion;
using SistemaGestion.Application.Facturacion.Dtos;
using SistemaGestion.Domain.Authorization;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.Controllers;

/// <summary>
/// Facturación simulada (api-spec §17A): emisión interna por pedido
/// entregado o completado (BR-130), snapshot histórico (BR-141) y
/// numeración aislada detrás de IBillingService (BR-131).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1")]
public class InvoicesController : ControllerBase
{
    private readonly FacturasService _facturasService;

    public InvoicesController(FacturasService facturasService)
    {
        _facturasService = facturasService;
    }

    /// <summary>Listado paginado global de facturas con filtros.</summary>
    [HttpGet("facturas")]
    [RequirePermission(Permisos.FacturasView)]
    [ProducesResponseType(typeof(PagedResult<FacturaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = FacturasService.DefaultPageSize,
        [FromQuery] int? orderId = null,
        [FromQuery] int? customerId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _facturasService.GetListAsync(
            page, pageSize, orderId, customerId, dateFrom, dateTo, search, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>Detalle de una factura por id.</summary>
    [HttpGet("facturas/{id:int}")]
    [RequirePermission(Permisos.FacturasView)]
    [ProducesResponseType(typeof(FacturaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _facturasService.GetByIdAsync(id, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Emite la factura interna de un pedido entregado o completado: una
    /// factura por pedido, snapshot histórico y numeración simulada
    /// (BR-130).
    /// </summary>
    [HttpPost("facturas")]
    [RequirePermission(Permisos.FacturasCreate)]
    [ProducesResponseType(typeof(FacturaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateFacturaDto dto,
        CancellationToken cancellationToken)
    {
        var firebaseUid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _facturasService.CreateAsync(
            firebaseUid ?? string.Empty, dto, cancellationToken);

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
