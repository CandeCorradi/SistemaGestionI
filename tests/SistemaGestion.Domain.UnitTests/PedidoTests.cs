using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using Xunit;

namespace SistemaGestion.Domain.UnitTests;

/// <summary>
/// Pedido originado en la aceptación (BR-070/BR-071): snapshot del total y
/// estado inicial Pendiente.
/// </summary>
public sealed class PedidoTests
{
    private static readonly DateTime FechaUtc = new(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Crear_WithValidData_StoresSnapshot_InPendiente()
    {
        var pedido = Pedido.Crear(clienteId: 7, presupuestoId: 42, total: 1500.50m, fechaCreacionUtc: FechaUtc);

        Assert.Equal(7, pedido.ClienteId);
        Assert.Equal(42, pedido.PresupuestoId);
        Assert.Equal(1500.50m, pedido.Total);
        Assert.Equal(FechaUtc, pedido.FechaCreacion);
        Assert.Equal(EstadoPedido.Pendiente, pedido.Estado);
    }

    [Fact]
    public void Crear_WithoutCliente_Throws()
        => Assert.Throws<ArgumentException>(() => Pedido.Crear(0, 42, 100m, FechaUtc));

    [Fact]
    public void Crear_WithoutPresupuesto_Throws()
        => Assert.Throws<ArgumentException>(() => Pedido.Crear(7, 0, 100m, FechaUtc));

    [Fact]
    public void Crear_NegativeTotal_Throws()
        => Assert.Throws<ArgumentException>(() => Pedido.Crear(7, 42, -1m, FechaUtc));

    [Fact]
    public void Crear_ZeroTotal_IsAllowed()
    {
        var pedido = Pedido.Crear(7, 42, 0m, FechaUtc);

        Assert.Equal(0m, pedido.Total);
    }

    [Fact]
    public void Crear_NonUtcDate_Throws()
        => Assert.Throws<ArgumentException>(() => Pedido.Crear(
            7, 42, 100m, new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Local)));

    // ------------------------------------------------------------------
    // Ciclo de vida (BR-072/BR-073)
    // ------------------------------------------------------------------

    private static Pedido NuevoPedido(EstadoPedido estado)
    {
        var pedido = Pedido.Crear(7, 42, 100m, FechaUtc);

        if (estado == EstadoPedido.Cancelado)
        {
            pedido.Cancelar();
            return pedido;
        }

        var cadena = new[]
        {
            EstadoPedido.Confirmado, EstadoPedido.EnProduccion,
            EstadoPedido.Listo, EstadoPedido.Entregado, EstadoPedido.Completado,
        };

        foreach (var paso in cadena)
        {
            // Chequear antes de avanzar: para Pendiente no debe moverse.
            if (pedido.Estado == estado)
            {
                break;
            }

            Avanzar(pedido, paso);
        }

        if (pedido.Estado != estado)
        {
            throw new InvalidOperationException(
                $"El helper no pudo llevar el pedido a {estado} (estado actual: {pedido.Estado}).");
        }

        return pedido;
    }

    private static void Avanzar(Pedido pedido, EstadoPedido destino)
    {
        switch (destino)
        {
            case EstadoPedido.Confirmado:
                pedido.Confirmar();
                break;
            case EstadoPedido.EnProduccion:
                pedido.IniciarProduccion();
                break;
            case EstadoPedido.Listo:
                pedido.MarcarListo();
                break;
            case EstadoPedido.Entregado:
                pedido.Entregar();
                break;
            case EstadoPedido.Completado:
                pedido.Completar();
                break;
            default:
                throw new InvalidOperationException("Destino no soportado en el helper.");
        }
    }

    [Fact]
    public void Lifecycle_HappyPath_FollowsBr073()
    {
        var pedido = NuevoPedido(EstadoPedido.Pendiente);

        pedido.Confirmar();
        Assert.Equal(EstadoPedido.Confirmado, pedido.Estado);
        pedido.IniciarProduccion();
        Assert.Equal(EstadoPedido.EnProduccion, pedido.Estado);
        pedido.MarcarListo();
        Assert.Equal(EstadoPedido.Listo, pedido.Estado);
        pedido.Entregar();
        Assert.Equal(EstadoPedido.Entregado, pedido.Estado);
        pedido.Completar();
        Assert.Equal(EstadoPedido.Completado, pedido.Estado);
    }

