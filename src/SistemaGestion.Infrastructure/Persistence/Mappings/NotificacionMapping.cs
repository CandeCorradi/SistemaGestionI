using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Infrastructure.Persistence.Mappings;

public class NotificacionMapping : IEntityTypeConfiguration<Notificacion>
{
    public void Configure(EntityTypeBuilder<Notificacion> builder)
    {
        builder.ToTable("Notificaciones");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Titulo).HasMaxLength(Notificacion.MaxTituloLength);
        builder.Property(n => n.Mensaje).HasMaxLength(Notificacion.MaxMensajeLength);
        builder.Property(n => n.EntidadTipo).HasMaxLength(Notificacion.MaxEntidadTipoLength);

        builder.Property(n => n.FechaCreacionUtc).HasDefaultValueSql("now()");

        // BR-090: la notificación pertenece a un usuario; el soft delete de
        // usuarios no debe arrastrar notificaciones históricas.
        builder.HasOne(n => n.Usuario)
            .WithMany()
            .HasForeignKey(n => n.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(n => new { n.UsuarioId, n.Leida, n.FechaCreacionUtc });
    }
}
