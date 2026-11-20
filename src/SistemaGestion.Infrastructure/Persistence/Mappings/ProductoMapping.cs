using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Infrastructure.Persistence.Mappings;

/// <summary>
/// Persistencia de productos (functional-spec §8). SKU único como
/// identidad de negocio (BR-030), baja lógica con <c>Activo</c> (BR-032)
/// y FK opcional a Proveedor con borrado restringido para conservar el
/// historial del proveedor (BR-021). Decimales con precisión exacta:
/// nunca float/double (AGENTS.md §16).
/// </summary>
public class ProductoMapping : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        builder.ToTable("Productos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nombre)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.CodigoSku)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Descripcion)
            .HasMaxLength(2000);

        builder.Property(p => p.Categoria)
            .HasMaxLength(100);

        builder.Property(p => p.Precio)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.Costo)
            .HasPrecision(18, 2);

        builder.Property(p => p.Material)
            .HasMaxLength(100);

        builder.Property(p => p.Acabado)
            .HasMaxLength(100);

        builder.Property(p => p.Color)
            .HasMaxLength(100);

        builder.Property(p => p.Dimensiones)
            .HasMaxLength(100);

        builder.Property(p => p.StockActual)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.StockMinimo)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.Showroom)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.EsPersonalizado)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.Activo)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.FechaCreacion)
            .IsRequired();

        builder.HasIndex(p => p.CodigoSku).IsUnique();

        builder.HasOne<Proveedor>()
            .WithMany()
            .HasForeignKey(p => p.ProveedorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
