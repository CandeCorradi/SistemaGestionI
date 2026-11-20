using Microsoft.EntityFrameworkCore;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Infrastructure.Persistence.Repositories;

/// <summary>
/// Persistencia de permisos con EF Core + PostgreSQL (BR-002).
/// Las lecturas usan AsNoTracking (AGENTS.md §26).
/// </summary>
public sealed class PermisoRepository : IPermisoRepository
{
    private readonly SistemaGestionDbContext _context;

    public PermisoRepository(SistemaGestionDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Permiso>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        List<Permiso> permisos = await _context.Permisos
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .ToListAsync(cancellationToken);

        return permisos;
    }

    public async Task<IReadOnlyList<string>> GetExplicitCodesAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        return await _context.UsuarioPermisos
            .AsNoTracking()
            .Where(up => up.UsuarioId == usuarioId)
            .OrderBy(up => up.PermisoId)
            .Select(up => up.Permiso.Codigo)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>?> GetEffectiveCodesByFirebaseUidAsync(
        string firebaseUid,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid, cancellationToken);

        // Usuario interno inexistente o dado de baja: la autorización debe
        // denegar (AGENTS.md §8: no confiar en la identidad externa sola).
        if (usuario is null || !usuario.Activo)
        {
            return null;
        }

        var delRol = _context.RolPermisos
            .AsNoTracking()
            .Where(rp => rp.RolId == usuario.RolId)
            .Select(rp => rp.Permiso.Codigo);

        var explicitos = _context.UsuarioPermisos
            .AsNoTracking()
            .Where(up => up.UsuarioId == usuario.Id)
            .Select(up => up.Permiso.Codigo);

        return await delRol
            .Concat(explicitos)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceExplicitAsync(
        int usuarioId,
        IReadOnlyCollection<int> permisoIds,
        CancellationToken cancellationToken = default)
    {
        var actuales = await _context.UsuarioPermisos
            .Where(up => up.UsuarioId == usuarioId)
            .ToListAsync(cancellationToken);

        var aEliminar = actuales
            .Where(up => !permisoIds.Contains(up.PermisoId))
            .ToList();

        var existentes = actuales.Select(up => up.PermisoId).ToHashSet();
        var aAgregar = permisoIds
            .Where(id => !existentes.Contains(id))
            .Select(id => new UsuarioPermiso(usuarioId, id))
            .ToList();

        _context.UsuarioPermisos.RemoveRange(aEliminar);
        await _context.UsuarioPermisos.AddRangeAsync(aAgregar, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
