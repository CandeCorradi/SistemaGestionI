using SistemaGestion.Domain.Entities;
using Xunit;

namespace SistemaGestion.Domain.UnitTests;

public sealed class ProveedorTests
{
    [Fact]
    public void Crear_WithValidData_StoresFields_AndTrims()
    {
        var proveedor = Proveedor.Crear(
            "  Melaminas del Sur ",
            contacto: " Carlos Ruiz ",
            email: "  Ventas@MelaminasDelSur.COM ",
            telefono: " 11 4444-5555 ");

        Assert.Equal("Melaminas del Sur", proveedor.Nombre);
        Assert.Equal("Carlos Ruiz", proveedor.Contacto);
        Assert.Equal("ventas@melaminasdelsur.com", proveedor.Email);
        Assert.Equal("11 4444-5555", proveedor.Telefono);
        Assert.True(proveedor.Activo);
        Assert.Null(proveedor.Direccion);
        Assert.Null(proveedor.Notas);
    }

    [Fact]
    public void Crear_WithoutEmail_IsAllowed()
    {
        var proveedor = Proveedor.Crear("Accesorios SA");

        Assert.Null(proveedor.Email);
        Assert.True(proveedor.Activo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_WithoutNombre_Throws(string nombre)
        => Assert.Throws<ArgumentException>(() => Proveedor.Crear(nombre));

    [Theory]
    [InlineData("sin-arroba")]
    [InlineData("arroba@")]
    public void Crear_WithInvalidEmail_Throws(string email)
        => Assert.Throws<ArgumentException>(() => Proveedor.Crear("Proveedor SA", email: email));

    [Fact]
    public void Crear_WithBlankOptionalFields_StoresNulls()
    {
        var proveedor = Proveedor.Crear("Proveedor SA", contacto: " ", email: "");

        Assert.Null(proveedor.Contacto);
        Assert.Null(proveedor.Email);
    }

    [Fact]
    public void Actualizar_PatchOnlyReplacesProvidedFields()
    {
        var proveedor = Proveedor.Crear("Proveedor SA", telefono: "115555");

        proveedor.Actualizar(contacto: "Maria Sosa", direccion: "Ruta 8 km 40", activo: false);

        Assert.Equal("Proveedor SA", proveedor.Nombre);
        Assert.Equal("115555", proveedor.Telefono);
        Assert.Equal("Maria Sosa", proveedor.Contacto);
        Assert.Equal("Ruta 8 km 40", proveedor.Direccion);
        Assert.False(proveedor.Activo);
    }

    [Fact]
    public void Actualizar_WithEmptyEmail_ClearsEmail()
    {
        var proveedor = Proveedor.Crear("Proveedor SA", email: "viejo@mail.com");

        proveedor.Actualizar(email: "");

        Assert.Null(proveedor.Email);
    }

    [Fact]
    public void Actualizar_WithInvalidEmail_Throws()
    {
        var proveedor = Proveedor.Crear("Proveedor SA");

        Assert.Throws<ArgumentException>(() => proveedor.Actualizar(email: "invalido"));
    }

    [Fact]
    public void Actualizar_WithNullEmail_KeepsCurrentEmail()
    {
        var proveedor = Proveedor.Crear("Proveedor SA", email: "actual@mail.com");

        proveedor.Actualizar(email: null, nombre: "Otro Nombre");

        Assert.Equal("actual@mail.com", proveedor.Email);
        Assert.Equal("Otro Nombre", proveedor.Nombre);
    }

    [Fact]
    public void Desactivar_SetsInactiveWithoutRemovingData()
    {
        var proveedor = Proveedor.Crear("Proveedor SA", email: "contacto@mail.com");

        proveedor.Desactivar();

        Assert.False(proveedor.Activo);
        Assert.Equal("Proveedor SA", proveedor.Nombre);
        Assert.Equal("contacto@mail.com", proveedor.Email);
    }
}
