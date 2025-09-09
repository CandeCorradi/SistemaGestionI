using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class inicio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Turnos");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Clientes");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Usuarios",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "Precio",
                table: "Productos",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Presupuestos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Pedidos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Pagos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "DetallesPedidos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.UpdateData(
                table: "DetallesPedidos",
                keyColumn: "Id",
                keyValue: 7,
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "DetallesPedidos",
                keyColumn: "Id",
                keyValue: 8,
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "DetallesPedidos",
                keyColumn: "Id",
                keyValue: 9,
                column: "IsDeleted",
                value: false);

            migrationBuilder.InsertData(
                table: "DetallesPresupuestos",
                columns: new[] { "IdDetalle", "Cantidad", "IsDeleted", "PresupuestoId", "ProductoId" },
                values: new object[,]
                {
                    { 1, 10, false, 1, 1 },
                    { 2, 5, false, 2, 2 },
                    { 3, 8, false, 3, 3 }
                });

            migrationBuilder.UpdateData(
                table: "Pagos",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FechaPago", "IsDeleted" },
                values: new object[] { new DateTime(2025, 9, 9, 16, 37, 1, 745, DateTimeKind.Local).AddTicks(4999), false });

            migrationBuilder.UpdateData(
                table: "Pagos",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "FechaPago", "IsDeleted" },
                values: new object[] { new DateTime(2025, 9, 9, 16, 37, 1, 745, DateTimeKind.Local).AddTicks(5030), false });

            migrationBuilder.UpdateData(
                table: "Pagos",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "FechaPago", "IsDeleted" },
                values: new object[] { new DateTime(2025, 9, 9, 16, 37, 1, 745, DateTimeKind.Local).AddTicks(5034), false });

            migrationBuilder.UpdateData(
                table: "Pedidos",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Fecha", "IsDeleted" },
                values: new object[] { new DateTime(2025, 9, 9, 16, 37, 1, 745, DateTimeKind.Local).AddTicks(5124), false });

            migrationBuilder.UpdateData(
                table: "Pedidos",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Fecha", "IsDeleted" },
                values: new object[] { new DateTime(2025, 9, 9, 16, 37, 1, 745, DateTimeKind.Local).AddTicks(5132), false });

            migrationBuilder.UpdateData(
                table: "Pedidos",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Fecha", "IsDeleted" },
                values: new object[] { new DateTime(2025, 9, 9, 16, 37, 1, 745, DateTimeKind.Local).AddTicks(5136), false });

            migrationBuilder.UpdateData(
                table: "Presupuestos",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FechaEmision", "FechaVencimiento", "IsDeleted" },
                values: new object[] { new DateTime(2025, 9, 9, 16, 37, 1, 745, DateTimeKind.Local).AddTicks(5231), new DateTime(2025, 10, 9, 16, 37, 1, 745, DateTimeKind.Local).AddTicks(5233), false });

            migrationBuilder.UpdateData(
                table: "Presupuestos",
                keyColumn: "Id",
                keyValue: 2,
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "Presupuestos",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "FechaEmision", "FechaVencimiento", "IsDeleted" },
                values: new object[] { new DateTime(2025, 9, 9, 16, 37, 1, 745, DateTimeKind.Local).AddTicks(5262), new DateTime(2025, 10, 9, 16, 37, 1, 745, DateTimeKind.Local).AddTicks(5264), false });

            migrationBuilder.InsertData(
                table: "ProductoPresupuestos",
                columns: new[] { "Id", "Categoria", "Descripcion", "IsDeleted", "Medida", "Nombre", "Precio", "Stock" },
                values: new object[,]
                {
                    { 1, "Melamina", "Melamina de alta calidad color blanco", false, 252, "Melamina Blanca", 2500m, 50 },
                    { 2, "Melamina", "Melamina de alta calidad, textura granulada", false, 252, "Melamina Caoba", 3650m, 15 },
                    { 3, "Melamina", "Melamina de alta calidad, textura mate", false, 252, "Melamina Negra", 4230m, 43 }
                });

            migrationBuilder.UpdateData(
                table: "Productos",
                keyColumn: "Id",
                keyValue: 1,
                column: "Precio",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "Productos",
                keyColumn: "Id",
                keyValue: 2,
                column: "Precio",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "Productos",
                keyColumn: "Id",
                keyValue: 3,
                column: "Precio",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "Email",
                value: "administrador123@gmail.com");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 2,
                column: "Email",
                value: "sergio2024@gmail.com");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 3,
                column: "Email",
                value: "franquito59@gmail.com");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetallesPresupuestos");

            migrationBuilder.DropTable(
                name: "ProductoPresupuestos");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Precio",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Presupuestos");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "DetallesPedidos");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Clientes",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Turnos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaHora = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Turnos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Clientes",
                keyColumn: "Id",
                keyValue: 1,
                column: "Email",
                value: "juanpe@gmail.com");

            migrationBuilder.UpdateData(
                table: "Clientes",
                keyColumn: "Id",
                keyValue: 2,
                column: "Email",
                value: "marito@gmail.com");

            migrationBuilder.UpdateData(
                table: "Clientes",
                keyColumn: "Id",
                keyValue: 3,
                column: "Email",
                value: "animo@gmail.com");

            migrationBuilder.UpdateData(
                table: "Pagos",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaPago",
                value: new DateTime(2025, 8, 26, 20, 42, 31, 641, DateTimeKind.Local).AddTicks(8322));

            migrationBuilder.UpdateData(
                table: "Pagos",
                keyColumn: "Id",
                keyValue: 2,
                column: "FechaPago",
                value: new DateTime(2025, 8, 26, 20, 42, 31, 641, DateTimeKind.Local).AddTicks(8346));

            migrationBuilder.UpdateData(
                table: "Pagos",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaPago",
                value: new DateTime(2025, 8, 26, 20, 42, 31, 641, DateTimeKind.Local).AddTicks(8349));

            migrationBuilder.UpdateData(
                table: "Pedidos",
                keyColumn: "Id",
                keyValue: 10,
                column: "Fecha",
                value: new DateTime(2025, 8, 26, 20, 42, 31, 641, DateTimeKind.Local).AddTicks(8431));

            migrationBuilder.UpdateData(
                table: "Pedidos",
                keyColumn: "Id",
                keyValue: 11,
                column: "Fecha",
                value: new DateTime(2025, 8, 26, 20, 42, 31, 641, DateTimeKind.Local).AddTicks(8437));

            migrationBuilder.UpdateData(
                table: "Pedidos",
                keyColumn: "Id",
                keyValue: 12,
                column: "Fecha",
                value: new DateTime(2025, 8, 26, 20, 42, 31, 641, DateTimeKind.Local).AddTicks(8440));

            migrationBuilder.UpdateData(
                table: "Presupuestos",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FechaEmision", "FechaVencimiento" },
                values: new object[] { new DateTime(2025, 8, 26, 20, 42, 31, 641, DateTimeKind.Local).AddTicks(8609), new DateTime(2025, 9, 25, 20, 42, 31, 641, DateTimeKind.Local).AddTicks(8610) });

            migrationBuilder.UpdateData(
                table: "Presupuestos",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "FechaEmision", "FechaVencimiento" },
                values: new object[] { new DateTime(2025, 8, 26, 20, 42, 31, 641, DateTimeKind.Local).AddTicks(8630), new DateTime(2025, 9, 25, 20, 42, 31, 641, DateTimeKind.Local).AddTicks(8632) });

            migrationBuilder.InsertData(
                table: "Turnos",
                columns: new[] { "Id", "ClienteId", "Estado", "FechaHora" },
                values: new object[,]
                {
                    { 1, 1, "Reunión para discutir el nuevo proyecto", new DateTime(2025, 8, 27, 10, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, 2, "Consulta sobre productos y servicios", new DateTime(2025, 8, 28, 14, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 3, 3, "Seguimiento de pedido y entrega", new DateTime(2025, 8, 29, 16, 0, 0, 0, DateTimeKind.Unspecified) }
                });
        }
    }
}
