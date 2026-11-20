using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGestion.API.Authorization;
using SistemaGestion.Application.Presupuestos;
using SistemaGestion.Application.Presupuestos.Dtos;
using SistemaGestion.Domain.Authorization;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.Controllers;

/// <summary>
/// Presupuestos (api-spec §15, functional-spec §12): listado con filtros,
/// detalle, creación con snapshot comercial (BR-063), edición sólo en
/// borrador y transiciones enviar/rechazar/anular. Los totales se calculan
/// en backend (BR-062). La aceptación crea el pedido de forma atómica
/// (BR-064/BR-065) en el mismo commit.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/quotes")]
public class QuotesController : ControllerBase
{
    private readonly PresupuestosService _presupuestosService;

    public QuotesController(PresupuestosService presupuestosService)
    {
        _presupuestosService = presupuestosService;
    }

    /// <summary>Listado paginado con filtros de cliente, estado y fechas.</summary>
    [HttpGet]
    [RequirePermission(Permisos.PresupuestosView)]
    [ProducesResponseType(typeof(PagedResult<QuoteListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PresupuestosService.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] int? customerId = null,
        [FromQuery] EstadoPresupuesto? status = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _presupuestosService.GetListAsync(
            page, pageSize, search, customerId, status, dateFrom, dateTo, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>Detalle con las líneas del presupuesto.</summary>
    [HttpGet("{id:int}")]
    [RequirePermission(Permisos.PresupuestosView)]
    [ProducesResponseType(typeof(QuoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _presupuestosService.GetByIdAsync(id, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Crea un presupuesto en borrador. Las líneas con producto congelan el
    /// precio vigente (BR-063); los totales los calcula el backend (BR-062).
    /// </summary>
    [HttpPost]
    [RequirePermission(Permisos.PresupuestosCreate)]
    [ProducesResponseType(typeof(QuoteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromBody] CreateQuoteDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _presupuestosService.CreateAsync(dto, cancellationToken);

        return ToHttpResult(
            result,
            quote => Created($"/api/v1/quotes/{quote.Id}", quote));
    }

    /// <summary>Edita un presupuesto; sólo permitido en estado borrador.</summary>
    [HttpPatch("{id:int}")]
    [RequirePermission(Permisos.PresupuestosUpdate)]
    [ProducesResponseType(typeof(QuoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateQuoteDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _presupuestosService.UpdateAsync(id, dto, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>Borrador → Enviado (BR-061: exige ítems).</summary>
    [HttpPost("{id:int}/send")]
    [RequirePermission(Permisos.PresupuestosUpdate)]
    [ProducesResponseType(typeof(QuoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Send(int id, CancellationToken cancellationToken)
    {
        var result = await _presupuestosService.SendAsync(id, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>Enviado → Rechazado (motivo opcional).</summary>
    [HttpPost("{id:int}/reject")]
    [RequirePermission(Permisos.PresupuestosUpdate)]
    [ProducesResponseType(typeof(QuoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(
        int id,
        [FromBody] QuoteStateChangeDto? dto,
        CancellationToken cancellationToken)
    {
        var result = await _presupuestosService.RejectAsync(id, dto?.Reason, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>Borrador/Enviado → Anulado (motivo opcional).</summary>
    [HttpPost("{id:int}/cancel")]
    [RequirePermission(Permisos.PresupuestosAnulate)]
    [ProducesResponseType(typeof(QuoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        int id,
        [FromBody] QuoteStateChangeDto? dto,
        CancellationToken cancellationToken)
    {
        var result = await _presupuestosService.CancelAsync(id, dto?.Reason, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Acepta el presupuesto y crea su pedido en una única transacción
    /// (BR-064/BR-065). Sólo válido desde Enviado; devuelve Conflict si
    /// otro proceso lo aceptó primero o si ya no es aceptable.
    /// </summary>
    [HttpPost("{id:int}/accept")]
    [RequirePermission(Permisos.PresupuestosAccept)]
    [ProducesResponseType(typeof(QuoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Accept(int id, CancellationToken cancellationToken)
    {
        var result = await _presupuestosService.AcceptAsync(id, cancellationToken);

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
