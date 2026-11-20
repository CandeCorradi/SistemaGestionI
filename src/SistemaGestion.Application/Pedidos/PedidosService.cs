using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Pedidos.Dtos;
using SistemaGestion.Domain.Common;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Pedidos;

/// <summary>
/// Casos de uso de pedidos (functional-spec §15/§16, BR-070..BR-076):
/// consultas con filtros, detalle con el snapshot del presupuesto aceptado,
/// cambio de estado validado por el dominio (BR-072) con historial
/// inmutable (BR-075), edición de la información de seguimiento (BR-076) y
/// los pedidos se crean aceptando un presupuesto (BR-065).
/// </summary>
public sealed class PedidosService
{
    /// <summary>Tamaño de página máximo permitido (AGENTS.md §30).</summary>
    public const int MaxPageSize = 100;

    public const int DefaultPageSize = 20;

    private readonly IPedidoRepository _pedidos;

    private readonly IUsuarioRepository _usuarios;

    private readonly ITimeProvider _clock;

    private readonly INotificacionPublisher _notificaciones;

    public PedidosService(
        IPedidoRepository pedidos,
        IUsuarioRepository usuarios,
        ITimeProvider clock,
        INotificacionPublisher notificaciones)
    {
        _pedidos = pedidos;
        _usuarios = usuarios;
        _clock = clock;
        _notificaciones = notificaciones;
    }

    /// <summary>Listado paginado con filtros de cliente, estado y fechas.</summary>
    public async Task<Result<PagedResult<OrderListDto>>> GetListAsync(
        int page,
        int pageSize,
        string? search,
        int? customerId,
        EstadoPedido? status,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        if (customerId is <= 0)
        {
            return Error.Validation("El cliente indicado no es válido.");
        }

        if (dateFrom.HasValue && dateTo.HasValue && dateFrom.Value.Date > dateTo.Value.Date)
        {
            return Error.Validation("El rango de fechas no es válido: el inicio no puede ser posterior al fin.");
        }

        var paged = await _pedidos.GetPagedAsync(
            page, pageSize, search, customerId, status, dateFrom, dateTo, cancellationToken);

        return new PagedResult<OrderListDto>
        {
            Items = paged.Items.Select(OrderListDto.From).ToList(),
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalItems = paged.TotalItems,
        };
    }

