using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Clientes;
using SistemaGestion.Application.Clientes.Dtos;
using SistemaGestion.Application.Pagos;
using SistemaGestion.Application.Pagos.Dtos;
using SistemaGestion.Application.Pedidos;
using SistemaGestion.Application.Pedidos.Dtos;
using SistemaGestion.Application.Presupuestos;
using SistemaGestion.Application.Presupuestos.Dtos;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.PortalClientes;

/// <summary>
/// Casos de uso del Portal Cliente (roadmap Phase 20 — Client Portal,
/// functional-spec §23). Cada operación se resuelve desde el Firebase UID
/// del token: primero el cliente vinculado (BR-010) y luego ownership
/// estricto sobre el recurso pedido (roadmap "Customer Isolation"). Un
/// recurso ajeno o inexistente responde 404 con el mismo mensaje, para no
/// revelar la existencia de datos de otros clientes (AGENTS.md §9).
/// </summary>
public sealed class PortalClientesService
{
    /// <summary>Tamaño de página máximo permitido (AGENTS.md §30).</summary>
    public const int MaxPageSize = 100;

    public const int DefaultPageSize = 20;

    private readonly IClienteRepository _clientes;

    private readonly ClientesService _clientesService;

    private readonly PresupuestosService _presupuestos;

    private readonly PedidosService _pedidos;

    private readonly PagosService _pagos;

    private readonly IPagoRepository _pagosRepository;

    private readonly INotificacionPublisher _notificaciones;

    public PortalClientesService(
        IClienteRepository clientes,
        ClientesService clientesService,
        PresupuestosService presupuestos,
        PedidosService pedidos,
        PagosService pagos,
        IPagoRepository pagosRepository,
        INotificacionPublisher notificaciones)
    {
        _clientes = clientes;
        _clientesService = clientesService;
        _presupuestos = presupuestos;
        _pedidos = pedidos;
        _pagos = pagos;
        _pagosRepository = pagosRepository;
        _notificaciones = notificaciones;
    }

    /// <summary>Perfil del cliente vinculado al token (BR-010).</summary>
    public async Task<Result<ClienteProfileDto>> GetProfileAsync(
        string? firebaseUid,
        CancellationToken cancellationToken = default)
    {
        var cliente = await GetClienteAsync(firebaseUid, cancellationToken);
        if (cliente.IsFailure)
        {
            return cliente.Error;
        }

        return ToProfileDto(cliente.Value);
    }

    /// <summary>
    /// Actualiza el perfil desde el portal: nombre, apellido y datos de
    /// contacto. El email no se modifica aquí (es la identidad de la
    /// cuenta de autenticación) ni el estado activo (lo define el equipo).
    /// </summary>
    public async Task<Result<ClienteProfileDto>> UpdateProfileAsync(
        string? firebaseUid,
        UpdateClienteProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        var cliente = await GetClienteAsync(firebaseUid, cancellationToken);
        if (cliente.IsFailure)
        {
            return cliente.Error;
        }

        var actualizado = await _clientesService.UpdateAsync(
            cliente.Value.Id,
            new UpdateClienteDto(
                Nombre: dto.Nombre,
                Apellido: dto.Apellido,
                Email: null,
                Telefono: dto.Telefono,
                Direccion: dto.Direccion,
                Documento: dto.Documento),
            cancellationToken);

        if (actualizado.IsFailure)
        {
            return actualizado.Error;
        }

        return ToProfileDto(actualizado.Value);
    }

    /// <summary>Resumen del portal: saldo, pagado, pedidos y presupuestos pendientes.</summary>
    public async Task<Result<ClienteSummaryDto>> GetSummaryAsync(
        string? firebaseUid,
        CancellationToken cancellationToken = default)
    {
        var cliente = await GetClienteAsync(firebaseUid, cancellationToken);
        if (cliente.IsFailure)
        {
            return cliente.Error;
        }

        var resumen = await _pagosRepository.GetResumenClienteAsync(
            cliente.Value.Id, cancellationToken);

        var pedidos = await _pedidos.GetListAsync(
            page: 1, pageSize: 1, search: null, customerId: cliente.Value.Id,
            status: null, dateFrom: null, dateTo: null, cancellationToken);
        if (pedidos.IsFailure)
        {
            return pedidos.Error;
        }

        var pendientes = await _presupuestos.GetListAsync(
            page: 1, pageSize: 1, search: null, customerId: cliente.Value.Id,
            status: EstadoPresupuesto.Enviado, dateFrom: null, dateTo: null,
            cancellationToken);
        if (pendientes.IsFailure)
        {
            return pendientes.Error;
        }

        return new ClienteSummaryDto(
            TotalPedidos: resumen.TotalPedidos,
            TotalPagado: resumen.TotalPagado,
            Saldo: resumen.TotalPedidos - resumen.TotalPagado,
            PedidosCount: pedidos.Value.TotalItems,
            PresupuestosPendientes: pendientes.Value.TotalItems);
    }

