using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Infrastructure.Persistence.Mappings;

public class PagoMapping : IEntityTypeConfiguration<Pago>
{
    public void Configure(EntityTypeBuilder<Pago> builder)
    {
        builder.ToTable("Pagos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Monto).HasPrecision(18, 2);

        builder.Property(p => p.FechaPago).HasDefaultValueSql("now()");

        builder.Property(p => p.Referencia).HasMaxLength(Pago.MaxReferenciaLength);

        builder.Property(p => p.Notas).HasMaxLength(Pago.MaxNotasLength);

        // BR-082: el historial de pagos es inmutable; no se borra por borrar
        // el pedido o el usuario (soft delete en usuarios).
        builder.HasOne(p => p.Pedido)
            .WithMany()
            .HasForeignKey(p => p.PedidoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Usuario)
            .WithMany()
            .HasForeignKey(p => p.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.PedidoId);
        builder.HasIndex(p => p.FechaPago);
        builder.HasIndex(p => new { p.PedidoId, p.FechaPago });
        builder.HasIndex(p => p.UsuarioId);
    }
}
