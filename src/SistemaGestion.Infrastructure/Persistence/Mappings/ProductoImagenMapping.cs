using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Infrastructure.Persistence.Mappings;

/// <summary>
/// Persistencia de imágenes de producto: sólo referencias al storage
/// externo (BR-101: nunca binarios en la base de datos). FK a Producto
/// con borrado en cascada para no dejar huérfanos (AGENTS.md §24).
/// </summary>
public class ProductoImagenMapping : IEntityTypeConfiguration<ProductoImagen>
{
    public void Configure(EntityTypeBuilder<ProductoImagen> builder)
    {
        builder.ToTable("ProductoImagenes");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Path)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(i => i.Orden)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(i => i.FechaCreacion)
            .IsRequired();

        builder.HasIndex(i => new { i.ProductoId, i.Orden });

        builder.HasOne<Producto>()
            .WithMany()
            .HasForeignKey(i => i.ProductoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
