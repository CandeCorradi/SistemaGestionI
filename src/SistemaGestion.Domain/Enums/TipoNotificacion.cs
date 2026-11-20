namespace SistemaGestion.Domain.Enums;

/// <summary>
/// Tipos de notificación de negocio (BR-092, functional-spec §18).
/// Serializados como número en la API (convención del proyecto).
/// </summary>
public enum TipoNotificacion
{
    /// <summary>Presupuesto enviado al cliente (BR-092).</summary>
    PresupuestoEnviado = 0,

    /// <summary>Presupuesto aceptado y pedido creado (BR-092).</summary>
    PresupuestoAceptado = 1,

    /// <summary>Cambio de estado de un pedido (BR-092).</summary>
    PedidoEstadoCambiado = 2,

    /// <summary>Pago registrado contra un pedido (BR-092).</summary>
    PagoRegistrado = 3,

    /// <summary>Pedido listo para entrega (BR-092).</summary>
    PedidoListo = 4,

    /// <summary>
    /// Solicitud de presupuesto recibida desde el Portal Cliente
    /// (BR-092, functional-spec §10/§23); avisa al equipo para cotizar.
    /// </summary>
    PresupuestoSolicitado = 5,
}
