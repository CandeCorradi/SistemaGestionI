using Microsoft.EntityFrameworkCore;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Shared.Results;

namespace SistemaGestion.Infrastructure.Persistence.Repositories;

/// <summary>
/// Persistencia de notificaciones con EF Core + PostgreSQL. Lecturas con
/// AsNoTracking (AGENTS.md §26).
/// </summary>
public sealed class NotificacionRepository : INotificacionRepository
{
    private readonly SistemaGestionDbContext _context;

    public NotificacionRepository(SistemaGestionDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<Notificacion>> GetPagedByUsuarioAsync(
        int usuarioId,
        int page,
        int pageSize,
        bool soloNoLeidas,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Notificaciones.AsNoTracking()
            .Where(n => n.UsuarioId == usuarioId);

        if (soloNoLeidas)
        {
            query = query.Where(n => !n.Leida);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(n => n.FechaCreacionUtc)
            .ThenByDescending(n => n.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Notificacion>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
        };
    }

    public async Task<Notificacion?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
        => await _context.Notificaciones
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public Task<int> CountUnreadAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
        => _context.Notificaciones
            .CountAsync(n => n.UsuarioId == usuarioId && !n.Leida, cancellationToken);

    public async Task AddRangeAsync(
        IReadOnlyCollection<Notificacion> notificaciones,
        CancellationToken cancellationToken = default)
    {
        await _context.Notificaciones.AddRangeAsync(notificaciones, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);

    public async Task<int> MarkAllReadAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var pendientes = await _context.Notificaciones
            .Where(n => n.UsuarioId == usuarioId && !n.Leida)
            .ToListAsync(cancellationToken);

        foreach (var notificacion in pendientes)
        {
            notificacion.MarcarLeida();
        }

        await _context.SaveChangesAsync(cancellationToken);
        return pendientes.Count;
    }
}
