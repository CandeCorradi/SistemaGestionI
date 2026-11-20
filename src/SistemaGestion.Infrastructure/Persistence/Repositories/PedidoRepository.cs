using Microsoft.EntityFrameworkCore;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Infrastructure.Persistence.Repositories;

/// <summary>
/// Persistencia de pedidos con EF Core + PostgreSQL. Las lecturas de
/// detalle incluyen Cliente y Presupuesto (ítems del snapshot, BR-071); la
/// escritura de cambios de estado persiste el pedido y su fila de historial
/// en un único SaveChanges (BR-075).
/// </summary>
public sealed class PedidoRepository : IPedidoRepository
{
    private readonly SistemaGestionDbContext _context;

    public PedidoRepository(SistemaGestionDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<Pedido>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        int? clienteId,
        EstadoPedido? estado,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Pedidos.AsNoTracking();

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
            .OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Pedido>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
        };
    }

    public Task<Pedido?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Presupuesto)
                .ThenInclude(p => p!.Detalles)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Pedido?> GetByIdReadOnlyAsync(int id, CancellationToken cancellationToken = default)
        => _context.Pedidos
            .AsNoTracking()
            .Include(p => p.Cliente)
            .Include(p => p.Presupuesto)
                .ThenInclude(p => p!.Detalles)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PedidoHistorial>> GetHistorialAsync(
        int pedidoId,
        CancellationToken cancellationToken = default)
        => await _context.PedidoHistoriales
            .AsNoTracking()
            .Include(h => h.Usuario)
            .Where(h => h.PedidoId == pedidoId)
            .OrderByDescending(h => h.FechaUtc)
            .ThenByDescending(h => h.Id)
            .ToListAsync(cancellationToken);

    public async Task UpdateWithHistoryAsync(
        Pedido pedido,
        PedidoHistorial historial,
        CancellationToken cancellationToken = default)
    {
        _context.Pedidos.Update(pedido);
        await _context.PedidoHistoriales.AddAsync(historial, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Pedido pedido,
        CancellationToken cancellationToken = default)
    {
        _context.Pedidos.Update(pedido);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
