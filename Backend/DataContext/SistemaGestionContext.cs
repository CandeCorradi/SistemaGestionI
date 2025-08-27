using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Service.Models;

namespace Backend.DataContext
{
    public class SistemaGestionContext: DbContext
    {
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Mayorista> Mayoristas { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<DetallePedido> DetallesPedidos { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<Turno> Turnos { get; set; }
        public DbSet<Presupuesto> Presupuestos { get; set; }
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
                    Email = "juanpe@gmail.com",
                    Direccion = "Praderas 4590",
                    IsDeleted = false
                },
                new Cliente
                {
                    Id = 2,
                    Nombre = "Mario",
                    Apellido = "Borges",
                    Telefono = "3498412987",
                    Email = "marito@gmail.com",
                    Direccion = "Rivadavia 3486",
                    IsDeleted = false
                },
                new Cliente
                {
                    Id = 3,
                    Nombre = "Analia",
                    Apellido = "Morales",
                    Telefono = "3498475986",
                    Email = "animo@gmail.com",
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
                    Password = "admin123",
                    TipoUsuario = Service.Enums.TipoUsuarioEnum.Administrador,
                    IsDeleted = false
                },
                new Usuario
                {
                    Id = 2,
                    Nombre = "sergio cliente",
                    Password = "cliente123",
                    TipoUsuario = Service.Enums.TipoUsuarioEnum.Cliente,
                    IsDeleted = false
                },
                new Usuario
                {
                    Id = 3,
                    Nombre = "franco cliente",
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
            //Cargamos los datos iniciales de los turnos
            modelBuilder.Entity<Turno>().HasData(
                new Turno
                {
                    Id = 1,
                    FechaHora = new DateTime(2025, 8, 27, 10, 0, 0),
                    Estado = "Reunión para discutir el nuevo proyecto",
                    ClienteId = 1,
                    IsDeleted = false
                },
                new Turno
                {
                    Id = 2,
                    FechaHora = new DateTime(2025, 8, 28, 14, 0, 0),
                    Estado = "Consulta sobre productos y servicios",
                    ClienteId = 2,
                    IsDeleted = false
                },
                new Turno
                {
                    Id = 3,
                    FechaHora = new DateTime(2025, 8, 29, 16, 0, 0),
                    Estado = "Seguimiento de pedido y entrega",
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


            //Configuramos las querys para que no devuelvan entidades con estado "Eliminado"
            modelBuilder.Entity<Cliente>().HasQueryFilter(p => !p.IsDeleted);
            modelBuilder.Entity<Mayorista>().HasQueryFilter(u => !u.IsDeleted);
            modelBuilder.Entity<Producto>().HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<Usuario>().HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<DetallePedido>().HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<Pago>().HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<Pedido>().HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<Turno>().HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<Presupuesto>().HasQueryFilter(c => !c.IsDeleted);

        }
    }
}
