using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Pagos.Dtos;
using SistemaGestion.Domain.Common;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Pagos;

/// <summary>
/// Casos de uso de pagos (functional-spec §17, BR-080..BR-083): registro
/// con validación de saldo contra la base dentro de una transacción
/// serializable (AGENTS.md §56), historial inmutable (BR-082), listado
/// global paginado y resumen de saldo por pedido calculado por backend
/// (BR-083).
/// </summary>
public sealed class PagosService
{
    /// <summary>Tamaño de página máximo permitido (AGENTS.md §30).</summary>
    public const int MaxPageSize = 100;

    public const int DefaultPageSize = 20;

    private readonly IPagoRepository _pagos;

    private readonly IPedidoRepository _pedidos;

    private readonly IUsuarioRepository _usuarios;

    private readonly ITimeProvider _clock;

    private readonly INotificacionPublisher _notificaciones;

    public PagosService(
        IPagoRepository pagos,
        IPedidoRepository pedidos,
        IUsuarioRepository usuarios,
        ITimeProvider clock,
        INotificacionPublisher notificaciones)
    {
        _pagos = pagos;
        _pedidos = pedidos;
        _usuarios = usuarios;
        _clock = clock;
        _notificaciones = notificaciones;
    }

    /// <summary>
    /// Registra un pago del usuario interno autenticado contra un pedido
    /// (BR-080/BR-081). La validación de saldo se hace dentro de una
    /// transacción serializable junto con la inserción para que dos pagos
    /// concurrentes no puedan dejar el saldo en negativo (AGENTS.md §56).
    /// </summary>
    public async Task<Result<PaymentDto>> RegisterAsync(
        string firebaseUid,
        int orderId,
        RegisterPaymentDto dto,
        CancellationToken cancellationToken = default)
    {
        if (orderId <= 0)
        {
            return Error.Validation("El pedido indicado no es válido.");
        }

        if (!Enum.IsDefined(dto.PaymentMethod))
        {
            return Error.Validation(
                $"El método de pago '{(int)dto.PaymentMethod}' no es un método válido.");
        }

        if (string.IsNullOrWhiteSpace(firebaseUid))
        {
            return Error.NotFound("No hay usuario interno asociado al token.");
        }

        var pedido = await _pedidos.GetByIdReadOnlyAsync(orderId, cancellationToken);
        if (pedido is null)
        {
            return Error.NotFound($"El pedido {orderId} no existe.");
        }

        if (pedido.Estado == EstadoPedido.Cancelado)
        {
            return Error.Conflict(
                "No se pueden registrar pagos sobre pedidos cancelados.");
        }

        var usuario = await _usuarios.GetByFirebaseUidAsync(firebaseUid, cancellationToken);
        if (usuario is null || !usuario.Activo)
        {
            return Error.NotFound("No hay usuario interno activo asociado al token.");
        }

        Pago pago;
        try
        {
            pago = Pago.Crear(
                pedidoId: orderId,
                monto: dto.Amount,
                fechaUtc: _clock.UtcNow,
                metodo: dto.PaymentMethod,
                referencia: dto.Reference,
                notas: dto.Notes,
                usuarioId: usuario.Id);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(ex.Message);
        }

        try
        {
            await _pagos.ExecuteSerializableAsync(async innerCt =>
            {
                var totalPagado = await _pagos.GetTotalPagadoAsync(orderId, innerCt);
                var saldo = pedido.Total - totalPagado;

                if (pago.Monto > saldo)
                {
                    throw new InvalidOperationException(
                        $"El pago excede el saldo pendiente del pedido: " +
                        $"saldo {saldo:0.00}, pago {pago.Monto:0.00} (BR-081).");
                }

                await _pagos.AddAsync(pago, innerCt);
            }, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Error.Conflict(ex.Message);
        }

        await _notificaciones.PublicarAsync(
            new NotificacionDraft(
                Tipo: TipoNotificacion.PagoRegistrado,
                Titulo: "Pago registrado",
                Mensaje: $"Se registró un pago de {pago.Monto:0.00} sobre el pedido #{orderId}.",
                ParaEquipo: true,
                EntidadTipo: "Pedido",
                EntidadId: orderId),
            cancellationToken);

        return PaymentDto.From(pago, pedido, usuario);
    }

    /// <summary>
    /// Pagos de un pedido con total pagado y saldo pendiente calculados
    /// por el backend (BR-083).
    /// </summary>
    public async Task<Result<OrderPaymentsDto>> GetOrderPaymentsAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        if (orderId <= 0)
        {
            return Error.Validation("El pedido indicado no es válido.");
        }

        var pedido = await _pedidos.GetByIdReadOnlyAsync(orderId, cancellationToken);
        if (pedido is null)
        {
            return Error.NotFound($"El pedido {orderId} no existe.");
        }

        var pagos = await _pagos.GetByPedidoAsync(orderId, cancellationToken);
        var totalPagado = await _pagos.GetTotalPagadoAsync(orderId, cancellationToken);

        return new OrderPaymentsDto
        {
            Items = pagos.Select(p => PaymentDto.From(p, pedido)).ToList(),
            TotalPaid = totalPagado,
            Balance = pedido.Total - totalPagado,
        };
    }

    /// <summary>Listado paginado global con filtros (AGENTS.md §30).</summary>
    public async Task<Result<PagedResult<PaymentDto>>> GetListAsync(
        int page,
        int pageSize,
        int? orderId,
        int? customerId,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? search,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        if (orderId is <= 0)
        {
            return Error.Validation("El pedido indicado no es válido.");
        }

        if (customerId is <= 0)
        {
            return Error.Validation("El cliente indicado no es válido.");
        }

        if (dateFrom.HasValue && dateTo.HasValue && dateFrom.Value.Date > dateTo.Value.Date)
        {
            return Error.Validation(
                "El rango de fechas no es válido: el inicio no puede ser posterior al fin.");
        }

        var paged = await _pagos.GetPagedAsync(
            page, pageSize, orderId, customerId, dateFrom, dateTo, search, cancellationToken);

        return new PagedResult<PaymentDto>
        {
            Items = paged.Items.Select(p => PaymentDto.From(p)).ToList(),
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalItems = paged.TotalItems,
        };
    }
}
