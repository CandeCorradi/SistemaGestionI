using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Infrastructure.Persistence.Mappings;

/// <summary>
/// Mapeo de movimientos de inventario (tabla MovimientosStock). FK con
/// DeleteBehavior.Restrict para preservar el historial (AGENTS.md §57:
/// nunca eliminar físicamente información histórica crítica).
/// </summary>
public sealed class MovimientoStockMapping : IEntityTypeConfiguration<MovimientoStock>
{
    public void Configure(EntityTypeBuilder<MovimientoStock> builder)
    {
        builder.ToTable("MovimientosStock");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ProductoId).IsRequired();
        builder.Property(m => m.Cantidad).IsRequired();
        builder.Property(m => m.StockAnterior).IsRequired();
        builder.Property(m => m.StockPosterior).IsRequired();
        builder.Property(m => m.Motivo).HasMaxLength(500);
        builder.Property(m => m.Referencia).HasMaxLength(100);
        builder.Property(m => m.Fecha).IsRequired();

        builder.HasOne(m => m.Producto)
            .WithMany()
            .HasForeignKey(m => m.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Usuario)
            .WithMany()
            .HasForeignKey(m => m.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.ProductoId, m.Fecha });
        builder.HasIndex(m => m.Fecha);
    }
}
