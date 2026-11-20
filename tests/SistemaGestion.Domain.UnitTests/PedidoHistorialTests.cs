using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using Xunit;

namespace SistemaGestion.Domain.UnitTests;

/// <summary>
/// Historial inmutable de estados de pedido (BR-075, functional-spec §16).
/// </summary>
public sealed class PedidoHistorialTests
{
    private static readonly DateTime FechaUtc = new(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Crear_WithValidData_StoresFields()
    {
        var historial = PedidoHistorial.Crear(
            pedidoId: 10,
            estadoAnterior: EstadoPedido.Pendiente,
            estadoNuevo: EstadoPedido.Confirmado,
            fechaUtc: FechaUtc,
            usuarioId: 5,
            comentario: "  Cliente confirmó por teléfono  ");

        Assert.Equal(10, historial.PedidoId);
        Assert.Equal(EstadoPedido.Pendiente, historial.EstadoAnterior);
        Assert.Equal(EstadoPedido.Confirmado, historial.EstadoNuevo);
        Assert.Equal(FechaUtc, historial.FechaUtc);
        Assert.Equal(5, historial.UsuarioId);
        Assert.Equal("Cliente confirmó por teléfono", historial.Comentario);
    }

    [Fact]
    public void Crear_CreationRecord_AllowsNullPreviousStatus()
    {
        var historial = PedidoHistorial.Crear(
            10, estadoAnterior: null, estadoNuevo: EstadoPedido.Pendiente,
            fechaUtc: FechaUtc);

        Assert.Null(historial.EstadoAnterior);
        Assert.Equal(EstadoPedido.Pendiente, historial.EstadoNuevo);
        Assert.Null(historial.UsuarioId);
        Assert.Null(historial.Comentario);
    }

    [Fact]
    public void Crear_BlankComment_BecomesNull()
    {
        var historial = PedidoHistorial.Crear(
            10, EstadoPedido.Pendiente, EstadoPedido.Confirmado, FechaUtc,
            comentario: "   ");

        Assert.Null(historial.Comentario);
    }

    [Fact]
    public void Crear_LongComment_IsTruncatedTo500()
    {
        var historial = PedidoHistorial.Crear(
            10, EstadoPedido.Pendiente, EstadoPedido.Confirmado, FechaUtc,
            comentario: new string('x', 600));

        Assert.Equal(500, historial.Comentario!.Length);
    }

    [Fact]
    public void Crear_SameState_Throws()
        => Assert.Throws<ArgumentException>(() => PedidoHistorial.Crear(
            10, EstadoPedido.Confirmado, EstadoPedido.Confirmado, FechaUtc));

    [Fact]
    public void Crear_NonUtcDate_Throws()
        => Assert.Throws<ArgumentException>(() => PedidoHistorial.Crear(
            10, EstadoPedido.Pendiente, EstadoPedido.Confirmado,
            new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Local)));

    [Fact]
    public void Crear_InvalidPedidoId_Throws()
        => Assert.Throws<ArgumentException>(() => PedidoHistorial.Crear(
            0, EstadoPedido.Pendiente, EstadoPedido.Confirmado, FechaUtc));

    [Fact]
    public void Crear_InvalidUsuarioId_Throws()
        => Assert.Throws<ArgumentException>(() => PedidoHistorial.Crear(
            10, EstadoPedido.Pendiente, EstadoPedido.Confirmado, FechaUtc,
            usuarioId: -1));
}
