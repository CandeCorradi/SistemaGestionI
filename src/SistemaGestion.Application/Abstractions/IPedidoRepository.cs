using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Abstractions;

/// <summary>
/// Persistencia de pedidos y de su historial inmutable de estados
/// (BR-072/BR-075).
/// </summary>
public interface IPedidoRepository
{
    /// <summary>
    /// Listado paginado con filtros de cliente, estado y fechas. Búsqueda
    /// por nombre o email del cliente.
    /// </summary>
    Task<PagedResult<Pedido>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        int? clienteId,
        EstadoPedido? estado,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default);

    /// <summary>Carga tracking con Cliente y Presupuesto (ítems del snapshot).</summary>
    Task<Pedido?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Igual que GetByIdAsync pero sin tracking (sólo lectura).</summary>
    Task<Pedido?> GetByIdReadOnlyAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Historial de estados ordenado del más reciente al más antiguo.</summary>
    Task<IReadOnlyList<PedidoHistorial>> GetHistorialAsync(
        int pedidoId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Guarda el cambio de estado y agrega la fila de historial en un único
    /// SaveChanges (BR-075: cada transición registra su historial).
    /// </summary>
    Task UpdateWithHistoryAsync(
        Pedido pedido,
        PedidoHistorial historial,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste la edición de la información de seguimiento del pedido
    /// (notas y entrega estimada, BR-076).
    /// </summary>
    Task UpdateAsync(Pedido pedido, CancellationToken cancellationToken = default);
}