    [Fact]
    public void Confirmar_FromPendiente_Ok()
    {
        var pedido = NuevoPedido(EstadoPedido.Pendiente);

        pedido.Confirmar();

        Assert.Equal(EstadoPedido.Confirmado, pedido.Estado);
    }

    [Fact]
    public void Confirmar_FromEnProduccion_Throws_Br072()
    {
        var pedido = NuevoPedido(EstadoPedido.EnProduccion);

        var ex = Assert.Throws<InvalidOperationException>(() => pedido.Confirmar());

        Assert.Contains("BR-072", ex.Message);
        Assert.Equal(EstadoPedido.EnProduccion, pedido.Estado);
    }

    [Fact]
    public void IniciarProduccion_FromPendiente_Throws_NoJump_Br072()
    {
        var pedido = NuevoPedido(EstadoPedido.Pendiente);

        Assert.Throws<InvalidOperationException>(() => pedido.IniciarProduccion());
        Assert.Equal(EstadoPedido.Pendiente, pedido.Estado);
    }

    [Fact]
    public void Completar_FromListo_Throws_MustDeliverFirst()
    {
        var pedido = NuevoPedido(EstadoPedido.Listo);

        Assert.Throws<InvalidOperationException>(() => pedido.Completar());
        Assert.Equal(EstadoPedido.Listo, pedido.Estado);
    }

    [Fact]
    public void Cancelar_FromPermittedStates_Ok()
    {
        foreach (var estado in new[]
        {
            EstadoPedido.Pendiente, EstadoPedido.Confirmado,
            EstadoPedido.EnProduccion, EstadoPedido.Listo,
        })
        {
            var pedido = NuevoPedido(estado);

            pedido.Cancelar();

            Assert.Equal(EstadoPedido.Cancelado, pedido.Estado);
        }
    }

    [Fact]
    public void Cancelar_FromEntregado_Throws()
    {
        var pedido = NuevoPedido(EstadoPedido.Entregado);

        Assert.Throws<InvalidOperationException>(() => pedido.Cancelar());
        Assert.Equal(EstadoPedido.Entregado, pedido.Estado);
    }

    [Fact]
    public void TerminalStates_AreImmutable_Br074()
    {
        var completado = NuevoPedido(EstadoPedido.Completado);
        Assert.Throws<InvalidOperationException>(() => completado.Completar());
        Assert.Throws<InvalidOperationException>(() => completado.Cancelar());

        var cancelado = NuevoPedido(EstadoPedido.Listo);
        cancelado.Cancelar();
        Assert.Throws<InvalidOperationException>(() => cancelado.Confirmar());
        Assert.Throws<InvalidOperationException>(() => cancelado.Cancelar());
        Assert.Equal(EstadoPedido.Cancelado, cancelado.Estado);
    }

    // ------------------------------------------------------------------
    // Seguimiento de producción y entrega (BR-076)
    // ------------------------------------------------------------------

    [Fact]
    public void Transiciones_RegistranMarcasDeSeguimiento()
    {
        var pedido = NuevoPedido(EstadoPedido.Confirmado);

        var inicio = new DateTime(2026, 10, 4, 8, 0, 0, DateTimeKind.Utc);
        pedido.IniciarProduccion(inicio);
        Assert.Equal(inicio, pedido.FechaInicioProduccion);
        Assert.Null(pedido.FechaFinProduccion);
        Assert.Null(pedido.FechaEntrega);

        var fin = new DateTime(2026, 10, 6, 17, 0, 0, DateTimeKind.Utc);
        pedido.MarcarListo(fin);
        Assert.Equal(fin, pedido.FechaFinProduccion);
        Assert.Equal(inicio, pedido.FechaInicioProduccion);

        var entrega = new DateTime(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);
        pedido.Entregar(entrega);
        Assert.Equal(entrega, pedido.FechaEntrega);
        Assert.Equal(EstadoPedido.Entregado, pedido.Estado);
    }

