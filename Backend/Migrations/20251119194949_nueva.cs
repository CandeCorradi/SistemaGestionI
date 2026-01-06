using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class nueva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Apellido = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telefono = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Direccion = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DetallesPedidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PedidoId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesPedidos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DetallesPresupuestos",
                columns: table => new
                {
                    IdDetalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PresupuestoId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesPresupuestos", x => x.IdDetalle);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Mayoristas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telefono = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Direccion = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TipoProducto = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mayoristas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Pagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PedidoId = table.Column<int>(type: "int", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TipoPago = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Pedidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Fecha = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Estado = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedidos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Presupuestos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    MontoEstimado = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Presupuestos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProductoPresupuestos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Medida = table.Column<int>(type: "int", nullable: false),
                    Stock = table.Column<int>(type: "int", nullable: false),
                    Categoria = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Precio = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductoPresupuestos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Medida = table.Column<int>(type: "int", nullable: false),
                    Stock = table.Column<int>(type: "int", nullable: false),
                    Categoria = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Precio = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Apellido = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Dni = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Password = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TipoUsuario = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Clientes",
                columns: new[] { "Id", "Apellido", "Direccion", "IsDeleted", "Nombre", "Telefono" },
                values: new object[,]
                {
                    { 1, "Perez", "Praderas 4590", false, "Juan", "3498657842" },
                    { 2, "Borges", "Rivadavia 3486", false, "Mario", "3498412987" },
                    { 3, "Morales", "Santa Fe 3961", false, "Analia", "3498475986" }
                });

            migrationBuilder.InsertData(
                table: "DetallesPedidos",
                columns: new[] { "Id", "Cantidad", "IsDeleted", "PedidoId", "ProductoId" },
                values: new object[,]
                {
                    { 7, 4532, false, 12, 123 },
                    { 8, 2548, false, 11, 365 },
                    { 9, 2500, false, 10, 2500 }
                });

            migrationBuilder.InsertData(
                table: "DetallesPresupuestos",
                columns: new[] { "IdDetalle", "Cantidad", "IsDeleted", "PresupuestoId", "ProductoId" },
                values: new object[,]
                {
                    { 1, 10, false, 1, 1 },
                    { 2, 5, false, 2, 2 },
                    { 3, 8, false, 3, 3 }
                });

            migrationBuilder.InsertData(
                table: "Mayoristas",
                columns: new[] { "Id", "Direccion", "Email", "IsDeleted", "Nombre", "Telefono", "TipoProducto" },
                values: new object[,]
                {
                    { 1, "Av. San Martin 1234", "maderaelroble@gmail.com", false, "Fabian Gonzalez", "3498657842", "" },
                    { 2, "Av. Siempreviva 2458", "melaminas@gmail.com", false, "Sergio Massa", "3498412987", "" },
                    { 3, "San roque 1257", "antonyeche@gmail.com", false, "Antonio Echeverria", "3498425698", "" }
                });

            migrationBuilder.InsertData(
                table: "Pagos",
                columns: new[] { "Id", "FechaPago", "IsDeleted", "Monto", "PedidoId", "TipoPago" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 11, 19, 16, 49, 44, 686, DateTimeKind.Local).AddTicks(5065), false, 15000m, 10, "Tarjeta de Credito" },
                    { 2, new DateTime(2025, 11, 19, 16, 49, 44, 686, DateTimeKind.Local).AddTicks(5092), false, 250000m, 11, "Efectivo" },
                    { 3, new DateTime(2025, 11, 19, 16, 49, 44, 686, DateTimeKind.Local).AddTicks(5095), false, 305000m, 12, "Tarjeta de Débito" }
                });

            migrationBuilder.InsertData(
                table: "Pedidos",
                columns: new[] { "Id", "ClienteId", "Estado", "Fecha", "IsDeleted" },
                values: new object[,]
                {
                    { 10, 1, "En Proceso", new DateTime(2025, 11, 19, 16, 49, 44, 686, DateTimeKind.Local).AddTicks(5164), false },
                    { 11, 2, "Completado", new DateTime(2025, 11, 19, 16, 49, 44, 686, DateTimeKind.Local).AddTicks(5169), false },
                    { 12, 3, "Pendiente", new DateTime(2025, 11, 19, 16, 49, 44, 686, DateTimeKind.Local).AddTicks(5173), false }
                });

            migrationBuilder.InsertData(
                table: "Presupuestos",
                columns: new[] { "Id", "ClientId", "FechaEmision", "FechaVencimiento", "IsDeleted", "MontoEstimado" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2025, 11, 19, 16, 49, 44, 686, DateTimeKind.Local).AddTicks(5242), new DateTime(2025, 12, 19, 16, 49, 44, 686, DateTimeKind.Local).AddTicks(5244), false, 50000m },
                    { 2, 2, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), false, 30500m },
                    { 3, 3, new DateTime(2025, 11, 19, 16, 49, 44, 686, DateTimeKind.Local).AddTicks(5263), new DateTime(2025, 12, 19, 16, 49, 44, 686, DateTimeKind.Local).AddTicks(5265), false, 20000m }
                });

            migrationBuilder.InsertData(
                table: "ProductoPresupuestos",
                columns: new[] { "Id", "Categoria", "Descripcion", "IsDeleted", "Medida", "Nombre", "Precio", "Stock" },
                values: new object[,]
                {
                    { 1, "Melamina", "Melamina de alta calidad color blanco", false, 252, "Melamina Blanca", 2500m, 50 },
                    { 2, "Melamina", "Melamina de alta calidad, textura granulada", false, 252, "Melamina Caoba", 3650m, 15 },
                    { 3, "Melamina", "Melamina de alta calidad, textura mate", false, 252, "Melamina Negra", 4230m, 43 }
                });

            migrationBuilder.InsertData(
                table: "Productos",
                columns: new[] { "Id", "Categoria", "Descripcion", "IsDeleted", "Medida", "Nombre", "Precio", "Stock" },
                values: new object[,]
                {
                    { 1, "Melamina", "Melamina de alta calidad color blanco", false, 252, "Melamina Blanca", 0m, 50 },
                    { 2, "Melamina", "Melamina de alta calidad, textura granulada", false, 252, "Melamina Caoba", 0m, 15 },
                    { 3, "Melamina", "Melamina de alta calidad, textura mate", false, 252, "Melamina Negra", 0m, 43 }
                });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "Apellido", "ClienteId", "Dni", "Email", "IsDeleted", "Nombre", "Password", "TipoUsuario" },
                values: new object[,]
                {
                    { 1, "istrador", 0, "", "administrador123@gmail.com", false, "admin", "admin123", 0 },
                    { 2, "massa", 0, "", "sergio2024@gmail.com", false, "sergio cliente", "cliente123", 1 },
                    { 3, "gomez", 0, "", "franquito59@gmail.com", false, "franco cliente", "cliente124", 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropTable(
                name: "DetallesPedidos");

            migrationBuilder.DropTable(
                name: "DetallesPresupuestos");

            migrationBuilder.DropTable(
                name: "Mayoristas");

            migrationBuilder.DropTable(
                name: "Pagos");

            migrationBuilder.DropTable(
                name: "Pedidos");

            migrationBuilder.DropTable(
                name: "Presupuestos");

            migrationBuilder.DropTable(
                name: "ProductoPresupuestos");

            migrationBuilder.DropTable(
                name: "Productos");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
