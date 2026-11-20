using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;

namespace SistemaGestion.Application.Abstractions;

/// <summary>
/// Consultas de solo lectura para reportes (functional-spec §21, BR-140/BR-141).
/// Devuelven listas acotadas por los filtros de rango de fechas para
/// construir los documentos de reporte en memoria (el volumen esperado de
/// una única carpintería es bajo; AGENTS.md §62).
/// </summary>
public interface IReportRepository
{
    /// <summary>Pedidos del rango con Cliente, opcionalmente por estado/cliente.</summary>
    Task<IReadOnlyList<Pedido>> GetPedidosAsync(
        DateTime? desde,
        DateTime? hasta,
        EstadoPedido? estado,
        int? clienteId,
        CancellationToken cancellationToken = default);

    /// <summary>Pagos del rango con Pedido+Cliente y Usuario registrado.</summary>
    Task<IReadOnlyList<Pago>> GetPagosAsync(
        DateTime? desde,
        DateTime? hasta,
        int? clienteId,
        MetodoPago? metodo,
        CancellationToken cancellationToken = default);

    /// <summary>Presupuestos del rango con Cliente.</summary>
    Task<IReadOnlyList<Presupuesto>> GetPresupuestosAsync(
        DateTime? desde,
        DateTime? hasta,
        EstadoPresupuesto? estado,
        int? clienteId,
        CancellationToken cancellationToken = default);

    /// <summary>Facturas emitidas del rango (snapshot histórico, BR-141).</summary>
    Task<IReadOnlyList<Factura>> GetFacturasAsync(
        DateTime? desde,
        DateTime? hasta,
        int? clienteId,
        CancellationToken cancellationToken = default);

    /// <summary>Productos para el reporte de inventario.</summary>
    Task<IReadOnlyList<Producto>> GetProductosAsync(
        TipoProducto? tipo,
        bool? activo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Suma de pagos por pedido para los pedidos indicados (columna
    /// "Pagado" del reporte de pedidos).
    /// </summary>
    Task<IReadOnlyDictionary<int, decimal>> GetSumasPagadasAsync(
        IReadOnlyCollection<int> pedidoIds,
        CancellationToken cancellationToken = default);
}
