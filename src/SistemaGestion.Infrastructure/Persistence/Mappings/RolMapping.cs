using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Infrastructure.Persistence.Mappings;

public class RolMapping : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Codigo)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(r => r.Codigo).IsUnique();

        // BR-001: exactamente cinco roles fijos, sembrados en la base.
        builder.HasData(
            new Rol(Rol.SuperAdminCodigo, Rol.SuperAdminCodigo) { Id = 1 },
            new Rol(Rol.OwnerCodigo, Rol.OwnerCodigo) { Id = 2 },
            new Rol(Rol.AdminCodigo, Rol.AdminCodigo) { Id = 3 },
            new Rol(Rol.EmpleadoCodigo, Rol.EmpleadoCodigo) { Id = 4 },
            new Rol(Rol.ClienteCodigo, Rol.ClienteCodigo) { Id = 5 });
    }
}
