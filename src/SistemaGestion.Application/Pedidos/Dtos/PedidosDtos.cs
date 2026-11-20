using SistemaGestion.Application.Presupuestos.Dtos;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;

namespace SistemaGestion.Application.Pedidos.Dtos;

/// <summary>Fila del listado de pedidos (GET /api/v1/orders).</summary>
public sealed class OrderListDto
{
    public int Id { get; init; }

    public int CustomerId { get; init; }

    public string CustomerName { get; init; } = string.Empty;

    /// <summary>Presupuesto de origen cuando existe (BR-070).</summary>
    public int? QuoteId { get; init; }

    public DateTime CreationDate { get; init; }

    public EstadoPedido Status { get; init; }

    public decimal Total { get; init; }

    /// <summary>Entrega estimada para planificación (BR-076).</summary>
    public DateTime? EstimatedDeliveryDate { get; init; }

    public static OrderListDto From(Pedido p)
        => new()
        {
            Id = p.Id,
            CustomerId = p.ClienteId,
            CustomerName = NombreCliente(p),
            QuoteId = p.PresupuestoId > 0 ? p.PresupuestoId : null,
            CreationDate = p.FechaCreacion,
            Status = p.Estado,
            Total = p.Total,
            EstimatedDeliveryDate = p.FechaEntregaEstimada,
        };

    internal static string NombreCliente(Pedido p)
        => p.Cliente is null
            ? $"Cliente {p.ClienteId}"
            : $"{p.Cliente.Nombre} {p.Cliente.Apellido}".Trim();
}

/// <summary>
/// Detalle del pedido. Los ítems provienen del snapshot congelado del
/// presupuesto aceptado (BR-071); las líneas se reutilizan del contrato de
/// presupuestos porque son el mismo snapshot comercial.
/// </summary>
public sealed class OrderDto
{
    public int Id { get; init; }

    public int CustomerId { get; init; }

    public string CustomerName { get; init; } = string.Empty;

    public int? QuoteId { get; init; }

    public DateTime CreationDate { get; init; }

    public EstadoPedido Status { get; init; }

    public decimal Total { get; init; }

    /// <summary>Marca de inicio de producción (→ EnProduccion, BR-076).</summary>
    public DateTime? ProductionStartDate { get; init; }

    /// <summary>Marca de fin de producción (→ Listo, BR-076).</summary>
    public DateTime? ProductionEndDate { get; init; }

    /// <summary>Entrega estimada acordada (editable, BR-076).</summary>
    public DateTime? EstimatedDeliveryDate { get; init; }

    /// <summary>Entrega real (→ Entregado, BR-076).</summary>
    public DateTime? DeliveryDate { get; init; }

    /// <summary>Notas operativas del pedido (máx. 2000, BR-076).</summary>
    public string? Notes { get; init; }

    public List<QuoteItemDto> Items { get; init; } = new();

    public static OrderDto From(Pedido p)
        => new()
        {
            Id = p.Id,
            CustomerId = p.ClienteId,
            CustomerName = OrderListDto.NombreCliente(p),
            QuoteId = p.PresupuestoId > 0 ? p.PresupuestoId : null,
            CreationDate = p.FechaCreacion,
            Status = p.Estado,
            Total = p.Total,
            ProductionStartDate = p.FechaInicioProduccion,
            ProductionEndDate = p.FechaFinProduccion,
            EstimatedDeliveryDate = p.FechaEntregaEstimada,
            DeliveryDate = p.FechaEntrega,
            Notes = p.Notas,
            Items = p.Presupuesto?.Detalles.Select(QuoteItemDto.From).ToList() ?? new List<QuoteItemDto>(),
        };
}

/// <summary>Fila del historial de estados (BR-075).</summary>
public sealed class OrderHistoryDto
{
    public int Id { get; init; }

    /// <summary>Null cuando el registro corresponde a la creación.</summary>
    public EstadoPedido? PreviousStatus { get; init; }

    public EstadoPedido NewStatus { get; init; }

    public DateTime Date { get; init; }

    /// <summary>Usuario responsable; null si el cambio lo hizo el sistema.</summary>
    public string? UserName { get; init; }

    public string? Comment { get; init; }

    public static OrderHistoryDto From(PedidoHistorial h)
        => new()
        {
            Id = h.Id,
            PreviousStatus = h.EstadoAnterior,
            NewStatus = h.EstadoNuevo,
            Date = h.FechaUtc,
            UserName = h.Usuario is null
                ? null
                : $"{h.Usuario.Nombre} {h.Usuario.Apellido}".Trim(),
            Comment = h.Comentario,
        };
}

/// <summary>Cambio de estado (POST /api/v1/orders/{id}/status).</summary>
public sealed record ChangeOrderStatusDto(EstadoPedido Status, string? Comment);

/// <summary>
/// Edición de la información de seguimiento editable (PATCH
/// /api/v1/orders/{id}, BR-076): notas y entrega estimada. Null limpia el
/// valor.
/// </summary>
public sealed record UpdateOrderDto(string? Notes, DateTime? EstimatedDeliveryDate);
