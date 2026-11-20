namespace SistemaGestion.Domain.Entities;

/// <summary>
/// Asociación entre un rol y un permiso (BR-002: cada rol tiene un conjunto
/// de permisos). Clave compuesta RolId + PermisoId; sin surrogate key.
/// </summary>
public class RolPermiso
{
    public int RolId { get; private set; }

    public int PermisoId { get; private set; }

    public Rol Rol { get; private set; } = null!;

    public Permiso Permiso { get; private set; } = null!;

    /// <summary>
    /// Constructor para EF Core / seed.
    /// </summary>
    public RolPermiso(int rolId, int permisoId)
    {
        if (rolId <= 0)
        {
            throw new ArgumentException("El rol es obligatorio.", nameof(rolId));
        }

        if (permisoId <= 0)
        {
            throw new ArgumentException("El permiso es obligatorio.", nameof(permisoId));
        }

        RolId = rolId;
        PermisoId = permisoId;
    }
}
