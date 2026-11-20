using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGestion.Domain.Authorization;
using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Infrastructure.Persistence.Mappings;

public class PermisoMapping : IEntityTypeConfiguration<Permiso>
{
    public void Configure(EntityTypeBuilder<Permiso> builder)
    {
        builder.ToTable("Permisos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Codigo)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(p => p.Codigo).IsUnique();

        // Catálogo fijo de permisos granulares (AGENTS.md §7 / BR-002),
        // sembrado con los Ids explícitos de Permisos.Catalogo.
        builder.HasData(
            Permisos.Catalogo.Select(p => new Permiso(p.Codigo, p.Nombre) { Id = p.Id }));
    }
}
