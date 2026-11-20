using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using Xunit;

namespace SistemaGestion.Domain.UnitTests;

public sealed class MovimientoStockTests
{
    private static readonly DateTime FechaUtc = new(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(TipoMovimientoStock.Ingreso)]
    [InlineData(TipoMovimientoStock.Devolucion)]
    [InlineData(TipoMovimientoStock.Liberacion)]
    public void Crear_TiposQueSuman_AumentanStock(TipoMovimientoStock tipo)
    {
        var movimiento = MovimientoStock.Crear(
            productoId: 1,
            tipo: tipo,
            cantidad: 5,
            stockAnterior: 10,
            usuarioId: 7,
            fechaUtc: FechaUtc);

        Assert.Equal(10, movimiento.StockAnterior);
        Assert.Equal(15, movimiento.StockPosterior);
        Assert.Equal(5, movimiento.Cantidad);
    }

    [Theory]
    [InlineData(TipoMovimientoStock.Egreso)]
    [InlineData(TipoMovimientoStock.Reserva)]
    public void Crear_TiposQueRestan_DisminuyenStock(TipoMovimientoStock tipo)
    {
        var movimiento = MovimientoStock.Crear(
            productoId: 1,
            tipo: tipo,
            cantidad: 4,
            stockAnterior: 10,
            usuarioId: null,
            fechaUtc: FechaUtc);

        Assert.Equal(6, movimiento.StockPosterior);
    }

    [Fact]
    public void Crear_AjustePositivo_SumaStock()
    {
        var movimiento = MovimientoStock.Crear(
            1, TipoMovimientoStock.Ajuste, cantidad: 3, stockAnterior: 10,
            usuarioId: 7, fechaUtc: FechaUtc);

        Assert.Equal(13, movimiento.StockPosterior);
    }

    [Fact]
    public void Crear_AjusteNegativo_RestaStock()
    {
        var movimiento = MovimientoStock.Crear(
            1, TipoMovimientoStock.Ajuste, cantidad: -3, stockAnterior: 10,
            usuarioId: 7, fechaUtc: FechaUtc);

        Assert.Equal(7, movimiento.StockPosterior);
        Assert.Equal(-3, movimiento.Cantidad);
    }

    [Fact]
    public void Crear_ConStockInsuficiente_LanzaParaEvitarStockNegativo()
    {
        // BR-052: el stock resultante no puede ser negativo.
        var excepcion = Assert.Throws<ArgumentException>(() =>
            MovimientoStock.Crear(
                1, TipoMovimientoStock.Egreso, cantidad: 30, stockAnterior: 10,
                usuarioId: null, fechaUtc: FechaUtc));

        Assert.Contains("BR-052", excepcion.Message);
    }

    [Fact]
    public void Crear_StockPosteriorCero_EstaPermitido()
    {
        var movimiento = MovimientoStock.Crear(
            1, TipoMovimientoStock.Egreso, cantidad: 10, stockAnterior: 10,
            usuarioId: null, fechaUtc: FechaUtc);

        Assert.Equal(0, movimiento.StockPosterior);
    }

    [Theory]
    [InlineData(TipoMovimientoStock.Ingreso)]
    [InlineData(TipoMovimientoStock.Egreso)]
    [InlineData(TipoMovimientoStock.Devolucion)]
    [InlineData(TipoMovimientoStock.Reserva)]
    [InlineData(TipoMovimientoStock.Liberacion)]
    public void Crear_CantidadNoPositiva_LanzaEnTiposDeValorAbsoluto(TipoMovimientoStock tipo)
    {
        Assert.Throws<ArgumentException>(() =>
            MovimientoStock.Crear(
                1, tipo, cantidad: 0, stockAnterior: 10,
                usuarioId: null, fechaUtc: FechaUtc));
    }

    [Fact]
    public void Crear_AjusteCero_Lanza()
    {
        Assert.Throws<ArgumentException>(() =>
            MovimientoStock.Crear(
                1, TipoMovimientoStock.Ajuste, cantidad: 0, stockAnterior: 10,
                usuarioId: null, fechaUtc: FechaUtc));
    }

    [Fact]
    public void Crear_TipoNoDefinido_Lanza()
    {
        Assert.Throws<ArgumentException>(() =>
            MovimientoStock.Crear(
                1, (TipoMovimientoStock)99, cantidad: 1, stockAnterior: 10,
                usuarioId: null, fechaUtc: FechaUtc));
    }

    [Fact]
    public void Crear_ProductoInvalido_Lanza()
    {
        Assert.Throws<ArgumentException>(() =>
            MovimientoStock.Crear(
                0, TipoMovimientoStock.Ingreso, cantidad: 1, stockAnterior: 10,
                usuarioId: null, fechaUtc: FechaUtc));
    }

    [Fact]
    public void Crear_FechaNoUtc_Lanza()
    {
        var local = new DateTime(2026, 10, 2, 9, 0, 0, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() =>
            MovimientoStock.Crear(
                1, TipoMovimientoStock.Ingreso, cantidad: 1, stockAnterior: 10,
                usuarioId: null, fechaUtc: local));
    }

    [Fact]
    public void Crear_ConMotivoYReferencia_PersisteDatosDeHistorial()
    {
        var movimiento = MovimientoStock.Crear(
            1, TipoMovimientoStock.Ingreso, cantidad: 5, stockAnterior: 0,
            usuarioId: 7, fechaUtc: FechaUtc,
            motivo: "  Recepción de pedido  ", referencia: " PO-2026-001 ");

        Assert.Equal("Recepción de pedido", movimiento.Motivo);
        Assert.Equal("PO-2026-001", movimiento.Referencia);
        Assert.Equal(7, movimiento.UsuarioId);
        Assert.Equal(FechaUtc, movimiento.Fecha);
    }
}
