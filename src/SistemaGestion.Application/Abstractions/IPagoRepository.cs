using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Abstractions;

/// <summary>
/// Persistencia de pagos (functional-spec §17, BR-080..BR-083). El
/// historial de pagos es inmutable: sólo se agregan registros (BR-082).
/// </summary>
public interface IPagoRepository
{
    /// <summary>Suma en base de los pagos de un pedido (BR-083).</summary>
    Task<decimal> GetTotalPagadoAsync(int pedidoId, CancellationToken cancellationToken = default);

    /// <summary>Registra un pago nuevo.</summary>
    Task AddAsync(Pago pago, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ejecuta la operación dentro de una transacción SERIALIZABLE para
    /// que leer la suma de pagos y registrar el nuevo pago sea atómico
    /// frente a registros concurrentes (AGENTS.md §56). Un fallo de
    /// serialización se relanza como <see cref="InvalidOperationException"/>
    /// para que la capa de aplicación lo traduzca a 409.
    /// </summary>
    Task ExecuteSerializableAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pagos de un pedido, del más reciente al más antiguo, con el usuario
    /// que los registró.
    /// </summary>
    Task<IReadOnlyList<Pago>> GetByPedidoAsync(
        int pedidoId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Listado paginado global con filtros de pedido, cliente y fechas y
    /// búsqueda en referencia/notas. Incluye Pedido+Cliente y Usuario.
    /// </summary>
    Task<PagedResult<Pago>> GetPagedAsync(
        int page,
        int pageSize,
        int? pedidoId,
        int? clienteId,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? search,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Totales de un cliente para el resumen del portal: suma de sus
    /// pedidos (excluyendo cancelados) y suma de sus pagos, en una sola
    /// pasada (AGENTS.md §62).
    /// </summary>
    Task<ClientePagosResumen> GetResumenClienteAsync(
        int clienteId,
        CancellationToken cancellationToken = default);
}

/// <summary>Totales de pedidos y pagos de un cliente (portal).</summary>
public sealed record ClientePagosResumen(decimal TotalPedidos, decimal TotalPagado);
