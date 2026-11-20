using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Infrastructure.Persistence.Mappings;

/// <summary>
/// Persistencia de clientes (functional-spec §6). Baja lógica con
/// <c>Activo</c> (BR-011); email y FirebaseUid únicos (BR-010).
/// </summary>
public class ClienteMapping : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Apellido)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.Telefono)
            .HasMaxLength(30);

        builder.Property(c => c.Direccion)
            .HasMaxLength(200);

        builder.Property(c => c.Documento)
            .HasMaxLength(30);

        builder.Property(c => c.Notas)
            .HasMaxLength(2000);

        builder.Property(c => c.FirebaseUid)
            .HasMaxLength(128);

        builder.Property(c => c.Activo)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.FechaCreacion)
            .IsRequired();

        builder.HasIndex(c => c.Email).IsUnique();

        // BR-010: el UID del portal es único cuando está vinculado
        // (PostgreSQL permite múltiples NULL en un índice único).
        builder.HasIndex(c => c.FirebaseUid).IsUnique();
    }
}
