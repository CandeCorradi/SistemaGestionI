using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Facturacion.Abstractions;
using SistemaGestion.Application.Facturacion.Dtos;
using SistemaGestion.Application.Pedidos.Dtos;
using SistemaGestion.Domain.Common;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Facturacion;

/// <summary>
/// Casos de uso de facturación simulada (functional-spec §20,
/// BR-130/BR-131): emisión interna de una factura por pedido entregado o
/// completado, con snapshot histórico de cliente y total (BR-141),
/// numeración aislada detrás de <see cref="IBillingService"/> y listado
/// paginado global.
/// </summary>
public sealed class FacturasService
{
    /// <summary>Tamaño de página máximo permitido (AGENTS.md §30).</summary>
    public const int MaxPageSize = 100;

    public const int DefaultPageSize = 20;

    private readonly IFacturaRepository _facturas;

    private readonly IPedidoRepository _pedidos;

    private readonly IUsuarioRepository _usuarios;

    private readonly IBillingService _billing;

    private readonly ITimeProvider _clock;

    public FacturasService(
        IFacturaRepository facturas,
        IPedidoRepository pedidos,
        IUsuarioRepository usuarios,
        IBillingService billing,
        ITimeProvider clock)
    {
        _facturas = facturas;
        _pedidos = pedidos;
        _usuarios = usuarios;
        _billing = billing;
        _clock = clock;
    }

    /// <summary>
    /// Emite la factura interna de un pedido (BR-130). Sólo se permiten
    /// pedidos en estado Entregado o Completado, una factura por pedido y
    /// un único snapshot histórico por emisión; la inserción y la
    /// numeración ocurren en la misma transacción (BR-151).
    /// </summary>
    public async Task<Result<FacturaDto>> CreateAsync(
        string firebaseUid,
        CreateFacturaDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.OrderId <= 0)
        {
            return Error.Validation("El pedido indicado no es válido.");
        }

        if (string.IsNullOrWhiteSpace(firebaseUid))
        {
            return Error.NotFound("No hay usuario interno asociado al token.");
        }

        var pedido = await _pedidos.GetByIdReadOnlyAsync(dto.OrderId, cancellationToken);
        if (pedido is null)
        {
            return Error.NotFound($"El pedido {dto.OrderId} no existe.");
        }

        if (pedido.Estado is not (EstadoPedido.Entregado or EstadoPedido.Completado))
        {
            return Error.Conflict(
                "Sólo se pueden emitir facturas de pedidos entregados o " +
                "completados.");
        }

        var usuario = await _usuarios.GetByFirebaseUidAsync(firebaseUid, cancellationToken);
        if (usuario is null || !usuario.Activo)
        {
            return Error.NotFound("No hay usuario interno activo asociado al token.");
        }

        if (await _facturas.ExistsByPedidoAsync(dto.OrderId, cancellationToken))
        {
            return Error.Conflict(
                $"El pedido {dto.OrderId} ya tiene una factura emitida.");
        }

        Factura factura;
        try
        {
            factura = Factura.Crear(
                pedidoId: pedido.Id,
                clienteId: pedido.ClienteId,
                clienteNombre: OrderListDto.NombreCliente(pedido),
                total: pedido.Total,
                fechaEmisionUtc: _clock.UtcNow,
                usuarioId: usuario.Id,
                notas: dto.Notes);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(ex.Message);
        }

        try
        {
            await _facturas.ExecuteInTransactionAsync(async innerCt =>
            {
                await _facturas.AddAsync(factura, innerCt);
                factura.AsignarDocumento(
                    _billing.GenerateNumber(factura.Id), _billing.IsSimulated);
                await _facturas.SaveChangesAsync(innerCt);
            }, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Error.Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(ex.Message);
        }

        return FacturaDto.From(factura, usuario);
    }

    /// <summary>Detalle de una factura por id (api-spec §17A).</summary>
    public async Task<Result<FacturaDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Error.Validation("La factura indicada no es válida.");
        }

        var factura = await _facturas.GetByIdReadOnlyAsync(id, cancellationToken);
        if (factura is null)
        {
            return Error.NotFound($"La factura {id} no existe.");
        }

        return FacturaDto.From(factura);
    }

    /// <summary>Listado paginado global con filtros (AGENTS.md §30).</summary>
    public async Task<Result<PagedResult<FacturaDto>>> GetListAsync(
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

        var paged = await _facturas.GetPagedAsync(
            page, pageSize, orderId, customerId, dateFrom, dateTo, search, cancellationToken);

        return new PagedResult<FacturaDto>
        {
            Items = paged.Items.Select(f => FacturaDto.From(f)).ToList(),
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalItems = paged.TotalItems,
        };
    }
}
