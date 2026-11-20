using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGestion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPresupuestosCompletos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Total",
                table: "Presupuestos",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "Condiciones",
                table: "Presupuestos",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Descuento",
                table: "Presupuestos",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Estado",
                table: "Presupuestos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EstadoMotivo",
                table: "Presupuestos",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEnvio",
                table: "Presupuestos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaModificacion",
                table: "Presupuestos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaValidez",
                table: "Presupuestos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notas",
                table: "Presupuestos",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "Presupuestos",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ProductoId",
                table: "PresupuestoDetalles",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Presupuestos_Estado_FechaCreacion",
                table: "Presupuestos",
                columns: new[] { "Estado", "FechaCreacion" });

            migrationBuilder.CreateIndex(
                name: "IX_PresupuestoDetalles_ProductoId",
                table: "PresupuestoDetalles",
                column: "ProductoId");

            migrationBuilder.AddForeignKey(
                name: "FK_PresupuestoDetalles_Productos_ProductoId",
                table: "PresupuestoDetalles",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PresupuestoDetalles_Productos_ProductoId",
                table: "PresupuestoDetalles");

            migrationBuilder.DropIndex(
                name: "IX_Presupuestos_Estado_FechaCreacion",
                table: "Presupuestos");

            migrationBuilder.DropIndex(
                name: "IX_PresupuestoDetalles_ProductoId",
                table: "PresupuestoDetalles");

            migrationBuilder.DropColumn(
                name: "Condiciones",
                table: "Presupuestos");

            migrationBuilder.DropColumn(
                name: "Descuento",
                table: "Presupuestos");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Presupuestos");

            migrationBuilder.DropColumn(
                name: "EstadoMotivo",
                table: "Presupuestos");

            migrationBuilder.DropColumn(
                name: "FechaEnvio",
                table: "Presupuestos");

            migrationBuilder.DropColumn(
                name: "FechaModificacion",
                table: "Presupuestos");

            migrationBuilder.DropColumn(
                name: "FechaValidez",
                table: "Presupuestos");

            migrationBuilder.DropColumn(
                name: "Notas",
                table: "Presupuestos");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "Presupuestos");

            migrationBuilder.DropColumn(
                name: "ProductoId",
                table: "PresupuestoDetalles");

            migrationBuilder.AlterColumn<decimal>(
                name: "Total",
                table: "Presupuestos",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0m);
        }
    }
}