    /// <summary>Listado paginado de los presupuestos del propio cliente.</summary>
    public async Task<Result<PagedResult<QuoteListDto>>> GetQuotesAsync(
        string? firebaseUid,
        int page,
        int pageSize,
        string? search,
        EstadoPresupuesto? status,
        CancellationToken cancellationToken = default)
    {
        var cliente = await GetClienteAsync(firebaseUid, cancellationToken);
        if (cliente.IsFailure)
        {
            return cliente.Error;
        }

        return await _presupuestos.GetListAsync(
            page, pageSize, search, cliente.Value.Id, status,
            dateFrom: null, dateTo: null, cancellationToken);
    }

    /// <summary>Detalle de un presupuesto propio (404 si es ajeno).</summary>
    public async Task<Result<QuoteDto>> GetQuoteAsync(
        string? firebaseUid,
        int id,
        CancellationToken cancellationToken = default)
    {
        var cliente = await GetClienteAsync(firebaseUid, cancellationToken);
        if (cliente.IsFailure)
        {
            return cliente.Error;
        }

        var quote = await _presupuestos.GetByIdAsync(id, cancellationToken);
        if (quote.IsFailure)
        {
            return quote.Error;
        }

        if (quote.Value.CustomerId != cliente.Value.Id)
        {
            return QuoteNotFound(id);
        }

        return quote.Value;
    }

