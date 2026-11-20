using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Infrastructure.Persistence.Mappings;

/// <summary>
/// Persistencia de imágenes de trabajos realizados: sólo referencias al
/// storage externo (BR-101: nunca binarios en la base de datos). FK a
/// TrabajoRealizado con borrado en cascada para no dejar huérfanos
/// (AGENTS.md §24).
/// </summary>
public class TrabajoRealizadoImagenMapping : IEntityTypeConfiguration<TrabajoRealizadoImagen>
{
    public void Configure(EntityTypeBuilder<TrabajoRealizadoImagen> builder)
    {
        builder.ToTable("TrabajoRealizadoImagenes");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Path)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(i => i.Orden)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(i => i.FechaCreacion)
            .IsRequired();

        builder.HasIndex(i => new { i.TrabajoRealizadoId, i.Orden });

        builder.HasOne<TrabajoRealizado>()
            .WithMany()
            .HasForeignKey(i => i.TrabajoRealizadoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
