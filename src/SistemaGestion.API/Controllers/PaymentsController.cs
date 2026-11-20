using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaGestion.API.Authorization;
using SistemaGestion.Application.Pagos;
using SistemaGestion.Application.Pagos.Dtos;
using SistemaGestion.Domain.Authorization;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.API.Controllers;

/// <summary>
/// Pagos (api-spec §17): registro con validación de saldo (BR-080/081),
/// historial inmutable (BR-082) y saldo calculado por backend (BR-083).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1")]
public class PaymentsController : ControllerBase
{
    private readonly PagosService _pagosService;

    public PaymentsController(PagosService pagosService)
    {
        _pagosService = pagosService;
    }

    /// <summary>Listado paginado global de pagos con filtros.</summary>
    [HttpGet("payments")]
    [RequirePermission(Permisos.PagosView)]
    [ProducesResponseType(typeof(PagedResult<PaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PagosService.DefaultPageSize,
        [FromQuery] int? orderId = null,
        [FromQuery] int? customerId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _pagosService.GetListAsync(
            page, pageSize, orderId, customerId, dateFrom, dateTo, search, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Pagos de un pedido con total pagado y saldo pendiente calculados
    /// por el backend (BR-083).
    /// </summary>
    [HttpGet("orders/{orderId:int}/payments")]
    [RequirePermission(Permisos.PagosView)]
    [ProducesResponseType(typeof(OrderPaymentsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderPayments(
        int orderId,
        CancellationToken cancellationToken)
    {
        var result = await _pagosService.GetOrderPaymentsAsync(orderId, cancellationToken);

        return ToHttpResult(result, Ok);
    }

    /// <summary>
    /// Registra un pago contra un pedido: monto positivo (BR-080) que no
    /// supera el saldo pendiente (BR-081); historial inmutable (BR-082).
    /// </summary>
    [HttpPost("orders/{orderId:int}/payments")]
    [RequirePermission(Permisos.PagosCreate)]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        int orderId,
        [FromBody] RegisterPaymentDto dto,
        CancellationToken cancellationToken)
    {
        var firebaseUid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _pagosService.RegisterAsync(
            firebaseUid ?? string.Empty, orderId, dto, cancellationToken);

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
