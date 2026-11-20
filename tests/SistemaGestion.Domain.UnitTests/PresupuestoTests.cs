using SistemaGestion.Domain.Entities;
using SistemaGestion.Domain.Enums;
using Xunit;

namespace SistemaGestion.Domain.UnitTests;

public sealed class PresupuestoTests
{
    private static readonly DateTime HoyUtc = new(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc);

    private static Presupuesto CrearPresupuesto(DateTime? fechaValidez = null)
        => Presupuesto.Crear(
            clienteId: 7,
            fechaValidezUtc: fechaValidez,
            notas: "Notas del presupuesto",
            condiciones: "50% de seña",
            fechaCreacionUtc: HoyUtc);

    private static PresupuestoDetalle Detalle(int cantidad = 2, decimal precio = 100m, decimal descuento = 0)
        => PresupuestoDetalle.Crear("Mueble a medida", cantidad, precio, descuento);

    [Fact]
    public void Crear_WithValidData_StoresFields_InBorrador()
    {
        var presupuesto = CrearPresupuesto(new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(7, presupuesto.ClienteId);
        Assert.Equal(HoyUtc, presupuesto.FechaCreacion);
        Assert.Equal(new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc), presupuesto.FechaValidez);
        Assert.Equal(EstadoPresupuesto.Borrador, presupuesto.Estado);
        Assert.Equal("Notas del presupuesto", presupuesto.Notas);
        Assert.Equal("50% de seña", presupuesto.Condiciones);
        Assert.Null(presupuesto.FechaEnvio);
        Assert.Empty(presupuesto.Detalles);
        Assert.Equal(0m, presupuesto.Total);
    }

    [Fact]
    public void Crear_WithoutCliente_Throws()
        => Assert.Throws<ArgumentException>(() => Presupuesto.Crear(0, null, null, null, HoyUtc));

