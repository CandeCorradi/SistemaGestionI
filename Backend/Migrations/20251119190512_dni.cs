using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class dni : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Apellido",
                table: "Usuarios",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Dni",
                table: "Usuarios",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Pagos",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaPago",
                value: new DateTime(2025, 11, 19, 16, 4, 48, 505, DateTimeKind.Local).AddTicks(7129));

            migrationBuilder.UpdateData(
                table: "Pagos",
                keyColumn: "Id",
                keyValue: 2,
                column: "FechaPago",
                value: new DateTime(2025, 11, 19, 16, 4, 48, 505, DateTimeKind.Local).AddTicks(7163));

            migrationBuilder.UpdateData(
                table: "Pagos",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaPago",
                value: new DateTime(2025, 11, 19, 16, 4, 48, 505, DateTimeKind.Local).AddTicks(7166));

            migrationBuilder.UpdateData(
                table: "Pedidos",
                keyColumn: "Id",
                keyValue: 10,
                column: "Fecha",
                value: new DateTime(2025, 11, 19, 16, 4, 48, 505, DateTimeKind.Local).AddTicks(7354));

            migrationBuilder.UpdateData(
                table: "Pedidos",
                keyColumn: "Id",
                keyValue: 11,
                column: "Fecha",
                value: new DateTime(2025, 11, 19, 16, 4, 48, 505, DateTimeKind.Local).AddTicks(7361));

            migrationBuilder.UpdateData(
                table: "Pedidos",
                keyColumn: "Id",
                keyValue: 12,
                column: "Fecha",
                value: new DateTime(2025, 11, 19, 16, 4, 48, 505, DateTimeKind.Local).AddTicks(7365));

            migrationBuilder.UpdateData(
                table: "Presupuestos",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FechaEmision", "FechaVencimiento" },
                values: new object[] { new DateTime(2025, 11, 19, 16, 4, 48, 505, DateTimeKind.Local).AddTicks(7524), new DateTime(2025, 12, 19, 16, 4, 48, 505, DateTimeKind.Local).AddTicks(7526) });

            migrationBuilder.UpdateData(
                table: "Presupuestos",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "FechaEmision", "FechaVencimiento" },
                values: new object[] { new DateTime(2025, 11, 19, 16, 4, 48, 505, DateTimeKind.Local).AddTicks(7552), new DateTime(2025, 12, 19, 16, 4, 48, 505, DateTimeKind.Local).AddTicks(7554) });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Apellido", "Dni" },
                values: new object[] { "istrador", "" });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Apellido", "Dni" },
                values: new object[] { "massa", "" });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Apellido", "Dni" },
                values: new object[] { "gomez", "" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Apellido",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Dni",
                table: "Usuarios");

            migrationBuilder.UpdateData(
                table: "Pagos",
                keyColumn: "Id",
                keyValue: 1,
                column: "FechaPago",
                value: new DateTime(2025, 9, 9, 16, 37, 1, 745, DateTimeKind.Local).AddTicks(4999));

            migrationBuilder.UpdateData(
                table: "Pagos",
                keyColumn: "Id",
                keyValue: 2,
                column: "FechaPago",
                value: new DateTime(2025, 9, 9, 16, 37, 1, 745, DateTimeKind.Local).AddTicks(5030));

            migrationBuilder.UpdateData(
                table: "Pagos",
                keyColumn: "Id",
                keyValue: 3,
                column: "FechaPago",
                value: new DateTime(2025, 9, 9, 16, 37, 1, 745, DateTimeKind.Local).AddTicks(5034));

            migrationBuilder.UpdateData(
                table: "Pedidos",
                keyColumn: "Id",
                keyValue: 10,
                column: "Fecha",
                value: new DateTime(2025, 9, 9, 16, 37, 1, 745, DateTimeKind.Local).AddTicks(5124));

            migrationBuilder.UpdateData(
                table: "Pedidos",
                keyColumn: "Id",
                keyValue: 11,
                column: "Fecha",
                value: new DateTime(2025, 9, 9, 16, 37, 1, 745, DateTimeKind.Local).AddTicks(5132));

            migrationBuilder.UpdateData(
                table: "Pedidos",
                keyColumn: "Id",
                keyValue: 12,
                column: "Fecha",
                value: new DateTime(2025, 9, 9, 16, 37, 1, 745, DateTimeKind.Local).AddTicks(5136));

            migrationBuilder.UpdateData(
                table: "Presupuestos",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FechaEmision", "FechaVencimiento" },
                values: new object[] { new DateTime(2025, 9, 9, 16, 37, 1, 745, DateTimeKind.Local).AddTicks(5231), new DateTime(2025, 10, 9, 16, 37, 1, 745, DateTimeKind.Local).AddTicks(5233) });

            migrationBuilder.UpdateData(
                table: "Presupuestos",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "FechaEmision", "FechaVencimiento" },
                values: new object[] { new DateTime(2025, 9, 9, 16, 37, 1, 745, DateTimeKind.Local).AddTicks(5262), new DateTime(2025, 10, 9, 16, 37, 1, 745, DateTimeKind.Local).AddTicks(5264) });
        }
    }
}
