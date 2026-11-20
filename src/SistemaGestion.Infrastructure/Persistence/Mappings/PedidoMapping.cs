using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;

namespace SistemaGestion.Infrastructure.Persistence.Mappings;

public class PedidoMapping : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("Pedidos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Estado)
            .HasDefaultValue(EstadoPedido.Pendiente);

        builder.Property(p => p.Total)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(p => p.FechaCreacion)
            .HasDefaultValueSql("now()");

        // Seguimiento de producción y entrega (BR-076): marcas de transición
        // y datos editables mientras el pedido está activo.
        builder.Property(p => p.Notas)
            .HasMaxLength(Pedido.MaxNotasLength);

        builder.HasOne(p => p.Cliente)
            .WithMany()
            .HasForeignKey(p => p.ClienteId);

        // BR-065: un presupuesto aceptado origina exactamente un pedido; la
        // restricción única en BD es la última barrera contra duplicados.
        builder.HasOne(p => p.Presupuesto)
            .WithOne()
            .HasForeignKey<Pedido>(p => p.PresupuestoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.PresupuestoId).IsUnique();
        builder.HasIndex(p => p.ClienteId);
        builder.HasIndex(p => new { p.Estado, p.FechaCreacion });
    }
}
