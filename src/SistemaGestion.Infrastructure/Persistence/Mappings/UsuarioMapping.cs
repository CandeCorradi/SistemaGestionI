using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Infrastructure.Persistence.Mappings;

public class UsuarioMapping : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(u => u.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Apellido)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.FirebaseUid)
            .HasMaxLength(128);

        builder.Property(u => u.FechaCreacion)
            .IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();

        builder.HasIndex(u => u.FirebaseUid).IsUnique();

        // BR-001 / roadmap §10.2: cada usuario tiene exactamente un rol fijo.
        builder.HasOne<Rol>()
            .WithMany()
            .HasForeignKey(u => u.RolId)
            .IsRequired();
    }
}
