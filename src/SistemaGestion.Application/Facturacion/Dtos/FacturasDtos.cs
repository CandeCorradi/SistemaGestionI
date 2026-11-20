using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;

namespace SistemaGestion.Application.Facturacion.Dtos;

/// <summary>Fila de factura (api-spec §17A).</summary>
public sealed class FacturaDto
{
    public int Id { get; init; }

    public int OrderId { get; init; }

    /// <summary>Cliente congelado al emitir (BR-141).</summary>
    public int CustomerId { get; init; }

    /// <summary>Nombre del cliente congelado al emitir (BR-141).</summary>
    public string CustomerName { get; init; } = string.Empty;

    /// <summary>Número interno del documento (SIM-######).</summary>
    public string Number { get; init; } = string.Empty;

    /// <summary>Momento de emisión en UTC (AGENTS.md §17).</summary>
    public DateTime IssueDate { get; init; }

    /// <summary>Total congelado del pedido al emitir (AGENTS.md §16).</summary>
    public decimal Total { get; init; }

    public EstadoFactura Status { get; init; }

    /// <summary><c>true</c> si el documento es simulado (BR-130).</summary>
    public bool IsSimulated { get; init; }

    public string? Notes { get; init; }

    /// <summary>Usuario interno que emitió la factura (auditoría §18).</summary>
    public string? UserName { get; init; }

    public static FacturaDto From(Factura f, Usuario? emitidaPor = null)
    {
        var usuario = emitidaPor ?? f.Usuario;

        return new FacturaDto
        {
            Id = f.Id,
            OrderId = f.PedidoId,
            CustomerId = f.ClienteId,
            CustomerName = f.ClienteNombre,
            Number = f.Numero,
            IssueDate = f.FechaEmision,
            Total = f.Total,
            Status = f.Estado,
            IsSimulated = f.EsSimulada,
            Notes = f.Notas,
            UserName = usuario is null
                ? null
                : $"{usuario.Nombre} {usuario.Apellido}".Trim(),
        };
    }
}

/// <summary>
/// Emisión de factura (POST /api/v1/facturas): sólo requiere el pedido;
/// la numeración y la condición de simulado los resuelve el backend
/// (BR-130/BR-131).
/// </summary>
public sealed record CreateFacturaDto(int OrderId, string? Notes);
