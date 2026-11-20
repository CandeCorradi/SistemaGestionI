namespace SistemaGestion.Domain.Entities;

/// <summary>
/// Permisos explícitos de un usuario, adicionales a los de su rol
/// (la autorización efectiva es unión: rol ∪ explícitos). Clave compuesta
/// UsuarioId + PermisoId; sin surrogate key.
/// </summary>
public class UsuarioPermiso
{
    public int UsuarioId { get; private set; }

    public int PermisoId { get; private set; }

    public Usuario Usuario { get; private set; } = null!;

    public Permiso Permiso { get; private set; } = null!;

    /// <summary>
    /// Constructor para EF Core / seed.
    /// </summary>
    public UsuarioPermiso(int usuarioId, int permisoId)
    {
        if (usuarioId <= 0)
        {
            throw new ArgumentException("El usuario es obligatorio.", nameof(usuarioId));
        }

        if (permisoId <= 0)
        {
            throw new ArgumentException("El permiso es obligatorio.", nameof(permisoId));
        }

        UsuarioId = usuarioId;
        PermisoId = permisoId;
    }
}
