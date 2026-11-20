using SistemaGestion.Application.Pedidos.Dtos;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;

namespace SistemaGestion.Application.Pagos.Dtos;

/// <summary>Fila de pago (api-spec §17).</summary>
public sealed class PaymentDto
{
    public int Id { get; init; }

    public int OrderId { get; init; }

    /// <summary>Cliente del pedido; null si el pedido no fue cargado.</summary>
    public int? CustomerId { get; init; }

    public string? CustomerName { get; init; }

    public decimal Amount { get; init; }

    /// <summary>Momento del registro en UTC (AGENTS.md §17).</summary>
    public DateTime Date { get; init; }

    public MetodoPago PaymentMethod { get; init; }

    public string? Reference { get; init; }

    public string? Notes { get; init; }

    /// <summary>Usuario interno que registró el pago (auditoría §18).</summary>
    public string? UserName { get; init; }

    public static PaymentDto From(Pago p, Pedido? pedidoContexto = null, Usuario? registradoPor = null)
    {
        var pedido = pedidoContexto ?? p.Pedido;
        var usuario = registradoPor ?? p.Usuario;

        return new PaymentDto
        {
            Id = p.Id,
            OrderId = p.PedidoId,
            CustomerId = pedido?.ClienteId,
            CustomerName = pedido is null ? null : OrderListDto.NombreCliente(pedido),
            Amount = p.Monto,
            Date = p.FechaPago,
            PaymentMethod = p.Metodo,
            Reference = p.Referencia,
            Notes = p.Notas,
            UserName = usuario is null
                ? null
                : $"{usuario.Nombre} {usuario.Apellido}".Trim(),
        };
    }
}

/// <summary>
/// Pagos de un pedido con el resumen financiero. El saldo lo calcula
/// siempre el backend: Total del pedido − pagos registrados (BR-083).
/// </summary>
public sealed class OrderPaymentsDto
{
    public List<PaymentDto> Items { get; init; } = new();

    public decimal TotalPaid { get; init; }

    /// <summary>Saldo pendiente; nunca negativo (BR-083).</summary>
    public decimal Balance { get; init; }
}

/// <summary>
/// Registro de pago (POST /api/v1/orders/{orderId}/payments): monto
/// positivo con 2 decimales (BR-080), método, referencia y notas.
/// </summary>
public sealed record RegisterPaymentDto(
    decimal Amount,
    MetodoPago PaymentMethod,
    string? Reference,
    string? Notes);
