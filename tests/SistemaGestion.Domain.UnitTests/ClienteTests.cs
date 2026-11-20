using SistemaGestion.Domain.Entities;
using Xunit;

namespace SistemaGestion.Domain.UnitTests;

public sealed class ClienteTests
{
    [Fact]
    public void Crear_NormalizesEmail_AndTrimsFields()
    {
        var cliente = Cliente.Crear(
            "  Maria ",
            " Gonzalez ",
            "  Maria.Gonzalez@Email.COM ",
            telefono: " 11 5555-1234 ");

        Assert.Equal("maria.gonzalez@email.com", cliente.Email);
        Assert.Equal("Maria", cliente.Nombre);
        Assert.Equal("Gonzalez", cliente.Apellido);
        Assert.Equal("11 5555-1234", cliente.Telefono);
        Assert.True(cliente.Activo);
        Assert.Null(cliente.FirebaseUid);
        Assert.Null(cliente.Direccion);
        Assert.Null(cliente.Notas);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sin-arroba")]
    [InlineData("arroba@")]
    public void Crear_WithInvalidEmail_Throws(string email)
        => Assert.Throws<ArgumentException>(() => Cliente.Crear("Maria", "Gonzalez", email));

    [Theory]
    [InlineData("", "Gonzalez")]
    [InlineData("Maria", "")]
    public void Crear_WithoutRequiredName_Throws(string nombre, string apellido)
        => Assert.Throws<ArgumentException>(() => Cliente.Crear(nombre, apellido, "maria@mail.com"));

    [Fact]
    public void Crear_WithBlankOptionalFields_StoresNulls()
    {
        var cliente = Cliente.Crear("Maria", "Gonzalez", "maria@mail.com", telefono: "  ", direccion: "");

        Assert.Null(cliente.Telefono);
        Assert.Null(cliente.Direccion);
    }

    [Fact]
    public void Actualizar_PatchOnlyReplacesProvidedFields()
    {
        var cliente = Cliente.Crear("Maria", "Gonzalez", "maria@mail.com", telefono: "115555");

        cliente.Actualizar(apellido: "Perez", direccion: "Av. Siempreviva 742", activo: false);

        Assert.Equal("Maria", cliente.Nombre);
        Assert.Equal("Perez", cliente.Apellido);
        Assert.Equal("maria@mail.com", cliente.Email);
        Assert.Equal("115555", cliente.Telefono);
        Assert.Equal("Av. Siempreviva 742", cliente.Direccion);
        Assert.False(cliente.Activo);
    }

    [Fact]
    public void Actualizar_WithInvalidEmail_Throws()
    {
        var cliente = Cliente.Crear("Maria", "Gonzalez", "maria@mail.com");

        Assert.Throws<ArgumentException>(() => cliente.Actualizar(email: "invalido"));
    }

    [Fact]
    public void Actualizar_WithNullEmail_KeepsCurrentEmail()
    {
        var cliente = Cliente.Crear("Maria", "Gonzalez", "maria@mail.com");

        cliente.Actualizar(email: null, nombre: "Ana");

        Assert.Equal("maria@mail.com", cliente.Email);
        Assert.Equal("Ana", cliente.Nombre);
    }

    [Fact]
    public void Desactivar_SetsInactiveWithoutRemovingData()
    {
        var cliente = Cliente.Crear("Maria", "Gonzalez", "maria@mail.com");

        cliente.Desactivar();

        Assert.False(cliente.Activo);
        Assert.Equal("maria@mail.com", cliente.Email);
    }

    [Fact]
    public void VincularFirebaseUid_StoresUid()
    {
        var cliente = Cliente.Crear("Maria", "Gonzalez", "maria@mail.com");

        cliente.VincularFirebaseUid("  firebase-uid-1 ");

        Assert.Equal("firebase-uid-1", cliente.FirebaseUid);
    }

    [Fact]
    public void VincularFirebaseUid_WithoutUid_Throws()
    {
        var cliente = Cliente.Crear("Maria", "Gonzalez", "maria@mail.com");

        Assert.Throws<ArgumentException>(() => cliente.VincularFirebaseUid("  "));
    }
}
