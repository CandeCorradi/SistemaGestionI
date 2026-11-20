using Microsoft.EntityFrameworkCore;
using SistemaGestion.Domain.Entities;

namespace SistemaGestion.Infrastructure.Persistence;

public class SistemaGestionDbContext : DbContext
{
    public SistemaGestionDbContext(DbContextOptions<SistemaGestionDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cliente> Clientes { get; set; } = null!;
    public DbSet<Producto> Productos { get; set; } = null!;
    public DbSet<Presupuesto> Presupuestos { get; set; } = null!;
    public DbSet<PresupuestoDetalle> PresupuestoDetalles { get; set; } = null!;
    public DbSet<Pedido> Pedidos { get; set; } = null!;
    public DbSet<PedidoHistorial> PedidoHistoriales { get; set; } = null!;
    public DbSet<Usuario> Usuarios { get; set; } = null!;
    public DbSet<Rol> Roles { get; set; } = null!;
    public DbSet<Proveedor> Proveedores { get; set; } = null!;
    public DbSet<Permiso> Permisos { get; set; } = null!;
    public DbSet<RolPermiso> RolPermisos { get; set; } = null!;
    public DbSet<UsuarioPermiso> UsuarioPermisos { get; set; } = null!;
    public DbSet<MovimientoStock> MovimientosStock { get; set; } = null!;
    public DbSet<Pago> Pagos { get; set; } = null!;
    public DbSet<Factura> Facturas { get; set; } = null!;
    public DbSet<Notificacion> Notificaciones { get; set; } = null!;

    public DbSet<ProductoImagen> ProductoImagenes { get; set; } = null!;

    public DbSet<TrabajoRealizado> TrabajosRealizados { get; set; } = null!;

    public DbSet<TrabajoRealizadoImagen> TrabajoRealizadoImagenes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(SistemaGestionDbContext).Assembly);
    }
}
