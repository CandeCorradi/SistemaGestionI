using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Infrastructure.Persistence.Mappings;

public class FacturaMapping : IEntityTypeConfiguration<Factura>
{
    public void Configure(EntityTypeBuilder<Factura> builder)
    {
        builder.ToTable("Facturas");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Numero).HasMaxLength(Factura.MaxNumeroLength);

        builder.Property(f => f.ClienteNombre)
            .HasMaxLength(Factura.MaxClienteNombreLength);

        builder.Property(f => f.Notas).HasMaxLength(Factura.MaxNotasLength);

        builder.Property(f => f.Total).HasPrecision(18, 2);

        builder.Property(f => f.FechaEmision).HasDefaultValueSql("now()");

        // BR-141/functional-spec §20: la factura preserva el histórico
        // comercial; las FK son RESTRICT para que no se borre el documento
        // al borrar pedido, cliente o usuario.
        builder.HasOne(f => f.Pedido)
            .WithMany()
            .HasForeignKey(f => f.PedidoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Cliente)
            .WithMany()
            .HasForeignKey(f => f.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Usuario)
            .WithMany()
            .HasForeignKey(f => f.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Una sola factura por pedido: la restricción de unicidad resuelve
        // la carrera entre dos emisiones concurrentes (409).
        builder.HasIndex(f => f.PedidoId).IsUnique();

        builder.HasIndex(f => f.Numero);
        builder.HasIndex(f => f.ClienteId);
        builder.HasIndex(f => f.FechaEmision);
        builder.HasIndex(f => f.UsuarioId);
    }
}
