using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;

namespace SistemaGestion.Infrastructure.Persistence.Mappings;

public class PresupuestoMapping : IEntityTypeConfiguration<Presupuesto>
{
    public void Configure(EntityTypeBuilder<Presupuesto> builder)
    {
        builder.ToTable("Presupuestos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Estado)
            .HasDefaultValue(EstadoPresupuesto.Borrador);

        builder.Property(p => p.Subtotal)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(p => p.Descuento)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(p => p.Total)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(p => p.Notas)
            .HasMaxLength(2000);

        builder.Property(p => p.Condiciones)
            .HasMaxLength(1000);

        builder.Property(p => p.EstadoMotivo)
            .HasMaxLength(500);

        builder.HasOne(p => p.Cliente)
            .WithMany(c => c.Presupuestos)
            .HasForeignKey(p => p.ClienteId);

        builder.HasIndex(p => new { p.Estado, p.FechaCreacion });
        builder.HasIndex(p => p.ClienteId);
    }
}