    /// <summary>
    /// Solicitud de presupuesto desde el portal (functional-spec §10/§12):
    /// crea el presupuesto en Borrador con un ítem descriptivo sin precio
    /// (0) que el equipo cotiza y envía. No envía nada al cliente todavía.
    /// </summary>
    public async Task<Result<QuoteDto>> RequestQuoteAsync(
        string? firebaseUid,
        RequestQuoteDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Description))
        {
            return Error.Validation("La descripción del trabajo es obligatoria.");
        }

        if (dto.Cantidad <= 0)
        {
            return Error.Validation("La cantidad debe ser mayor que cero.");
        }

        var cliente = await GetClienteAsync(firebaseUid, cancellationToken);
        if (cliente.IsFailure)
        {
            return cliente.Error;
        }

        var creado = await _presupuestos.CreateAsync(
            new CreateQuoteDto(
                CustomerId: cliente.Value.Id,
                ExpirationDate: null,
                Notes: null,
                Conditions: null,
                Items:
                [
                    new CreateQuoteItemDto(
                        ProductId: null,
                        Description: dto.Description.Trim(),
                        Quantity: dto.Cantidad,
                        UnitPrice: 0m,
                        Discount: null)
                ]),
            cancellationToken);
        if (creado.IsFailure)
        {
            return creado.Error;
        }

        await _notificaciones.PublicarAsync(
            new NotificacionDraft(
                Tipo: TipoNotificacion.PresupuestoSolicitado,
                Titulo: "Solicitud de presupuesto",
                Mensaje:
                    $"El cliente {cliente.Value.Nombre} {cliente.Value.Apellido} "
                    + $"solicitó un presupuesto (#{creado.Value.Id}).",
                ParaEquipo: true,
                EntidadTipo: "Presupuesto",
                EntidadId: creado.Value.Id),
            cancellationToken);

        return creado.Value;
    }

    /// <summary>
    /// Acepta un presupuesto Enviado propio; reutiliza la aceptación
    /// atómica con creación de pedido (BR-064/BR-065).
    /// </summary>
    public async Task<Result<QuoteDto>> AcceptQuoteAsync(
        string? firebaseUid,
        int id,
        CancellationToken cancellationToken = default)
    {
        var cliente = await GetClienteAsync(firebaseUid, cancellationToken);
        if (cliente.IsFailure)
        {
            return cliente.Error;
        }

        var quote = await _presupuestos.GetByIdAsync(id, cancellationToken);
        if (quote.IsFailure)
        {
            return quote.Error;
        }

        if (quote.Value.CustomerId != cliente.Value.Id)
        {
            return QuoteNotFound(id);
        }

        return await _presupuestos.AcceptAsync(id, cancellationToken);
    }

    /// <summary>Listado paginado de los pedidos del propio cliente.</summary>
    public async Task<Result<PagedResult<OrderListDto>>> GetOrdersAsync(
        string? firebaseUid,
        int page,
        int pageSize,
        string? search,
        EstadoPedido? status,
        CancellationToken cancellationToken = default)
    {
        var cliente = await GetClienteAsync(firebaseUid, cancellationToken);
        if (cliente.IsFailure)
        {
            return cliente.Error;
        }

        return await _pedidos.GetListAsync(
            page, pageSize, search, cliente.Value.Id, status,
            dateFrom: null, dateTo: null, cancellationToken);
    }

    /// <summary>Detalle de un pedido propio (404 si es ajeno).</summary>
    public async Task<Result<OrderDto>> GetOrderAsync(
        string? firebaseUid,
        int id,
        CancellationToken cancellationToken = default)
    {
        var cliente = await GetClienteAsync(firebaseUid, cancellationToken);
        if (cliente.IsFailure)
        {
            return cliente.Error;
        }

        var order = await _pedidos.GetByIdAsync(id, cancellationToken);
        if (order.IsFailure)
        {
            return order.Error;
        }

        if (order.Value.CustomerId != cliente.Value.Id)
        {
            return OrderNotFound(id);
        }

        return order.Value;
    }

    /// <summary>Historial de estados de un pedido propio (seguimiento).</summary>
    public async Task<Result<IReadOnlyList<OrderHistoryDto>>> GetOrderHistoryAsync(
        string? firebaseUid,
        int id,
        CancellationToken cancellationToken = default)
    {
        var propio = await GetOrderAsync(firebaseUid, id, cancellationToken);
        if (propio.IsFailure)
        {
            return propio.Error;
        }

        return await _pedidos.GetHistoryAsync(id, cancellationToken);
    }

    /// <summary>Pagos registrados contra un pedido propio.</summary>
    public async Task<Result<OrderPaymentsDto>> GetOrderPaymentsAsync(
        string? firebaseUid,
        int id,
        CancellationToken cancellationToken = default)
    {
        var propio = await GetOrderAsync(firebaseUid, id, cancellationToken);
        if (propio.IsFailure)
        {
            return propio.Error;
        }

        return await _pagos.GetOrderPaymentsAsync(id, cancellationToken);
    }

    /// <summary>Listado paginado de los pagos del propio cliente.</summary>
    public async Task<Result<PagedResult<PaymentDto>>> GetPaymentsAsync(
        string? firebaseUid,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var cliente = await GetClienteAsync(firebaseUid, cancellationToken);
        if (cliente.IsFailure)
        {
            return cliente.Error;
        }

        return await _pagos.GetListAsync(
            page, pageSize, orderId: null, customerId: cliente.Value.Id,
            dateFrom: null, dateTo: null, search: null, cancellationToken);
    }

    /// <summary>
    /// Resuelve el cliente activo vinculado al Firebase UID. Sin vínculo
    /// (BR-010) responde 404: la cuenta existe pero no tiene portal.
    /// </summary>
    private async Task<Result<Cliente>> GetClienteAsync(
        string? firebaseUid,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(firebaseUid))
        {
            return ClientNotFound();
        }

        var cliente = await _clientes.GetByFirebaseUidAsync(firebaseUid, cancellationToken);
        if (cliente is null)
        {
            return ClientNotFound();
        }

        return cliente;
    }

    private static Error ClientNotFound()
        => Error.NotFound("No hay un cliente vinculado a esta cuenta.");

    private static Error QuoteNotFound(int id)
        => Error.NotFound($"El presupuesto {id} no existe.");

    private static Error OrderNotFound(int id)
        => Error.NotFound($"El pedido {id} no existe.");

    private static ClienteProfileDto ToProfileDto(Cliente cliente) => new(
        cliente.Id,
        cliente.Email,
        cliente.Nombre,
        cliente.Apellido,
        cliente.Telefono,
        cliente.Direccion,
        cliente.Documento,
        cliente.FechaCreacion);

    private static ClienteProfileDto ToProfileDto(ClienteDto cliente) => new(
        cliente.Id,
        cliente.Email,
        cliente.Nombre,
        cliente.Apellido,
        cliente.Telefono,
        cliente.Direccion,
        cliente.Documento,
        cliente.FechaCreacion);
}
