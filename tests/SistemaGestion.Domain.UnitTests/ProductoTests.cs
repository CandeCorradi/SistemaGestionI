using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using Xunit;

namespace SistemaGestion.Domain.UnitTests;

public sealed class ProductoTests
{
    [Fact]
    public void Crear_WithValidData_StoresFields_AndNormalizesSku()
    {
        var producto = Producto.Crear(
            "  Melamina Blanca 18mm ",
            codigoSku: "  mel-001  ",
            tipo: TipoProducto.Melamina,
            precio: 1500.50m,
            categoria: " Melaminas ",
            costo: 900m,
            stockActual: 25,
            stockMinimo: 5,
            showroom: true);

        Assert.Equal("Melamina Blanca 18mm", producto.Nombre);
        Assert.Equal("MEL-001", producto.CodigoSku);
        Assert.Equal(TipoProducto.Melamina, producto.Tipo);
        Assert.Equal(1500.50m, producto.Precio);
        Assert.Equal(900m, producto.Costo);
        Assert.Equal("Melaminas", producto.Categoria);
        Assert.Equal(25, producto.StockActual);
        Assert.Equal(5, producto.StockMinimo);
        Assert.True(producto.Showroom);
        Assert.True(producto.Activo);
        Assert.Null(producto.ProveedorId);
        Assert.False(producto.EsPersonalizado);
    }

    [Fact]
    public void Crear_WithEsPersonalizado_StoresCustomFlag()
    {
        var producto = Producto.Crear(
            "Mesa a medida",
            "SKU-CUST-1",
            TipoProducto.Mueble,
            100m,
            esPersonalizado: true);

        Assert.True(producto.EsPersonalizado);
    }

    [Fact]
    public void Actualizar_EsPersonalizado_UpdatesFlag()
    {
        var producto = Producto.Crear("Mesa", "SKU-1", TipoProducto.Mueble, 100m);

        producto.Actualizar(esPersonalizado: true);
        Assert.True(producto.EsPersonalizado);

        producto.Actualizar(esPersonalizado: false);
        Assert.False(producto.EsPersonalizado);
    }

    [Fact]
    public void Actualizar_WithoutEsPersonalizado_KeepsCurrentValue()
    {
        var producto = Producto.Crear(
            "Mesa a medida", "SKU-2", TipoProducto.Mueble, 100m, esPersonalizado: true);

        producto.Actualizar(nombre: "Mesa nueva");

        Assert.True(producto.EsPersonalizado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_WithoutNombre_Throws(string nombre)
        => Assert.Throws<ArgumentException>(
            () => Producto.Crear(nombre, "SKU-1", TipoProducto.Mueble, 100m));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_WithoutSku_Throws(string sku)
        => Assert.Throws<ArgumentException>(
            () => Producto.Crear("Mesa", sku, TipoProducto.Mueble, 100m));

    [Fact]
    public void Crear_WithNegativePrecio_Throws()
        => Assert.Throws<ArgumentException>(
            () => Producto.Crear("Mesa", "SKU-1", TipoProducto.Mueble, -1m));

    [Fact]
    public void Crear_WithNegativeCosto_Throws()
        => Assert.Throws<ArgumentException>(
            () => Producto.Crear("Mesa", "SKU-1", TipoProducto.Mueble, 100m, costo: -5m));

    [Fact]
    public void Crear_WithNegativeStock_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => Producto.Crear("Mesa", "SKU-1", TipoProducto.Mueble, 100m, stockActual: -1));
        Assert.Throws<ArgumentException>(
            () => Producto.Crear("Mesa", "SKU-1", TipoProducto.Mueble, 100m, stockMinimo: -1));
    }

    [Fact]
    public void Actualizar_PatchOnlyReplacesProvidedFields()
    {
        var producto = Producto.Crear(
            "Mesa", "SKU-1", TipoProducto.Mueble, 100m, categoria: "Mesas", stockMinimo: 2);

        producto.Actualizar(precio: 250.75m, showroom: true, tipo: TipoProducto.Otro);

        Assert.Equal("Mesa", producto.Nombre);
        Assert.Equal("SKU-1", producto.CodigoSku);
        Assert.Equal("Mesas", producto.Categoria);
        Assert.Equal(2, producto.StockMinimo);
        Assert.Equal(250.75m, producto.Precio);
        Assert.True(producto.Showroom);
        Assert.Equal(TipoProducto.Otro, producto.Tipo);
    }

    [Fact]
    public void Actualizar_WithNegativePrecio_Throws()
    {
        var producto = Producto.Crear("Mesa", "SKU-1", TipoProducto.Mueble, 100m);

        Assert.Throws<ArgumentException>(() => producto.Actualizar(precio: -1m));
    }

    [Fact]
    public void Actualizar_WithBlankDescripcion_ClearsField()
    {
        var producto = Producto.Crear(
            "Mesa", "SKU-1", TipoProducto.Mueble, 100m, descripcion: "Descripción vieja");

        producto.Actualizar(descripcion: " ");

        Assert.Null(producto.Descripcion);
    }

    [Fact]
    public void Actualizar_WithNullField_KeepsCurrentValue()
    {
        var producto = Producto.Crear("Mesa", "SKU-1", TipoProducto.Mueble, 100m);

        producto.Actualizar(nombre: null, precio: null);

        Assert.Equal("Mesa", producto.Nombre);
        Assert.Equal(100m, producto.Precio);
    }

    [Fact]
    public void Actualizar_ChangingPrecio_DoesNotAffectOtherProducts()
    {
        var historico = Producto.Crear("Mesa", "SKU-1", TipoProducto.Mueble, 100m);
        var actual = Producto.Crear("Mesa", "SKU-1", TipoProducto.Mueble, 100m);

        actual.Actualizar(precio: 300m);

        Assert.Equal(100m, historico.Precio);
        Assert.Equal(300m, actual.Precio);
    }

    [Fact]
    public void Desactivar_SetsInactiveWithoutRemovingData()
    {
        var producto = Producto.Crear("Mesa", "SKU-1", TipoProducto.Mueble, 100m);

        producto.Desactivar();

        Assert.False(producto.Activo);
        Assert.Equal("Mesa", producto.Nombre);
        Assert.Equal("SKU-1", producto.CodigoSku);
    }
}
