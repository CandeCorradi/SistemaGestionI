using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Application.Abstractions;

/// <summary>
/// Contrato de persistencia de presupuestos (functional-spec §12). La
/// implementación (EF Core + PostgreSQL) vive en Infrastructure (AGENTS.md §25).
/// </summary>
public interface IPresupuestoRepository
{
    /// <summary>
    /// Listado paginado con Cliente cargado para el nombre. Búsqueda por
    /// nombre/apellido/email del cliente.
    /// </summary>
    Task<PagedResult<Presupuesto>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        int? clienteId,
        EstadoPresupuesto? estado,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default);

    /// <summary>Presupuesto con Detalles y Cliente (tracking, para escritura).</summary>
    Task<Presupuesto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Presupuesto con Detalles y Cliente (AsNoTracking, para lectura).</summary>
    Task<Presupuesto?> GetByIdReadOnlyAsync(int id, CancellationToken cancellationToken = default);

    Task<Presupuesto> AddAsync(Presupuesto presupuesto, CancellationToken cancellationToken = default);

    Task UpdateAsync(Presupuesto presupuesto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Acepta el presupuesto y crea su pedido en una sola transacción
    /// (BR-065): sólo si sigue Enviado (CAS sobre el estado), si otro
    /// proceso lo ganó devuelve null sin crear nada. El índice único sobre
    /// PresupuestoId en Pedidos garantiza como último recurso un solo
    /// pedido por presupuesto.
    /// </summary>
    Task<Pedido?> TryAcceptAndCreateOrderAsync(
        Presupuesto presupuesto,
        DateTime fechaUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Id del pedido originado por el presupuesto, si existe.</summary>
    Task<int?> GetOrderIdAsync(int presupuestoId, CancellationToken cancellationToken = default);
}
