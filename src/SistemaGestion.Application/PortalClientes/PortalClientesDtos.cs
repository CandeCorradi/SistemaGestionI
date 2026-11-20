namespace SistemaGestion.Application.PortalClientes;

/// <summary>
/// Perfil del cliente del portal (functional-spec §23): datos del
/// registro de cliente vinculado a la cuenta (BR-010).
/// </summary>
public sealed record ClienteProfileDto(
    int Id,
    string Email,
    string Nombre,
    string Apellido,
    string? Telefono,
    string? Direccion,
    string? Documento,
    DateTime FechaCreacion);

/// <summary>
/// Actualización del perfil desde el portal. El email no se modifica
/// desde aquí: es la identidad de la cuenta de autenticación.
/// </summary>
public sealed record UpdateClienteProfileDto(
    string? Nombre = null,
    string? Apellido = null,
    string? Telefono = null,
    string? Direccion = null,
    string? Documento = null);

/// <summary>
/// Solicitud de presupuesto desde el portal (functional-spec §10/§12):
/// el cliente describe lo que necesita; la carpintería cotiza. El
/// presupuesto se crea en Borrador con un ítem descriptivo sin precio
/// (0) que el equipo completa antes de enviarlo.
/// </summary>
public sealed record RequestQuoteDto(
    string Description,
    int Cantidad = 1);

/// <summary>
/// Resumen del portal: totales de pedidos (excluyendo cancelados) y
/// pagos del cliente, saldo (= TotalPedidos - TotalPagado), cantidad de
/// pedidos y presupuestos aún sin responder (Enviado).
/// </summary>
public sealed record ClienteSummaryDto(
    decimal TotalPedidos,
    decimal TotalPagado,
    decimal Saldo,
    int PedidosCount,
    int PresupuestosPendientes);
