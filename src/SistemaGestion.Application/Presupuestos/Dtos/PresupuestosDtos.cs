using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;

namespace SistemaGestion.Application.Presupuestos.Dtos;

/// <summary>Línea recibida en creación/edición de presupuesto.</summary>
public sealed record CreateQuoteItemDto(
    int? ProductId,
    string? Description,
    int? Quantity,
    decimal? UnitPrice,
    decimal? Discount);

/// <summary>POST /api/v1/quotes (BR-060: un cliente exacto; BR-061: ítems).</summary>
public sealed record CreateQuoteDto(
    int CustomerId,
    DateTime? ExpirationDate,
    string? Notes,
    string? Conditions,
    List<CreateQuoteItemDto> Items);

/// <summary>
/// PATCH /api/v1/quotes/{id}: mezcla parcial; <c>Items</c> null conserva
/// las líneas actuales, lista no vacía las reemplaza.
/// </summary>
public sealed record UpdateQuoteDto(
    int? CustomerId,
    DateTime? ExpirationDate,
    string? Notes,
    string? Conditions,
    List<CreateQuoteItemDto>? Items);

/// <summary>Cuerpo de las acciones send/reject/cancel.</summary>
public sealed record QuoteStateChangeDto(string? Reason);

public sealed record QuoteItemDto(
    int Id,
    int? ProductId,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal Subtotal)
{
    public static QuoteItemDto From(PresupuestoDetalle d) => new(
        d.Id,
        d.ProductoId,
        d.Descripcion,
        d.Cantidad,
        d.PrecioUnitario,
        d.Descuento,
        d.Subtotal);
}

public sealed class QuoteDto
{
    public int Id { get; init; }
    public int CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public DateTime CreationDate { get; init; }
    public DateTime? ExpirationDate { get; init; }
    public DateTime? SendDate { get; init; }
    public EstadoPresupuesto Status { get; init; }
    public decimal Subtotal { get; init; }
    public decimal Discount { get; init; }
    public decimal Total { get; init; }
    public string? Notes { get; init; }
    public string? Conditions { get; init; }
    public string? StateReason { get; init; }

    /// <summary>Vencido por fecha de validez con estado aún no definitivo (BR-064).</summary>
    public bool Expired { get; init; }

    /// <summary> Pedido originado al aceptar (BR-065); null mientras no se acepta.</summary>
    public int? OrderId { get; init; }

    public List<QuoteItemDto> Items { get; init; } = new();

    public static QuoteDto From(Presupuesto p, DateTime fechaHoyUtc, int? orderId = null)
    {
        var clienteNombre = p.Cliente is null
            ? $"Cliente {p.ClienteId}"
            : $"{p.Cliente.Nombre} {p.Cliente.Apellido}".Trim();

        return new QuoteDto
        {
            Id = p.Id,
            CustomerId = p.ClienteId,
            CustomerName = clienteNombre,
            CreationDate = p.FechaCreacion,
            ExpirationDate = p.FechaValidez,
            SendDate = p.FechaEnvio,
            Status = p.Estado,
            Subtotal = p.Subtotal,
            Discount = p.Descuento,
            Total = p.Total,
            Notes = p.Notas,
            Conditions = p.Condiciones,
            StateReason = p.EstadoMotivo,
            Expired = p.EstaExpirado(fechaHoyUtc),
            OrderId = orderId,
            Items = p.Detalles.Select(QuoteItemDto.From).ToList(),
        };
    }
}

public sealed class QuoteListDto
{
    public int Id { get; init; }
    public int CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public DateTime CreationDate { get; init; }
    public DateTime? ExpirationDate { get; init; }
    public EstadoPresupuesto Status { get; init; }
    public decimal Total { get; init; }
    public int ItemsCount { get; init; }
    public bool Expired { get; init; }

    public static QuoteListDto From(Presupuesto p, DateTime fechaHoyUtc)
    {
        var clienteNombre = p.Cliente is null
            ? $"Cliente {p.ClienteId}"
            : $"{p.Cliente.Nombre} {p.Cliente.Apellido}".Trim();

        return new QuoteListDto
        {
            Id = p.Id,
            CustomerId = p.ClienteId,
            CustomerName = clienteNombre,
            CreationDate = p.FechaCreacion,
            ExpirationDate = p.FechaValidez,
            Status = p.Estado,
            Total = p.Total,
            ItemsCount = p.Detalles.Count,
            Expired = p.EstaExpirado(fechaHoyUtc),
        };
    }
}
