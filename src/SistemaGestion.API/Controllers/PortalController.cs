using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGestion.Application.Pagos.Dtos;
using SistemaGestion.Application.Pedidos.Dtos;
using SistemaGestion.Application.PortalClientes;
using SistemaGestion.Application.Presupuestos.Dtos;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.Controllers;

/// <summary>
/// Portal Cliente (roadmap Phase 20 — Client Portal + Customer Isolation,
/// functional-spec §23). Todos los endpoints operan sobre los recursos del
/// propio cliente: la identidad se deriva del Firebase ID Token, el
/// backend resuelve el cliente vinculado (BR-010) y aplica ownership
/// estricto (404 para recursos ajenos). No requieren permisos granulares:
/// el rol Cliente no los tiene (AGENTS.md §9).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/my")]
public class PortalController : ControllerBase
{
    private readonly PortalClientesService _portal;

    public PortalController(PortalClientesService portal)
    {
        _portal = portal;
    }

    /// <summary>Perfil del cliente vinculado al token.</summary>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(ClienteProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var result = await _portal.GetProfileAsync(Uid, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>Actualiza nombre, apellido y datos de contacto del perfil.</summary>
    [HttpPatch("profile")]
    [ProducesResponseType(typeof(ClienteProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateClienteProfileDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _portal.UpdateProfileAsync(Uid, dto, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>Resumen del portal: saldo, totales y contadores.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ClienteSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _portal.GetSummaryAsync(Uid, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>Listado paginado de los presupuestos propios.</summary>
    [HttpGet("quotes")]
    [ProducesResponseType(typeof(PagedResult<QuoteListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuotes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PortalClientesService.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] EstadoPresupuesto? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _portal.GetQuotesAsync(
            Uid, page, pageSize, search, status, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>Detalle de un presupuesto propio (404 si es ajeno).</summary>
    [HttpGet("quotes/{id:int}")]
    [ProducesResponseType(typeof(QuoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuote(int id, CancellationToken cancellationToken)
    {
        var result = await _portal.GetQuoteAsync(Uid, id, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Solicita un presupuesto describiendo el trabajo: crea el presupuesto
    /// en borrador con un ítem sin precio que el equipo cotiza.
    /// </summary>
    [HttpPost("quotes")]
    [ProducesResponseType(typeof(QuoteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RequestQuote(
        [FromBody] RequestQuoteDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _portal.RequestQuoteAsync(Uid, dto, cancellationToken);

        return ToHttpResult(
            result,
            quote => Created($"/api/v1/my/quotes/{quote.Id}", quote));
    }

    /// <summary>
    /// Acepta un presupuesto propio y crea el pedido en una única
    /// transacción (BR-064/BR-065).
    /// </summary>
    [HttpPost("quotes/{id:int}/accept")]
    [ProducesResponseType(typeof(QuoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AcceptQuote(int id, CancellationToken cancellationToken)
    {
        var result = await _portal.AcceptQuoteAsync(Uid, id, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>Listado paginado de los pedidos propios.</summary>
    [HttpGet("orders")]
    [ProducesResponseType(typeof(PagedResult<OrderListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PortalClientesService.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] EstadoPedido? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _portal.GetOrdersAsync(
            Uid, page, pageSize, search, status, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>Detalle de un pedido propio (404 si es ajeno).</summary>
    [HttpGet("orders/{id:int}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(int id, CancellationToken cancellationToken)
    {
        var result = await _portal.GetOrderAsync(Uid, id, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>Historial de estados de un pedido propio (seguimiento).</summary>
    [HttpGet("orders/{id:int}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderHistory(int id, CancellationToken cancellationToken)
    {
        var result = await _portal.GetOrderHistoryAsync(Uid, id, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>Pagos registrados contra un pedido propio.</summary>
    [HttpGet("orders/{id:int}/payments")]
    [ProducesResponseType(typeof(OrderPaymentsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderPayments(int id, CancellationToken cancellationToken)
    {
        var result = await _portal.GetOrderPaymentsAsync(Uid, id, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>Listado paginado de los pagos propios.</summary>
    [HttpGet("payments")]
    [ProducesResponseType(typeof(PagedResult<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PortalClientesService.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await _portal.GetPaymentsAsync(Uid, page, pageSize, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    private string? Uid => User.FindFirstValue(ClaimTypes.NameIdentifier);

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
