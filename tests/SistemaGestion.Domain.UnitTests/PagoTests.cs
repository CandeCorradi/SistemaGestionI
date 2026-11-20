using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using Xunit;

namespace SistemaGestion.Domain.UnitTests;

/// <summary>
/// Pago (BR-080/BR-082, functional-spec §17): monto positivo con
/// exactitud monetaria, fechas UTC y límites de longitud.
/// </summary>
public sealed class PagoTests
{
    private static readonly DateTime FechaUtc = new(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Crear_WithValidData_StoresFields()
    {
        var pago = Pago.Crear(
            pedidoId: 10,
            monto: 250000m,
            fechaUtc: FechaUtc,
            metodo: MetodoPago.Transferencia,
            referencia: "  TRX-12345  ",
            notas: " Señia del presupuesto ",
            usuarioId: 5);

        Assert.Equal(10, pago.PedidoId);
        Assert.Equal(250000m, pago.Monto);
        Assert.Equal(FechaUtc, pago.FechaPago);
        Assert.Equal(MetodoPago.Transferencia, pago.Metodo);
        Assert.Equal("TRX-12345", pago.Referencia);
        Assert.Equal("Señia del presupuesto", pago.Notas);
        Assert.Equal(5, pago.UsuarioId);
    }

    [Fact]
    public void Crear_WithoutReferenceOrNotes_AllowsNulls()
    {
        var pago = Pago.Crear(10, 100m, FechaUtc, MetodoPago.Efectivo, null, null, usuarioId: null);

        Assert.Null(pago.Referencia);
        Assert.Null(pago.Notas);
        Assert.Null(pago.UsuarioId);
    }

    [Fact]
    public void Crear_ZeroAmount_Throws_Br080()
        => Assert.Throws<ArgumentException>(() => Pago.Crear(
            10, 0m, FechaUtc, MetodoPago.Efectivo, null, null, null));

    [Fact]
    public void Crear_NegativeAmount_Throws_Br080()
        => Assert.Throws<ArgumentException>(() => Pago.Crear(
            10, -500m, FechaUtc, MetodoPago.Efectivo, null, null, null));

    [Fact]
    public void Crear_AmountWithThreeDecimals_Throws()
        => Assert.Throws<ArgumentException>(() => Pago.Crear(
            10, 100.123m, FechaUtc, MetodoPago.Efectivo, null, null, null));

    [Fact]
    public void Crear_CentsAmount_IsAccepted()
    {
        var pago = Pago.Crear(10, 100.55m, FechaUtc, MetodoPago.Efectivo, null, null, null);

        Assert.Equal(100.55m, pago.Monto);
    }

    [Fact]
    public void Crear_NonUtcDate_Throws()
        => Assert.Throws<ArgumentException>(() => Pago.Crear(
            10, 100m, new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Local),
            MetodoPago.Efectivo, null, null, null));

    [Fact]
    public void Crear_InvalidPedidoId_Throws()
        => Assert.Throws<ArgumentException>(() => Pago.Crear(
            0, 100m, FechaUtc, MetodoPago.Efectivo, null, null, null));

    [Fact]
    public void Crear_UndefinedMethod_Throws()
        => Assert.Throws<ArgumentException>(() => Pago.Crear(
            10, 100m, FechaUtc, (MetodoPago)99, null, null, null));

    [Fact]
    public void Crear_NegativeUsuarioId_Throws()
        => Assert.Throws<ArgumentException>(() => Pago.Crear(
            10, 100m, FechaUtc, MetodoPago.Efectivo, null, null, usuarioId: -1));

    [Fact]
    public void Crear_TooLongReference_Throws()
        => Assert.Throws<ArgumentException>(() => Pago.Crear(
            10, 100m, FechaUtc, MetodoPago.Efectivo,
            new string('x', Pago.MaxReferenciaLength + 1), null, null));

    [Fact]
    public void Crear_TooLongNotes_Throws()
        => Assert.Throws<ArgumentException>(() => Pago.Crear(
            10, 100m, FechaUtc, MetodoPago.Efectivo, null,
            new string('x', Pago.MaxNotasLength + 1), null));

    [Fact]
    public void Crear_MaxLengthReferenceAndNotes_AreAccepted()
    {
        var pago = Pago.Crear(
            10, 100m, FechaUtc, MetodoPago.Otro,
            new string('r', Pago.MaxReferenciaLength),
            new string('n', Pago.MaxNotasLength),
            null);

        Assert.Equal(Pago.MaxReferenciaLength, pago.Referencia!.Length);
        Assert.Equal(Pago.MaxNotasLength, pago.Notas!.Length);
    }

    [Fact]
    public void Crear_TrimsWhitespace()
    {
        var pago = Pago.Crear(
            10, 100m, FechaUtc, MetodoPago.Efectivo, "   TRX-1   ", "   nota   ", null);

        Assert.Equal("TRX-1", pago.Referencia);
        Assert.Equal("nota", pago.Notas);
    }
}
