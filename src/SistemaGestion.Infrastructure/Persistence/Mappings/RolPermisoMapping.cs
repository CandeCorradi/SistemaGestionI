using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGestion.Domain.Authorization;
using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Infrastructure.Persistence.Mappings;

/// <summary>
/// Asociación rol ↔ permiso con seed inicial (BR-002):
/// SuperAdmin, Owner y Admin reciben el catálogo completo; Empleado recibe
/// exactamente el ejemplo de BR-002; Cliente no recibe permisos de gestión
/// (BR-004). Son valores iniciales configurables, no reglas de negocio
/// nuevas (business-rules.md no se modifica).
/// </summary>
public class RolPermisoMapping : IEntityTypeConfiguration<RolPermiso>
{
    public const int RolSuperAdmin = 1;
    public const int RolOwner = 2;
    public const int RolAdmin = 3;
    public const int RolEmpleado = 4;

    /// <summary>Permisos por defecto de Empleado (BR-002).</summary>
    public static readonly int[] PermisosEmpleado = { 1, 2, 16, 17 };

    public void Configure(EntityTypeBuilder<RolPermiso> builder)
    {
        builder.ToTable("RolPermisos");

        builder.HasKey(rp => new { rp.RolId, rp.PermisoId });

        builder.HasOne(rp => rp.Rol)
            .WithMany()
            .HasForeignKey(rp => rp.RolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(rp => rp.Permiso)
            .WithMany()
            .HasForeignKey(rp => rp.PermisoId)
            .OnDelete(DeleteBehavior.Restrict);

        var todosLosPermisos = Permisos.Catalogo.Select(p => p.Id).ToList();

        builder.HasData(
            todosLosPermisos.Select(id => new RolPermiso(RolSuperAdmin, id))
                .Concat(todosLosPermisos.Select(id => new RolPermiso(RolOwner, id)))
                .Concat(todosLosPermisos.Select(id => new RolPermiso(RolAdmin, id)))
                .Concat(PermisosEmpleado.Select(id => new RolPermiso(RolEmpleado, id))));
    }
}
