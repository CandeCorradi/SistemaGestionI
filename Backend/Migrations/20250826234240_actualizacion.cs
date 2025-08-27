using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class actualizacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DetallesPedidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PedidoId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesPedidos", x => x.Id);
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
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                    ClienteId = table.Column<int>(type: "int", nullable: false)
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
                    MontoEstimado = table.Column<decimal>(type: "decimal(65,30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Presupuestos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Turnos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FechaHora = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Estado = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClienteId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Turnos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "DetallesPedidos",
                columns: new[] { "Id", "Cantidad", "PedidoId", "ProductoId" },
                values: new object[,]
                {
                    { 7, 4532, 12, 123 },
                    { 8, 2548, 11, 365 },
                    { 9, 2500, 10, 2500 }
                });

            migrationBuilder.InsertData(
                table: "Pagos",
                columns: new[] { "Id", "FechaPago", "Monto", "PedidoId", "TipoPago" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 8, 26, 20, 42, 31, 641, DateTimeKind.Local).AddTicks(8322), 15000m, 10, "Tarjeta de Credito" },
                    { 2, new DateTime(2025, 8, 26, 20, 42, 31, 641, DateTimeKind.Local).AddTicks(8346), 250000m, 11, "Efectivo" },
                    { 3, new DateTime(2025, 8, 26, 20, 42, 31, 641, DateTimeKind.Local).AddTicks(8349), 305000m, 12, "Tarjeta de Débito" }
                });

            migrationBuilder.InsertData(
                table: "Pedidos",
                columns: new[] { "Id", "ClienteId", "Estado", "Fecha" },
                values: new object[,]
                {
                    { 10, 1, "En Proceso", new DateTime(2025, 8, 26, 20, 42, 31, 641, DateTimeKind.Local).AddTicks(8431) },
                    { 11, 2, "Completado", new DateTime(2025, 8, 26, 20, 42, 31, 641, DateTimeKind.Local).AddTicks(8437) },
                    { 12, 3, "Pendiente", new DateTime(2025, 8, 26, 20, 42, 31, 641, DateTimeKind.Local).AddTicks(8440) }
                });

            migrationBuilder.InsertData(
                table: "Presupuestos",
                columns: new[] { "Id", "ClientId", "FechaEmision", "FechaVencimiento", "MontoEstimado" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2025, 8, 26, 20, 42, 31, 641, DateTimeKind.Local).AddTicks(8609), new DateTime(2025, 9, 25, 20, 42, 31, 641, DateTimeKind.Local).AddTicks(8610), 50000m },
                    { 2, 2, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 30500m },
                    { 3, 3, new DateTime(2025, 8, 26, 20, 42, 31, 641, DateTimeKind.Local).AddTicks(8630), new DateTime(2025, 9, 25, 20, 42, 31, 641, DateTimeKind.Local).AddTicks(8632), 20000m }
                });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetallesPedidos");

            migrationBuilder.DropTable(
                name: "Pagos");

            migrationBuilder.DropTable(
                name: "Pedidos");

            migrationBuilder.DropTable(
                name: "Presupuestos");

            migrationBuilder.DropTable(
                name: "Turnos");
        }
    }
}
