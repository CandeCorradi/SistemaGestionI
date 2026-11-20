using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using Xunit;

namespace SistemaGestion.Domain.UnitTests;

/// <summary>
/// Factura (BR-130/BR-131/BR-141, functional-spec §20): snapshot
/// histórico de cliente y total, dinero exacto, fechas UTC, numeración
/// simulada asignable una sola vez y marcas de documento simulado.
/// </summary>
public sealed class FacturaTests
{
    private static readonly DateTime FechaUtc = new(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);

    private static Factura CrearValida(
        decimal total = 250000m,
        string clienteNombre = "  Ana García  ",
        string? notas = null)
        => Factura.Crear(
            pedidoId: 10,
            clienteId: 7,
            clienteNombre: clienteNombre,
            total: total,
            fechaEmisionUtc: FechaUtc,
            usuarioId: 5,
            notas: notas);

    [Fact]
    public void Crear_WithValidData_StoresSnapshotAndSimulatedFlag_Br141()
    {
        var factura = CrearValida();

        Assert.Equal(10, factura.PedidoId);
        Assert.Equal(7, factura.ClienteId);
        Assert.Equal("Ana García", factura.ClienteNombre);
        Assert.Equal(250000m, factura.Total);
        Assert.Equal(FechaUtc, factura.FechaEmision);
        Assert.Equal(5, factura.UsuarioId);
        Assert.Equal(EstadoFactura.Emitida, factura.Estado);
        Assert.True(factura.EsSimulada);
        Assert.Equal(string.Empty, factura.Numero);
    }

    [Fact]
    public void Crear_WithNotes_TrimsAndStores()
    {
        var factura = CrearValida(notas: "  Factura por entrega  ");

        Assert.Equal("Factura por entrega", factura.Notas);
    }

    [Fact]
    public void Crear_WithoutNotes_AllowsNull()
    {
        var factura = CrearValida();

        Assert.Null(factura.Notas);
    }

    [Fact]
    public void Crear_InvalidPedidoId_Throws()
        => Assert.Throws<ArgumentException>(() => Factura.Crear(
            0, 7, "Ana", 100m, FechaUtc, 5, null));

    [Fact]
    public void Crear_InvalidClienteId_Throws()
        => Assert.Throws<ArgumentException>(() => Factura.Crear(
            10, 0, "Ana", 100m, FechaUtc, 5, null));

    [Fact]
    public void Crear_EmptyCustomerName_Throws()
        => Assert.Throws<ArgumentException>(() => Factura.Crear(
            10, 7, "   ", 100m, FechaUtc, 5, null));

    [Fact]
    public void Crear_TooLongCustomerName_Throws()
        => Assert.Throws<ArgumentException>(() => Factura.Crear(
            10, 7, new string('x', Factura.MaxClienteNombreLength + 1),
            100m, FechaUtc, 5, null));

    [Fact]
    public void Crear_ZeroTotal_Throws()
        => Assert.Throws<ArgumentException>(() => Factura.Crear(
            10, 7, "Ana", 0m, FechaUtc, 5, null));

    [Fact]
    public void Crear_NegativeTotal_Throws()
        => Assert.Throws<ArgumentException>(() => Factura.Crear(
            10, 7, "Ana", -100m, FechaUtc, 5, null));

    [Fact]
    public void Crear_TotalWithThreeDecimals_Throws()
        => Assert.Throws<ArgumentException>(() => Factura.Crear(
            10, 7, "Ana", 100.123m, FechaUtc, 5, null));

    [Fact]
    public void Crear_CentsTotal_IsAccepted()
    {
        var factura = CrearValida(total: 100.55m);

        Assert.Equal(100.55m, factura.Total);
    }

    [Fact]
    public void Crear_NonUtcDate_Throws()
        => Assert.Throws<ArgumentException>(() => Factura.Crear(
            10, 7, "Ana", 100m,
            new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Local), 5, null));

    [Fact]
    public void Crear_InvalidUsuarioId_Throws()
        => Assert.Throws<ArgumentException>(() => Factura.Crear(
            10, 7, "Ana", 100m, FechaUtc, 0, null));

    [Fact]
    public void Crear_TooLongNotes_Throws()
        => Assert.Throws<ArgumentException>(() => Factura.Crear(
            10, 7, "Ana", 100m, FechaUtc, 5,
            new string('x', Factura.MaxNotasLength + 1)));

    [Fact]
    public void AsignarDocumento_WithValidData_StoresNumberAndSimulatedFlag_Br130()
    {
        var factura = CrearValida();

        factura.AsignarDocumento("  SIM-000042  ", esSimulada: true);

        Assert.Equal("SIM-000042", factura.Numero);
        Assert.True(factura.EsSimulada);
    }

    [Fact]
    public void AsignarDocumento_NotSimulated_ClearsFlag()
    {
        var factura = CrearValida();

        factura.AsignarDocumento("A-0001", esSimulada: false);

        Assert.False(factura.EsSimulada);
    }

    [Fact]
    public void AsignarDocumento_EmptyNumber_Throws()
    {
        var factura = CrearValida();

        Assert.Throws<ArgumentException>(
            () => factura.AsignarDocumento("   ", esSimulada: true));
    }

    [Fact]
    public void AsignarDocumento_TooLongNumber_Throws()
    {
        var factura = CrearValida();

        Assert.Throws<ArgumentException>(() => factura.AsignarDocumento(
            new string('x', Factura.MaxNumeroLength + 1), esSimulada: true));
    }

    [Fact]
    public void AsignarDocumento_Twice_Throws()
    {
        var factura = CrearValida();
        factura.AsignarDocumento("SIM-000001", esSimulada: true);

        Assert.Throws<InvalidOperationException>(
            () => factura.AsignarDocumento("SIM-000002", esSimulada: true));
        Assert.Equal("SIM-000001", factura.Numero);
    }
}
