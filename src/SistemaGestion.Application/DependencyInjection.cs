using Microsoft.Extensions.DependencyInjection;
using SistemaGestion.Application.Abstractions;
using SistemaGestion.Application.Clientes;
using SistemaGestion.Application.Facturacion;
using SistemaGestion.Application.Facturacion.Abstractions;
using SistemaGestion.Application.Inventario;
using SistemaGestion.Application.Notificaciones;
using SistemaGestion.Application.Pedidos;
using SistemaGestion.Application.Permisos;
using SistemaGestion.Application.Presupuestos;
using SistemaGestion.Application.Proveedores;
using SistemaGestion.Application.Productos;
using SistemaGestion.Application.Roles;
using SistemaGestion.Application.Usuarios;

namespace SistemaGestion.Application;

/// <summary>
/// Punto de entrada de los servicios de la capa Application.
/// Los casos de uso, validadores y mappings se registrarán en fases siguientes.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<UsuariosService>();
        services.AddScoped<RolesService>();
        services.AddScoped<PermisosService>();
        services.AddScoped<ClientesService>();
        services.AddScoped<ProveedoresService>();
        services.AddScoped<ProductosService>();
        services.AddScoped<InventarioService>();
        services.AddScoped<PresupuestosService>();
        services.AddScoped<PedidosService>();
        services.AddScoped<Pagos.PagosService>();
        services.AddScoped<FacturasService>();
        services.AddScoped<IBillingService, SimulatedBillingService>();
        services.AddScoped<NotificacionesService>();
        services.AddScoped<INotificacionPublisher>(sp => sp.GetRequiredService<NotificacionesService>());
        services.AddScoped<Reports.ReportsService>();
        services.AddScoped<Dashboard.DashboardService>();
        services.AddScoped<PortalClientes.PortalClientesService>();
        services.AddScoped<TrabajosRealizados.TrabajosRealizadosService>();

        return services;
    }
}