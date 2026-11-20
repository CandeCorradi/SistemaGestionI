using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Abstractions;

/// <summary>
/// Persistencia de facturas (functional-spec §20, BR-130/BR-131). La
/// emisión de una factura (inserta + número de documento) se ejecuta en
/// una única transacción para no dejar facturas sin numerar (BR-151).
/// </summary>
public interface IFacturaRepository
{
    /// <summary>Si el pedido ya tiene factura (una factura por pedido).</summary>
    Task<bool> ExistsByPedidoAsync(
        int pedidoId,
        CancellationToken cancellationToken = default);

    /// <summary>Registra una factura nueva (asigna el Id).</summary>
    Task AddAsync(Factura factura, CancellationToken cancellationToken = default);

    /// <summary>Persiste cambios pendientes (p. ej. el número de documento).</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ejecuta la operación dentro de una transacción (BR-151). Una
    /// violación de unicidad (pedido ya facturado de forma concurrente) se
    /// relanza como <see cref="InvalidOperationException"/> para que la
    /// capa de aplicación responda 409.
    /// </summary>
    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Factura por id con Pedido+Cliente y Usuario, de solo lectura.
    /// </summary>
    Task<Factura?> GetByIdReadOnlyAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Listado paginado global con filtros de pedido, cliente y fechas y
    /// búsqueda en número de documento o nombre de cliente. Incluye
    /// Pedido+Cliente y Usuario.
    /// </summary>
    Task<PagedResult<Factura>> GetPagedAsync(
        int page,
        int pageSize,
        int? pedidoId,
        int? clienteId,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? search,
        CancellationToken cancellationToken = default);
}
