using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Abstractions;

/// <summary>
/// Persistencia de inventario (functional-spec §11). La aplicación de
/// movimientos es atómica: actualiza el stock con comparación condicional
/// del valor leído (CAS) e inserta el movimiento en la misma transacción
/// (BR-050); si el stock cambió concurrentemente devuelve false sin tocar
/// nada (BR-053).
/// </summary>
public interface IInventarioRepository
{
    /// <summary>Producto con su stock actual (tracking desactivado). Null si no existe.</summary>
    Task<Producto?> GetStockAsync(int productoId, CancellationToken cancellationToken = default);

    /// <summary>Historial paginado de movimientos, más recientes primero (BR-051).</summary>
    Task<PagedResult<MovimientoStock>> GetMovementsPagedAsync(
        int page,
        int pageSize,
        int? productoId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Aplica el movimiento de forma atómica (BR-050/BR-053): actualiza
    /// <c>Producto.StockActual</c> sólo si sigue igual a <paramref name="stockEsperado"/>
    /// (CAS) y registrа el movimiento en la misma transacción. Devuelve false
    /// si otro proceso modificó el stock entre la lectura y la escritura.
    /// </summary>
    Task<bool> TryApplyMovementAsync(
        MovimientoStock movimiento,
        int stockEsperado,
        CancellationToken cancellationToken = default);
}