    /// <summary>Detalle del pedido con los ítems del snapshot (BR-071).</summary>
    public async Task<Result<OrderDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Error.Validation("El pedido indicado no es válido.");
        }

        var pedido = await _pedidos.GetByIdReadOnlyAsync(id, cancellationToken);
        if (pedido is null)
        {
            return Error.NotFound($"El pedido {id} no existe.");
        }

        return OrderDto.From(pedido);
    }

    /// <summary>Historial inmutable de cambios de estado (BR-075).</summary>
    public async Task<Result<IReadOnlyList<OrderHistoryDto>>> GetHistoryAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Error.Validation("El pedido indicado no es válido.");
        }

        var existe = await _pedidos.GetByIdReadOnlyAsync(id, cancellationToken);
        if (existe is null)
        {
            return Error.NotFound($"El pedido {id} no existe.");
        }

        var historial = await _pedidos.GetHistorialAsync(id, cancellationToken);
        return historial.Select(OrderHistoryDto.From).ToList();
    }

    /// <summary>
    /// Cambia el estado del pedido en nombre del usuario interno
    /// autenticado. El dominio valida la transición (BR-072) y cada cambio
    /// registra su fila de historial inmutable (BR-075) en un único commit.
    /// </summary>
    public async Task<Result<OrderDto>> ChangeStatusAsync(
        string firebaseUid,
        int id,
        ChangeOrderStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Error.Validation("El pedido indicado no es válido.");
        }

        if (!Enum.IsDefined(dto.Status))
        {
            return Error.Validation($"El estado '{(int)dto.Status}' no es un estado de pedido válido.");
        }

        var pedido = await _pedidos.GetByIdAsync(id, cancellationToken);
        if (pedido is null)
        {
            return Error.NotFound($"El pedido {id} no existe.");
        }

        if (string.IsNullOrWhiteSpace(firebaseUid))
        {
            return Error.NotFound("No hay usuario interno asociado al token.");
        }

        var usuario = await _usuarios.GetByFirebaseUidAsync(firebaseUid, cancellationToken);
        if (usuario is null || !usuario.Activo)
        {
            return Error.NotFound("No hay usuario interno activo asociado al token.");
        }

        var estadoAnterior = pedido.Estado;

        try
        {
            pedido.TransicionarA(dto.Status, _clock.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Error.Conflict(ex.Message);
        }

        var historial = PedidoHistorial.Crear(
            pedidoId: pedido.Id,
            estadoAnterior: estadoAnterior,
            estadoNuevo: pedido.Estado,
            fechaUtc: _clock.UtcNow,
            usuarioId: usuario.Id,
            comentario: dto.Comment);

        await _pedidos.UpdateWithHistoryAsync(pedido, historial, cancellationToken);

        var esListo = pedido.Estado == EstadoPedido.Listo;
        await _notificaciones.PublicarAsync(
            new NotificacionDraft(
                Tipo: esListo ? TipoNotificacion.PedidoListo : TipoNotificacion.PedidoEstadoCambiado,
                Titulo: esListo ? "Pedido listo" : "Pedido actualizado",
                Mensaje: esListo
                    ? $"El pedido #{pedido.Id} está listo para entrega."
                    : $"El pedido #{pedido.Id} cambió al estado {EtiquetaEstado(pedido.Estado)}.",
                ClienteId: pedido.ClienteId,
                EntidadTipo: "Pedido",
                EntidadId: pedido.Id),
            cancellationToken);

        return OrderDto.From(pedido);
    }

    private static string EtiquetaEstado(EstadoPedido estado) => estado switch
    {
        EstadoPedido.Pendiente => "Pendiente",
        EstadoPedido.Confirmado => "Confirmado",
        EstadoPedido.EnProduccion => "En producción",
        EstadoPedido.Listo => "Listo",
        EstadoPedido.Entregado => "Entregado",
        EstadoPedido.Cancelado => "Cancelado",
        EstadoPedido.Completado => "Completado",
        _ => ((int)estado).ToString(),
    };

    /// <summary>
    /// Edita la información de seguimiento editable del pedido: notas y
    /// entrega estimada (functional-spec §15, BR-076). Los estados finales
    /// no admiten cambios (BR-074).
    /// </summary>
    public async Task<Result<OrderDto>> UpdateAsync(
        int id,
        UpdateOrderDto dto,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Error.Validation("El pedido indicado no es válido.");
        }

        var pedido = await _pedidos.GetByIdAsync(id, cancellationToken);
        if (pedido is null)
        {
            return Error.NotFound($"El pedido {id} no existe.");
        }

        var fechaEstimada = NormalizarFecha(dto.EstimatedDeliveryDate);

        try
        {
            pedido.ActualizarInformacion(dto.Notes, fechaEstimada);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Error.Conflict(ex.Message);
        }

        await _pedidos.UpdateAsync(pedido, cancellationToken);

        return OrderDto.From(pedido);
    }

    /// <summary>
    /// Normaliza la fecha de entrega estimada a UTC: los date-only del
    /// frontend llegan como Unspecified y se interpretan como UTC
    /// (AGENTS.md §17).
    /// </summary>
    private static DateTime? NormalizarFecha(DateTime? fecha)
    {
        if (!fecha.HasValue || fecha.Value.Kind == DateTimeKind.Utc)
        {
            return fecha;
        }

        return fecha.Value.Kind == DateTimeKind.Local
            ? fecha.Value.ToUniversalTime()
            : DateTime.SpecifyKind(fecha.Value, DateTimeKind.Utc);
    }
}
