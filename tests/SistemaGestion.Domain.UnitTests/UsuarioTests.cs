using SistemaGestion.Domain.Entities;
using Xunit;

namespace SistemaGestion.Domain.UnitTests;

public sealed class UsuarioTests
{
    private const int RolId = 1;

    [Fact]
    public void Crear_NormalizesEmailToLowercase_AndTrimsFields()
    {
        var usuario = Usuario.Crear("  Juan.Perez@Email.COM ", "  Juan ", " Perez ", RolId);

        Assert.Equal("juan.perez@email.com", usuario.Email);
        Assert.Equal("Juan", usuario.Nombre);
        Assert.Equal("Perez", usuario.Apellido);
        Assert.Equal(RolId, usuario.RolId);
        Assert.True(usuario.Activo);
        Assert.Null(usuario.FirebaseUid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sin-arroba")]
    [InlineData("arroba@")]
    public void Crear_WithInvalidEmail_Throws(string email)
        => Assert.Throws<ArgumentException>(() => Usuario.Crear(email, "Juan", "Perez", RolId));

    [Theory]
    [InlineData(null, "Apellido")]
    [InlineData("Juan", null)]
    public void Crear_WithoutRequiredName_Throws(string? nombre, string? apellido)
        => Assert.Throws<ArgumentException>(() => Usuario.Crear("juan@mail.com", nombre!, apellido!, RolId));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Crear_WithoutRole_Throws(int rolId)
        => Assert.Throws<ArgumentException>(() => Usuario.Crear("juan@mail.com", "Juan", "Perez", rolId));

    [Fact]
    public void Actualizar_PatchOnlyReplacesProvidedFields()
    {
        var usuario = Usuario.Crear("juan@mail.com", "Juan", "Perez", RolId);

        usuario.Actualizar(apellido: "Gomez", activo: false);

        Assert.Equal("Juan", usuario.Nombre);
        Assert.Equal("Gomez", usuario.Apellido);
        Assert.Equal("juan@mail.com", usuario.Email);
        Assert.Equal(RolId, usuario.RolId);
        Assert.False(usuario.Activo);
    }

    [Fact]
    public void Actualizar_WithInvalidEmail_Throws()
    {
        var usuario = Usuario.Crear("juan@mail.com", "Juan", "Perez", RolId);

        Assert.Throws<ArgumentException>(() => usuario.Actualizar(email: "no-es-email"));
    }

    [Fact]
    public void Actualizar_ChangesRole()
    {
        var usuario = Usuario.Crear("juan@mail.com", "Juan", "Perez", rolId: 1);

        usuario.Actualizar(rolId: 4);

        Assert.Equal(4, usuario.RolId);
    }

    [Fact]
    public void VincularFirebaseUid_SetsUid()
    {
        var usuario = Usuario.Crear("juan@mail.com", "Juan", "Perez", RolId);

        usuario.VincularFirebaseUid(" firebase-uid-123 ");

        Assert.Equal("firebase-uid-123", usuario.FirebaseUid);
    }
}
