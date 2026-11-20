using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Infrastructure.Persistence.Mappings;

public class UsuarioPermisoMapping : IEntityTypeConfiguration<UsuarioPermiso>
{
    public void Configure(EntityTypeBuilder<UsuarioPermiso> builder)
    {
        builder.ToTable("UsuarioPermisos");

        builder.HasKey(up => new { up.UsuarioId, up.PermisoId });

        builder.HasOne(up => up.Usuario)
            .WithMany()
            .HasForeignKey(up => up.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(up => up.Permiso)
            .WithMany()
            .HasForeignKey(up => up.PermisoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Sin seed: los permisos explícitos se asignan por usuario en la
        // fase de administración (PUT /users/{id}/permissions).
    }
}
