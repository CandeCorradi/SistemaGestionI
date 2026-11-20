using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;

namespace SistemaGestion.Infrastructure.Persistence.Mappings;

public class PedidoHistorialMapping : IEntityTypeConfiguration<PedidoHistorial>
{
    public void Configure(EntityTypeBuilder<PedidoHistorial> builder)
    {
        builder.ToTable("PedidoHistoriales");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Comentario).HasMaxLength(500);

        builder.Property(h => h.FechaUtc).HasDefaultValueSql("now()");

        // BR-075: el historial es inmutable; no se puede borrar por borrar
        // el pedido o el usuario (soft delete en usuarios).
        builder.HasOne(h => h.Pedido)
            .WithMany()
            .HasForeignKey(h => h.PedidoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Usuario)
            .WithMany()
            .HasForeignKey(h => h.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => new { h.PedidoId, h.FechaUtc });
        builder.HasIndex(h => h.UsuarioId);
    }
}