    [Fact]
    public void Crear_WithNonUtcCreationDate_Throws()
        => Assert.Throws<ArgumentException>(() => Presupuesto.Crear(
            7, null, null, null, new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Local)));

    [Fact]
    public void AgregarDetalle_RecalculaTotales_SubtotalDescuentoTotal()
    {
        var presupuesto = CrearPresupuesto();

        presupuesto.AgregarDetalle(Detalle(cantidad: 3, precio: 100m, descuento: 20m));
        presupuesto.AgregarDetalle(Detalle(cantidad: 1, precio: 50m, descuento: 0m));

        Assert.Equal(350m, presupuesto.Subtotal);
        Assert.Equal(20m, presupuesto.Descuento);
        Assert.Equal(330m, presupuesto.Total);
        Assert.Equal(2, presupuesto.Detalles.Count);
    }

    [Fact]
    public void RecalcularTotales_OnlyBackendAuthority_ValuesMatchLines()
    {
        var presupuesto = CrearPresupuesto();
        presupuesto.AgregarDetalle(Detalle(cantidad: 4, precio: 25.75m, descuento: 10m));

        presupuesto.RecalcularTotales();

        Assert.Equal(103m, presupuesto.Subtotal);
        Assert.Equal(10m, presupuesto.Descuento);
        Assert.Equal(93m, presupuesto.Total);
        Assert.Equal(presupuesto.Detalles.Single().Subtotal, presupuesto.Total);
    }

    [Fact]
    public void Detalle_CantidadInvalida_Throws()
        => Assert.Throws<ArgumentException>(() => PresupuestoDetalle.Crear("Mesa", 0, 100m, 0));

    [Fact]
    public void Detalle_PrecioNegativo_Throws()
        => Assert.Throws<ArgumentException>(() => PresupuestoDetalle.Crear("Mesa", 1, -1m, 0));

    [Fact]
    public void Detalle_DescuentoNegativo_Throws()
        => Assert.Throws<ArgumentException>(() => PresupuestoDetalle.Crear("Mesa", 1, 100m, -5m));

    [Fact]
    public void Detalle_DescuentoMayorQueBruto_Throws()
        => Assert.Throws<ArgumentException>(() => PresupuestoDetalle.Crear("Mesa", 1, 100m, 101m));

    [Fact]
    public void Detalle_DescuentoIgualAlBruto_Ok()
    {
        var detalle = PresupuestoDetalle.Crear("Mesa", 2, 100m, 200m);

        Assert.Equal(0m, detalle.Subtotal);
    }

    [Fact]
    public void Detalle_DescuentoValido_Ok()
    {
        var detalle = PresupuestoDetalle.Crear("Mesa", 2, 100m, 50m);

        Assert.Equal(150m, detalle.Subtotal);
        Assert.Equal("Mesa", detalle.Descripcion);
    }

    [Fact]
    public void Detalle_SinDescripcion_Throws()
        => Assert.Throws<ArgumentException>(() => PresupuestoDetalle.Crear("  ", 1, 100m, 0));

    [Fact]
    public void ValidarAlMenosUnDetalle_SinDetalles_Throws()
    {
        var presupuesto = CrearPresupuesto();

        Assert.Throws<ArgumentException>(() => presupuesto.ValidarAlMenosUnDetalle());
    }

    [Fact]
    public void Enviar_FromBorrador_Ok_SetsEnviadoAndFechaEnvio()
    {
        var presupuesto = CrearPresupuesto();
        presupuesto.AgregarDetalle(Detalle());

        presupuesto.Enviar(HoyUtc);

        Assert.Equal(EstadoPresupuesto.Enviado, presupuesto.Estado);
        Assert.Equal(HoyUtc, presupuesto.FechaEnvio);
    }

    [Fact]
    public void Enviar_WithoutItems_Throws()
    {
        var presupuesto = CrearPresupuesto();

        Assert.Throws<ArgumentException>(() => presupuesto.Enviar(HoyUtc));
    }

    [Fact]
    public void Enviar_FromEnviado_Throws()
    {
        var presupuesto = CrearPresupuesto();
        presupuesto.AgregarDetalle(Detalle());
        presupuesto.Enviar(HoyUtc);

        Assert.Throws<InvalidOperationException>(() => presupuesto.Enviar(HoyUtc));
    }

    [Fact]
    public void Rechazar_FromEnviado_Ok_WithMotivo()
    {
        var presupuesto = CrearPresupuesto();
        presupuesto.AgregarDetalle(Detalle());
        presupuesto.Enviar(HoyUtc);

        presupuesto.Rechazar("Precio alto");

        Assert.Equal(EstadoPresupuesto.Rechazado, presupuesto.Estado);
        Assert.Equal("Precio alto", presupuesto.EstadoMotivo);
    }

    [Fact]
    public void Rechazar_FromBorrador_Throws()
    {
        var presupuesto = CrearPresupuesto();
        presupuesto.AgregarDetalle(Detalle());

        Assert.Throws<InvalidOperationException>(() => presupuesto.Rechazar(null));
    }

    [Fact]
    public void Anular_FromBorrador_Ok()
    {
        var presupuesto = CrearPresupuesto();
        presupuesto.AgregarDetalle(Detalle());

        presupuesto.Anular("Se dio de baja");

        Assert.Equal(EstadoPresupuesto.Anulado, presupuesto.Estado);
        Assert.Equal("Se dio de baja", presupuesto.EstadoMotivo);
    }

    [Fact]
    public void Anular_FromEnviado_Ok()
    {
        var presupuesto = CrearPresupuesto();
        presupuesto.AgregarDetalle(Detalle());
        presupuesto.Enviar(HoyUtc);

        presupuesto.Anular(null);

        Assert.Equal(EstadoPresupuesto.Anulado, presupuesto.Estado);
        Assert.Null(presupuesto.EstadoMotivo);
    }

    [Fact]
    public void Anular_FromAceptado_Throws()
    {
        var presupuesto = CrearPresupuesto();
        presupuesto.AgregarDetalle(Detalle());
        presupuesto.Enviar(HoyUtc);
        presupuesto.Aceptar(HoyUtc);

        Assert.Throws<InvalidOperationException>(() => presupuesto.Anular(null));
    }

    [Fact]
    public void Aceptar_FromEnviadoYVigente_Ok()
    {
        var presupuesto = CrearPresupuesto();
        presupuesto.AgregarDetalle(Detalle());
        presupuesto.Enviar(HoyUtc);

        presupuesto.Aceptar(HoyUtc);

        Assert.Equal(EstadoPresupuesto.Aceptado, presupuesto.Estado);
    }

    [Fact]
    public void Aceptar_WhenExpired_Throws_BR064()
    {
        var presupuesto = CrearPresupuesto(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
        presupuesto.AgregarDetalle(Detalle());
        presupuesto.Enviar(HoyUtc);

        Assert.Throws<InvalidOperationException>(() => presupuesto.Aceptar(HoyUtc));
        Assert.Equal(EstadoPresupuesto.Enviado, presupuesto.Estado);
    }

    [Fact]
    public void Aceptar_FromBorrador_Throws()
    {
        var presupuesto = CrearPresupuesto();
        presupuesto.AgregarDetalle(Detalle());

        Assert.Throws<InvalidOperationException>(() => presupuesto.Aceptar(HoyUtc));
    }

    [Fact]
    public void Aceptar_Twice_Throws_BR064()
    {
        var presupuesto = CrearPresupuesto();
        presupuesto.AgregarDetalle(Detalle());
        presupuesto.Enviar(HoyUtc);
        presupuesto.Aceptar(HoyUtc);

        Assert.Throws<InvalidOperationException>(() => presupuesto.Aceptar(HoyUtc));
    }

    [Fact]
    public void Aceptar_FromRechazado_Throws_BR064()
    {
        var presupuesto = CrearPresupuesto();
        presupuesto.AgregarDetalle(Detalle());
        presupuesto.Enviar(HoyUtc);
        presupuesto.Rechazar(null);

        Assert.Throws<InvalidOperationException>(() => presupuesto.Aceptar(HoyUtc));
    }

    [Fact]
    public void EstaExpirado_WithFutureValidez_False()
    {
        var presupuesto = CrearPresupuesto(new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        presupuesto.AgregarDetalle(Detalle());

        Assert.False(presupuesto.EstaExpirado(HoyUtc));
    }

    [Fact]
    public void EstaExpirado_PastValidez_Ok()
    {
        var presupuesto = CrearPresupuesto(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
        presupuesto.AgregarDetalle(Detalle());

        Assert.True(presupuesto.EstaExpirado(HoyUtc));
    }

    [Fact]
    public void EstaExpirado_WithoutValidez_False()
    {
        var presupuesto = CrearPresupuesto();
        presupuesto.AgregarDetalle(Detalle());

        Assert.False(presupuesto.EstaExpirado(HoyUtc));
    }

    [Fact]
    public void EstaExpirado_Aceptado_False_NotConsideredExpired()
    {
        // Aceptado a tiempo (mismo día de la validez); pasado el vencimiento
        // no se reporta expirado porque el estado ya es definitivo.
        var presupuesto = CrearPresupuesto(new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc));
        presupuesto.AgregarDetalle(Detalle());
        presupuesto.Enviar(HoyUtc);
        presupuesto.Aceptar(HoyUtc);

        Assert.False(presupuesto.EstaExpirado(HoyUtc.AddDays(5)));
    }

    [Fact]
    public void Actualizar_InBorrador_UpdatesHeaderFields()
    {
        var presupuesto = CrearPresupuesto();

        presupuesto.Actualizar(
            clienteId: 9,
            fechaValidezUtc: new DateTime(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            notas: "Nueva nota",
            condiciones: "Nueva condición",
            fechaModificacionUtc: HoyUtc);

        Assert.Equal(9, presupuesto.ClienteId);
        Assert.Equal(new DateTime(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc), presupuesto.FechaValidez);
        Assert.Equal("Nueva nota", presupuesto.Notas);
        Assert.Equal("Nueva condición", presupuesto.Condiciones);
        Assert.Equal(HoyUtc, presupuesto.FechaModificacion);
    }

    [Fact]
    public void Actualizar_WithInvalidCliente_Throws()
    {
        var presupuesto = CrearPresupuesto();

        Assert.Throws<ArgumentException>(() => presupuesto.Actualizar(0, null, null, null, HoyUtc));
    }

    [Fact]
    public void LimpiarDetalles_RemovesAllLines_AndTotalsGoToZero()
    {
        var presupuesto = CrearPresupuesto();
        presupuesto.AgregarDetalle(Detalle());

        presupuesto.LimpiarDetalles();
        presupuesto.RecalcularTotales();

        Assert.Empty(presupuesto.Detalles);
        Assert.Equal(0m, presupuesto.Subtotal);
        Assert.Equal(0m, presupuesto.Total);
    }
}