    [Fact]
    public void Transiciones_WithoutMoment_UsesUtcNow()
    {
        var pedido = NuevoPedido(EstadoPedido.Confirmado);
        var antes = DateTime.UtcNow;

        pedido.IniciarProduccion();

        Assert.NotNull(pedido.FechaInicioProduccion);
        Assert.InRange(
            pedido.FechaInicioProduccion!.Value,
            antes.AddSeconds(-5),
            DateTime.UtcNow.AddSeconds(5));
    }

    [Fact]
    public void IniciarProduccion_NonUtcMoment_Throws_AndKeepsState()
    {
        var pedido = NuevoPedido(EstadoPedido.Confirmado);

        Assert.Throws<ArgumentException>(() => pedido.IniciarProduccion(
            new DateTime(2026, 10, 4, 8, 0, 0, DateTimeKind.Local)));

        Assert.Equal(EstadoPedido.Confirmado, pedido.Estado);
        Assert.Null(pedido.FechaInicioProduccion);
    }

    [Fact]
    public void TransicionarA_PassesTrackingMoment()
    {
        var pedido = NuevoPedido(EstadoPedido.Confirmado);
        var momento = new DateTime(2026, 10, 4, 8, 0, 0, DateTimeKind.Utc);

        pedido.TransicionarA(EstadoPedido.EnProduccion, momento);

        Assert.Equal(EstadoPedido.EnProduccion, pedido.Estado);
        Assert.Equal(momento, pedido.FechaInicioProduccion);
    }

    [Fact]
    public void ActualizarInformacion_WithValidData_StoresTrimmed()
    {
        var pedido = NuevoPedido(EstadoPedido.Confirmado);
        var estimada = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);

        pedido.ActualizarInformacion("  Colocar en showroom  ", estimada);

        Assert.Equal("Colocar en showroom", pedido.Notas);
        Assert.Equal(estimada, pedido.FechaEntregaEstimada);
    }

    [Fact]
    public void ActualizarInformacion_NullValues_ClearFields()
    {
        var pedido = NuevoPedido(EstadoPedido.Confirmado);
        pedido.ActualizarInformacion(
            "nota previa", new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc));

        pedido.ActualizarInformacion(null, null);

        Assert.Null(pedido.Notas);
        Assert.Null(pedido.FechaEntregaEstimada);
    }

    [Fact]
    public void ActualizarInformacion_OnTerminal_Throws_Br074()
    {
        foreach (var estado in new[] { EstadoPedido.Completado, EstadoPedido.Cancelado })
        {
            var pedido = NuevoPedido(estado);

            var ex = Assert.Throws<InvalidOperationException>(
                () => pedido.ActualizarInformacion("x", null));

            Assert.Contains("BR-074", ex.Message);
            Assert.Equal(estado, pedido.Estado);
        }
    }

    [Fact]
    public void ActualizarInformacion_TooLongNotes_Throws()
    {
        var pedido = NuevoPedido(EstadoPedido.Pendiente);

        Assert.Throws<ArgumentException>(() => pedido.ActualizarInformacion(
            new string('x', Pedido.MaxNotasLength + 1), null));
    }

    [Fact]
    public void ActualizarInformacion_NotesAtMaxLength_IsAllowed()
    {
        var pedido = NuevoPedido(EstadoPedido.Pendiente);

        pedido.ActualizarInformacion(new string('x', Pedido.MaxNotasLength), null);

        Assert.Equal(Pedido.MaxNotasLength, pedido.Notas!.Length);
    }

    [Fact]
    public void ActualizarInformacion_BeforeCreationDate_Throws()
    {
        // FechaCreacion = 2026-10-03 (FechaUtc).
        var pedido = NuevoPedido(EstadoPedido.Pendiente);

        Assert.Throws<ArgumentException>(() => pedido.ActualizarInformacion(
            null, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void ActualizarInformacion_SameDayAsCreation_IsAllowed()
    {
        var pedido = NuevoPedido(EstadoPedido.Pendiente);

        pedido.ActualizarInformacion(
            null, new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc));

        Assert.NotNull(pedido.FechaEntregaEstimada);
    }

    [Fact]
    public void ActualizarInformacion_NonUtcEstimate_Throws()
    {
        var pedido = NuevoPedido(EstadoPedido.Pendiente);

        Assert.Throws<ArgumentException>(() => pedido.ActualizarInformacion(
            null, new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Local)));
    }
}
