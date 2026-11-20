namespace SistemaGestion.Domain.Authorization;

/// <summary>
/// Catálogo fijo de permisos granulares (AGENTS.md §7, roadmap §10.4).
/// Formato UPPER_SNAKE consistente con business-rules.md BR-002 y
/// api-specification.md §4 (p. ej. CLIENTES_VIEW, USUARIOS_CREATE).
/// El Id es explícito y estable porque se usa como seed en la base de datos.
/// </summary>
public static class Permisos
{
    public sealed record PermisoDefinicion(int Id, string Codigo, string Nombre);

    public static readonly IReadOnlyList<PermisoDefinicion> Catalogo = new List<PermisoDefinicion>
    {
        new(1, "CLIENTES_VIEW", "Ver clientes"),
        new(2, "CLIENTES_CREATE", "Crear clientes"),
        new(3, "CLIENTES_UPDATE", "Editar clientes"),
        new(4, "CLIENTES_DELETE", "Eliminar clientes"),
        new(5, "PROVEEDORES_VIEW", "Ver proveedores"),
        new(6, "PROVEEDORES_CREATE", "Crear proveedores"),
        new(7, "PROVEEDORES_UPDATE", "Editar proveedores"),
        new(8, "PROVEEDORES_DELETE", "Eliminar proveedores"),
        new(9, "PRODUCTOS_VIEW", "Ver productos"),
        new(10, "PRODUCTOS_CREATE", "Crear productos"),
        new(11, "PRODUCTOS_UPDATE", "Editar productos"),
        new(12, "PRODUCTOS_DELETE", "Eliminar productos"),
        new(13, "STOCK_VIEW", "Ver stock"),
        new(14, "STOCK_ENTRY", "Ingresar stock"),
        new(15, "STOCK_ADJUST", "Ajustar stock"),
        new(16, "PRESUPUESTOS_VIEW", "Ver presupuestos"),
        new(17, "PRESUPUESTOS_CREATE", "Crear presupuestos"),
        new(18, "PRESUPUESTOS_UPDATE", "Editar presupuestos"),
        new(19, "PRESUPUESTOS_ACCEPT", "Aceptar presupuestos"),
        new(20, "PRESUPUESTOS_ANULATE", "Anular presupuestos"),
        new(21, "PEDIDOS_VIEW", "Ver pedidos"),
        new(22, "PEDIDOS_CREATE", "Crear pedidos"),
        new(23, "PEDIDOS_UPDATE", "Editar pedidos"),
        new(24, "PEDIDOS_CANCEL", "Cancelar pedidos"),
        new(25, "PAGOS_VIEW", "Ver pagos"),
        new(26, "PAGOS_CREATE", "Registrar pagos"),
        new(27, "PAGOS_ANULATE", "Anular pagos"),
        new(28, "FACTURAS_VIEW", "Ver facturas"),
        new(29, "FACTURAS_CREATE", "Crear facturas"),
        new(30, "REPORTES_VIEW", "Ver reportes"),
        new(31, "REPORTES_EXPORT", "Exportar reportes"),
        new(32, "USUARIOS_VIEW", "Ver usuarios"),
        new(33, "USUARIOS_CREATE", "Crear usuarios"),
        new(34, "USUARIOS_UPDATE", "Editar usuarios"),
        new(35, "USUARIOS_DELETE", "Eliminar usuarios"),
        new(36, "ROLES_VIEW", "Ver roles"),
        new(37, "ROLES_UPDATE", "Editar roles"),
        new(38, "TRABAJOS_VIEW", "Ver trabajos realizados"),
        new(39, "TRABAJOS_CREATE", "Crear trabajos realizados"),
        new(40, "TRABAJOS_UPDATE", "Editar trabajos realizados"),
        new(41, "TRABAJOS_DELETE", "Eliminar trabajos realizados"),
    };

    public static IReadOnlyList<string> Todos { get; } =
        Catalogo.Select(p => p.Codigo).ToList();

    public const string ClientesView = "CLIENTES_VIEW";
    public const string ClientesCreate = "CLIENTES_CREATE";
    public const string ClientesUpdate = "CLIENTES_UPDATE";
    public const string ClientesDelete = "CLIENTES_DELETE";
    public const string ProveedoresView = "PROVEEDORES_VIEW";
    public const string ProveedoresCreate = "PROVEEDORES_CREATE";
    public const string ProveedoresUpdate = "PROVEEDORES_UPDATE";
    public const string ProveedoresDelete = "PROVEEDORES_DELETE";
    public const string ProductosView = "PRODUCTOS_VIEW";
    public const string ProductosCreate = "PRODUCTOS_CREATE";
    public const string ProductosUpdate = "PRODUCTOS_UPDATE";
    public const string ProductosDelete = "PRODUCTOS_DELETE";
    public const string StockView = "STOCK_VIEW";
    public const string StockEntry = "STOCK_ENTRY";
    public const string StockAdjust = "STOCK_ADJUST";
    public const string PresupuestosView = "PRESUPUESTOS_VIEW";
    public const string PresupuestosCreate = "PRESUPUESTOS_CREATE";
    public const string PresupuestosUpdate = "PRESUPUESTOS_UPDATE";
    public const string PresupuestosAccept = "PRESUPUESTOS_ACCEPT";
    public const string PresupuestosAnulate = "PRESUPUESTOS_ANULATE";
    public const string PedidosView = "PEDIDOS_VIEW";
    public const string PedidosCreate = "PEDIDOS_CREATE";
    public const string PedidosUpdate = "PEDIDOS_UPDATE";
    public const string PedidosCancel = "PEDIDOS_CANCEL";
    public const string PagosView = "PAGOS_VIEW";
    public const string PagosCreate = "PAGOS_CREATE";
    public const string PagosAnulate = "PAGOS_ANULATE";
    public const string FacturasView = "FACTURAS_VIEW";
    public const string FacturasCreate = "FACTURAS_CREATE";
    public const string ReportesView = "REPORTES_VIEW";
    public const string ReportesExport = "REPORTES_EXPORT";
    public const string UsuariosView = "USUARIOS_VIEW";
    public const string UsuariosCreate = "USUARIOS_CREATE";
    public const string UsuariosUpdate = "USUARIOS_UPDATE";
    public const string UsuariosDelete = "USUARIOS_DELETE";
    public const string RolesView = "ROLES_VIEW";
    public const string RolesUpdate = "ROLES_UPDATE";
    public const string TrabajosView = "TRABAJOS_VIEW";
    public const string TrabajosCreate = "TRABAJOS_CREATE";
    public const string TrabajosUpdate = "TRABAJOS_UPDATE";
    public const string TrabajosDelete = "TRABAJOS_DELETE";
}
