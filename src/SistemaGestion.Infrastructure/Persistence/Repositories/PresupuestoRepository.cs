using Microsoft.EntityFrameworkCore;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Infrastructure.Persistence.Repositories;

/// <summary>
/// Persistencia de presupuestos con EF Core + PostgreSQL. Las lecturas de
/// detalle siempre incluyen Detalles (líneas) y Cliente (nombre); las de
/// escritura usan tracking para que las transiciones de estado se
/// persistan con SaveChanges.
/// </summary>
public sealed class PresupuestoRepository : IPresupuestoRepository
{
    private readonly SistemaGestionDbContext _context;

    public PresupuestoRepository(SistemaGestionDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<Presupuesto>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        int? clienteId,
        EstadoPresupuesto? estado,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Presupuestos.AsNoTracking();

        if (clienteId.HasValue)
        {
            query = query.Where(p => p.ClienteId == clienteId.Value);
        }

        if (estado.HasValue)
        {
            query = query.Where(p => p.Estado == estado.Value);
        }

        if (dateFrom.HasValue)
        {
            var desde = DateTime.SpecifyKind(dateFrom.Value.Date, DateTimeKind.Utc);
            query = query.Where(p => p.FechaCreacion >= desde);
        }

        if (dateTo.HasValue)
        {
            var hasta = DateTime.SpecifyKind(dateTo.Value.Date, DateTimeKind.Utc).AddDays(1);
            query = query.Where(p => p.FechaCreacion < hasta);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(p =>
                p.Cliente!.Nombre.ToLower().Contains(term)
                || p.Cliente!.Apellido.ToLower().Contains(term)
                || p.Cliente!.Email.ToLower().Contains(term));
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(p => p.Cliente)
            .Include(p => p.Detalles)
            .OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Presupuesto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
        };
    }

    public Task<Presupuesto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Presupuestos
            .Include(p => p.Detalles)
            .Include(p => p.Cliente)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Presupuesto?> GetByIdReadOnlyAsync(int id, CancellationToken cancellationToken = default)
        => _context.Presupuestos
            .AsNoTracking()
            .Include(p => p.Detalles)
            .Include(p => p.Cliente)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Presupuesto> AddAsync(Presupuesto presupuesto, CancellationToken cancellationToken = default)
    {
        await _context.Presupuestos.AddAsync(presupuesto, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return presupuesto;
    }

    public async Task UpdateAsync(Presupuesto presupuesto, CancellationToken cancellationToken = default)
    {
        // Las líneas quitadas del snapshot quedan tracked (cargadas por
        // Include) pero fuera de la colección: EF no las marcaría como
        // eliminadas, así que se remueven explícitamente.
        var tracked = _context.Set<PresupuestoDetalle>().Local
            .Where(d => d.PresupuestoId == presupuesto.Id)
            .Select(d => d.Id)
            .ToHashSet();

        if (tracked.Count > 0)
        {
            var conservadas = presupuesto.Detalles.Select(d => d.Id).ToHashSet();
            var eliminadas = _context.Set<PresupuestoDetalle>().Local
                .Where(d => d.PresupuestoId == presupuesto.Id && !conservadas.Contains(d.Id))
                .ToList();

            if (eliminadas.Count > 0)
            {
                _context.Set<PresupuestoDetalle>().RemoveRange(eliminadas);
            }
        }

        _context.Presupuestos.Update(presupuesto);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Pedido?> TryAcceptAndCreateOrderAsync(
        Presupuesto presupuesto,
        DateTime fechaUtc,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        // CAS sobre el estado (BR-065): sólo pasa si sigue Enviado. Si otro
        // proceso aceptó primero, affected = 0 → rollback y nada se creó.
        var affected = await _context.Presupuestos
            .Where(p => p.Id == presupuesto.Id && p.Estado == EstadoPresupuesto.Enviado)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(p => p.Estado, EstadoPresupuesto.Aceptado),
                cancellationToken);

        if (affected == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var pedido = Pedido.Crear(
            clienteId: presupuesto.ClienteId,
            presupuestoId: presupuesto.Id,
            total: presupuesto.Total,
            fechaCreacionUtc: fechaUtc);

        await _context.Pedidos.AddAsync(pedido, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Historial inicial de creación (BR-075): EstadoAnterior null y sin
        // usuario porque lo genera el sistema; mismo commit que el pedido.
        await _context.PedidoHistoriales.AddAsync(
            PedidoHistorial.Crear(
                pedidoId: pedido.Id,
                estadoAnterior: null,
                estadoNuevo: EstadoPedido.Pendiente,
                fechaUtc: fechaUtc,
                comentario: $"Creado al aceptar el presupuesto #{presupuesto.Id}"),
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return pedido;
    }

    public async Task<int?> GetOrderIdAsync(int presupuestoId, CancellationToken cancellationToken = default)
        => await _context.Pedidos.AsNoTracking()
            .Where(p => p.PresupuestoId == presupuestoId)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);
}
