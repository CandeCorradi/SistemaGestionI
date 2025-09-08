using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Service.Models;

namespace Backend.DataContext
{
    public class SistemaGestionContext : DbContext
    {
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Mayorista> Mayoristas { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<DetallePedido> DetallesPedidos { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<Presupuesto> Presupuestos { get; set; }
        public DbSet<ProductoPresupuesto> ProductoPresupuestos { get; set; }
        public DbSet<DetallePresupuesto> DetallesPresupuestos { get; set; }
        public SistemaGestionContext(){ }

        public SistemaGestionContext(DbContextOptions<SistemaGestionContext> options) : base(options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Cargamos los datos iniciales del cliente
            modelBuilder.Entity<Cliente>().HasData(
                new Cliente
                {
                    Id = 1,
                    Nombre = "Juan",
                    Apellido = "Perez",
                    Telefono = "3498657842",
                    Direccion = "Praderas 4590",
                    IsDeleted = false
                },
                new Cliente
                {
                    Id = 2,
                    Nombre = "Mario",
                    Apellido = "Borges",
                    Telefono = "3498412987",
                    Direccion = "Rivadavia 3486",
                    IsDeleted = false
                },
                new Cliente
                {
                    Id = 3,
                    Nombre = "Analia",
                    Apellido = "Morales",
                    Telefono = "3498475986",
                    Direccion = "Santa Fe 3961",
                    IsDeleted = false
                }
            );
            //Cargamos los datos iniciales de los mayoristas
            modelBuilder.Entity<Mayorista>().HasData(
                new Mayorista
                {
                    Id = 1,
                    Nombre = "Fabian Gonzalez",
                    Telefono = "3498657842",
                    Email = "maderaelroble@gmail.com",
                    Direccion = "Av. San Martin 1234",
                    IsDeleted = false
                },
                new Mayorista
                {
                    Id = 2,
                    Nombre = "Sergio Massa",
                    Telefono = "3498412987",
                    Email = "melaminas@gmail.com",
                    Direccion = "Av. Siempreviva 2458",
                    IsDeleted = false
                },
                new Mayorista
                {
                    Id = 3,
                    Nombre = "Antonio Echeverria",
                    Telefono = "3498425698",
                    Email = "antonyeche@gmail.com",
                    Direccion = "San roque 1257",
                    IsDeleted = false
                }
            );
            //Cargamos los datos iniciales de los productos
            modelBuilder.Entity<Producto>().HasData(
                new Producto
                {
                    Id = 1,
                    Nombre = "Melamina Blanca",
                    Descripcion = "Melamina de alta calidad color blanco",
                    Medida = 21 * 12,
                    Stock = 50,
                    Categoria = "Melamina",
                    IsDeleted = false
                },
                new Producto
                {
                    Id = 2,
                    Nombre = "Melamina Caoba",
                    Descripcion = "Melamina de alta calidad, textura granulada",
                    Medida = 21 * 12,
                    Stock = 15,
                    Categoria = "Melamina",
                    IsDeleted = false
                },
                new Producto
                {
                    Id = 3,
                    Nombre = "Melamina Negra",
                    Descripcion = "Melamina de alta calidad, textura mate",
                    Medida = 21 * 12,
                    Stock = 43,
                    Categoria = "Melamina",
                    IsDeleted = false
                }
            );
            //Cargamos los datos iniciales de los usuarios: administrador y cliente
            modelBuilder.Entity<Usuario>().HasData(
                new Usuario
                {
                    Id = 1,
                    Nombre = "admin",
                    Email = "administrador123@gmail.com",
                    Password = "admin123",
                    TipoUsuario = Service.Enums.TipoUsuarioEnum.Administrador,
                    IsDeleted = false
                },
                new Usuario
                {
                    Id = 2,
                    Nombre = "sergio cliente",
                    Email = "sergio2024@gmail.com",
                    Password = "cliente123",
                    TipoUsuario = Service.Enums.TipoUsuarioEnum.Cliente,
                    IsDeleted = false
                },
                new Usuario
                {
                    Id = 3,
                    Nombre = "franco cliente",
                    Email = "franquito59@gmail.com",
                    Password = "cliente124",
                    TipoUsuario = Service.Enums.TipoUsuarioEnum.Cliente,
                    IsDeleted = false
                }
            );
            //Cargamos los datos iniciales de los detalles de pedidos
            modelBuilder.Entity<DetallePedido>().HasData(
                new DetallePedido
                {
                    Id = 9,
                    PedidoId = 10,
                    ProductoId = 2500,
                    Cantidad = 2500,
                    IsDeleted = false
                },
                new DetallePedido
                {
                    Id = 8,
                    PedidoId = 11,
                    ProductoId = 365,
                    Cantidad = 2548,
                    IsDeleted = false
                },
                new DetallePedido
                {
                    Id = 7,
                    PedidoId = 12,
                    ProductoId = 123,
                    Cantidad = 4532,
                    IsDeleted = false
                }
            );
            //Cargamos los datos iniciales de los pagos
            modelBuilder.Entity<Pago>().HasData(
                new Pago
                {
                    Id = 1,
                    PedidoId = 10,
                    FechaPago = DateTime.Now,
                    Monto = 15000,
                    TipoPago = "Tarjeta de Credito",
                    IsDeleted = false

                },
                new Pago
                {
                    Id = 2,
                    PedidoId = 11,
                    FechaPago = DateTime.Now,
                    Monto = 250000,
                    TipoPago = "Efectivo",
                    IsDeleted = false
                },
                new Pago
                {
                    Id = 3,
                    PedidoId = 12,
                    FechaPago = DateTime.Now,
                    Monto = 305000,
                    TipoPago = "Tarjeta de Débito",
                    IsDeleted = false
                }
            );
            //Cargamos los datos iniciales de los pedidos
            modelBuilder.Entity<Pedido>().HasData(
                new Pedido
                {
                    Id = 10,
                    Fecha = DateTime.Now,
                    Estado = "En Proceso",
                    ClienteId = 1,
                    IsDeleted = false
                },
                new Pedido
                {
                    Id = 11,
                    Fecha = DateTime.Now,
                    Estado = "Completado",
                    ClienteId = 2,
                    IsDeleted = false
                },
                new Pedido
                {
                    Id = 12,
                    Fecha = DateTime.Now,
                    Estado = "Pendiente",
                    ClienteId = 3,
                    IsDeleted = false
                }
            );
            
            //Cargamos los datos iniciales de los presupuestos
            modelBuilder.Entity<Presupuesto>().HasData(
                new Presupuesto
                {
                    Id = 1,
                    ClientId = 1,
                    FechaEmision = DateTime.Now,
                    FechaVencimiento = DateTime.Now.AddDays(30),
                    MontoEstimado = 50000,
                    IsDeleted = false
                },
                new Presupuesto
                {
                    Id = 2,
                    ClientId = 2,
                    FechaEmision = new DateTime(2025, 8, 26),
                    FechaVencimiento = new DateTime(2025, 9, 25),
                    MontoEstimado = 30500,
                    IsDeleted = false
                },
                new Presupuesto
                {
                    Id = 3,
                    ClientId = 3,
                    FechaEmision = DateTime.Now,
                    FechaVencimiento = DateTime.Now.AddDays(30),
                    MontoEstimado = 20000,
                    IsDeleted = false
                }
            );
            //Cargamos los datos iniciales de detalles de presupuestos
            modelBuilder.Entity<DetallePresupuesto>().HasData(
                new DetallePresupuesto
                {
                    IdDetalle = 1,
                    PresupuestoId = 1,
                    ProductoId = 1,
                    Cantidad = 10,
                    IsDeleted = false
                },
                new DetallePresupuesto
                {
                    IdDetalle = 2,
                    PresupuestoId = 2,
                    ProductoId = 2,
                    Cantidad = 5,
                    IsDeleted = false
                },
                new DetallePresupuesto
                {
                    IdDetalle = 3,
                    PresupuestoId = 3,
                    ProductoId = 3,
                    Cantidad = 8,
                    IsDeleted = false
                }
            );
            //Cargamos los datos iniciales de productos de presupuestos
            modelBuilder.Entity<ProductoPresupuesto>().HasData(
                new ProductoPresupuesto
                {
                    Id = 1,
                    Nombre = "Melamina Blanca",
                    Descripcion = "Melamina de alta calidad color blanco",
                    Medida = 21 * 12,
                    Stock = 50,
                    Categoria = "Melamina",
                    Precio = 2500,
                    IsDeleted = false
                },
                new ProductoPresupuesto
                {
                    Id = 2,
                    Nombre = "Melamina Caoba",
                    Descripcion = "Melamina de alta calidad, textura granulada",
                    Medida = 21 * 12,
                    Stock = 15,
                    Categoria = "Melamina",
                    Precio = 3650,
                    IsDeleted = false
                },
                new ProductoPresupuesto
                {
                    Id = 3,
                    Nombre = "Melamina Negra",
                    Descripcion = "Melamina de alta calidad, textura mate",
                    Medida = 21 * 12,
                    Stock = 43,
                    Categoria = "Melamina",
                    Precio = 4230,
                    IsDeleted = false
                }
            );


            //Configuramos las querys para que no devuelvan entidades con estado "Eliminado"
            modelBuilder.Entity<Cliente>().HasQueryFilter(p => !p.IsDeleted);
            modelBuilder.Entity<Mayorista>().HasQueryFilter(u => !u.IsDeleted);
            modelBuilder.Entity<Producto>().HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<Usuario>().HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<DetallePedido>().HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<Pago>().HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<Pedido>().HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<Presupuesto>().HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<DetallePresupuesto>().HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<ProductoPresupuesto>().HasQueryFilter(c => !c.IsDeleted);

        }
    }
}
