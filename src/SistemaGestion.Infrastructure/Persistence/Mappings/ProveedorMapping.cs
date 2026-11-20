using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Infrastructure.Persistence.Mappings;

/// <summary>
/// Persistencia de proveedores (functional-spec §7). Baja lógica con
/// <c>Activo</c> (BR-021); terminología "Proveedor" (BR-020); email único
/// cuando existe (PostgreSQL admite múltiples NULL en índices únicos).
/// </summary>
public class ProveedorMapping : IEntityTypeConfiguration<Proveedor>
{
    public void Configure(EntityTypeBuilder<Proveedor> builder)
    {
        builder.ToTable("Proveedores");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Contacto)
            .HasMaxLength(100);

        builder.Property(p => p.Email)
            .HasMaxLength(150);

        builder.Property(p => p.Telefono)
            .HasMaxLength(30);

        builder.Property(p => p.Direccion)
            .HasMaxLength(200);

        builder.Property(p => p.Notas)
            .HasMaxLength(2000);

        builder.Property(p => p.Activo)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.FechaCreacion)
            .IsRequired();

        builder.HasIndex(p => p.Email).IsUnique();
    }
}
