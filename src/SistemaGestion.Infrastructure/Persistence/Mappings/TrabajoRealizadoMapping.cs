using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Infrastructure.Persistence.Mappings;

/// <summary>
/// Persistencia de trabajos realizados / vidriera (AGENTS.md §23).
/// Portfolio de la carpintería: sin precios ni stock. Las etiquetas se
/// guardan como array de texto de PostgreSQL; el flag
/// <c>PublicarNombreCliente</c> gobierna la privacidad en el showroom
/// público (BR-191).
/// </summary>
public class TrabajoRealizadoMapping : IEntityTypeConfiguration<TrabajoRealizado>
{
    public void Configure(EntityTypeBuilder<TrabajoRealizado> builder)
    {
        builder.ToTable("TrabajosRealizados");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Nombre)
            .IsRequired()
            .HasMaxLength(TrabajoRealizado.MaxNombreLength);

        builder.Property(t => t.TipoMueble)
            .HasMaxLength(100);

        builder.Property(t => t.ClienteNombre)
            .HasMaxLength(200);

        builder.Property(t => t.PublicarNombreCliente)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.Fecha)
            .HasColumnType("date");

        builder.Property(t => t.Descripcion)
            .HasMaxLength(TrabajoRealizado.MaxTextoLength);

        builder.Property(t => t.Medidas)
            .HasMaxLength(200);

        builder.Property(t => t.Melamina)
            .HasMaxLength(200);

        builder.Property(t => t.Accesorios)
            .HasMaxLength(200);

        builder.Property(t => t.Categoria)
            .HasMaxLength(100);

        builder.Property(t => t.Tags)
            .HasColumnType("text[]")
            .IsRequired();

        builder.Property(t => t.Destacado)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.Publicado)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.FechaCreacion)
            .IsRequired();

        builder.HasIndex(t => t.Publicado);
        builder.HasIndex(t => t.Categoria);
        builder.HasIndex(t => new { t.Publicado, t.Destacado });
    }
}
