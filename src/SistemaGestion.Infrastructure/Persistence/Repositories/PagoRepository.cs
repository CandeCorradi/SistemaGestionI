using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Infrastructure.Persistence.Repositories;

/// <summary>
/// Persistencia de pagos con EF Core + PostgreSQL. El registro de un pago
/// se ejecuta dentro de una transacción SERIALIZABLE para que la lectura
/// de la suma de pagos y la inserción sean atómicas frente a registros
/// concurrentes (AGENTS.md §56); un fallo de serialización (40001) se
/// relanza como InvalidOperationException para que la aplicación responda
/// 409 (BR-081/BR-083).
/// </summary>
public sealed class PagoRepository : IPagoRepository
{
    private readonly SistemaGestionDbContext _context;

    public PagoRepository(SistemaGestionDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> GetTotalPagadoAsync(
        int pedidoId,
        CancellationToken cancellationToken = default)
        => await _context.Pagos
            .AsNoTracking()
            .Where(p => p.PedidoId == pedidoId)
            .SumAsync(p => (decimal?)p.Monto, cancellationToken) ?? 0m;

    public async Task AddAsync(Pago pago, CancellationToken cancellationToken = default)
    {
        await _context.Pagos.AddAsync(pago, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ExecuteSerializableAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex) when (EsFalloDeSerializacion(ex))
        {
            // La transacción queda abortada; el dispose hace el rollback.
            throw new InvalidOperationException(
                "Otro pago se registró al mismo tiempo; volvé a intentar " +
                "(BR-081/BR-083).", ex);
        }
    }

    public async Task<IReadOnlyList<Pago>> GetByPedidoAsync(
        int pedidoId,
        CancellationToken cancellationToken = default)
        => await _context.Pagos
            .AsNoTracking()
            .Include(p => p.Usuario)
            .Where(p => p.PedidoId == pedidoId)
            .OrderByDescending(p => p.FechaPago)
            .ThenByDescending(p => p.Id)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<Pago>> GetPagedAsync(
        int page,
        int pageSize,
        int? pedidoId,
        int? clienteId,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Pagos.AsNoTracking();

        if (pedidoId.HasValue)
        {
            query = query.Where(p => p.PedidoId == pedidoId.Value);
        }

        if (clienteId.HasValue)
        {
            query = query.Where(p => p.Pedido!.ClienteId == clienteId.Value);
        }

        if (dateFrom.HasValue)
        {
            var desde = DateTime.SpecifyKind(dateFrom.Value.Date, DateTimeKind.Utc);
            query = query.Where(p => p.FechaPago >= desde);
        }

        if (dateTo.HasValue)
        {
            var hasta = DateTime.SpecifyKind(dateTo.Value.Date, DateTimeKind.Utc).AddDays(1);
            query = query.Where(p => p.FechaPago < hasta);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(p =>
                (p.Referencia != null && p.Referencia.ToLower().Contains(term))
                || (p.Notas != null && p.Notas.ToLower().Contains(term)));
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(p => p.Pedido)
                .ThenInclude(p => p!.Cliente)
            .Include(p => p.Usuario)
            .OrderByDescending(p => p.FechaPago)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Pago>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
        };
    }

    public async Task<ClientePagosResumen> GetResumenClienteAsync(
        int clienteId,
        CancellationToken cancellationToken = default)
    {
        var pedidos = await _context.Pedidos
            .AsNoTracking()
            .Where(p => p.ClienteId == clienteId && p.Estado != EstadoPedido.Cancelado)
            .SumAsync(p => (decimal?)p.Total, cancellationToken) ?? 0m;

        var pagado = await _context.Pagos
            .AsNoTracking()
            .Where(p => p.Pedido!.ClienteId == clienteId)
            .SumAsync(p => (decimal?)p.Monto, cancellationToken) ?? 0m;

        return new ClientePagosResumen(pedidos, pagado);
    }

    private static bool EsFalloDeSerializacion(Exception ex)
        => ex is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure }
            || ex is DbUpdateException { InnerException: PostgresException inner }
                && inner.SqlState == PostgresErrorCodes.SerializationFailure;
}
