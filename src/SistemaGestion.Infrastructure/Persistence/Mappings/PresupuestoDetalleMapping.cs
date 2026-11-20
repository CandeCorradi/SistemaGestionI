using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Infrastructure.Persistence.Mappings;

public class PresupuestoDetalleMapping : IEntityTypeConfiguration<PresupuestoDetalle>
{
    public void Configure(EntityTypeBuilder<PresupuestoDetalle> builder)
    {
        builder.ToTable("PresupuestoDetalles");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Descripcion)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.PrecioUnitario)
            .HasPrecision(18, 2);

        builder.Property(d => d.Descuento)
            .HasPrecision(18, 2);

        builder.Property(d => d.Subtotal)
            .HasPrecision(18, 2);

        builder.HasOne(d => d.Presupuesto)
            .WithMany(p => p.Detalles)
            .HasForeignKey(d => d.PresupuestoId);

        builder.HasOne(d => d.Producto)
            .WithMany()
            .HasForeignKey(d => d.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.ProductoId);
    }
}
