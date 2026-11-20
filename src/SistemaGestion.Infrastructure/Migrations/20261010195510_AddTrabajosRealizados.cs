using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SistemaGestion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrabajosRealizados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrabajosRealizados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TipoMueble = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ClienteNombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PublicarNombreCliente = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Medidas = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Melamina = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Accesorios = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Tags = table.Column<string[]>(type: "text[]", nullable: false),
                    Destacado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Publicado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrabajosRealizados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrabajoRealizadoImagenes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TrabajoRealizadoId = table.Column<int>(type: "integer", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrabajoRealizadoImagenes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrabajoRealizadoImagenes_TrabajosRealizados_TrabajoRealizad~",
                        column: x => x.TrabajoRealizadoId,
                        principalTable: "TrabajosRealizados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "Id", "Codigo", "Nombre" },
                values: new object[,]
                {
                    { 38, "TRABAJOS_VIEW", "Ver trabajos realizados" },
                    { 39, "TRABAJOS_CREATE", "Crear trabajos realizados" },
                    { 40, "TRABAJOS_UPDATE", "Editar trabajos realizados" },
                    { 41, "TRABAJOS_DELETE", "Eliminar trabajos realizados" }
                });

            migrationBuilder.InsertData(
                table: "RolPermisos",
                columns: new[] { "PermisoId", "RolId" },
                values: new object[,]
                {
                    { 38, 1 },
                    { 39, 1 },
                    { 40, 1 },
                    { 41, 1 },
                    { 38, 2 },
                    { 39, 2 },
                    { 40, 2 },
                    { 41, 2 },
                    { 38, 3 },
                    { 39, 3 },
                    { 40, 3 },
                    { 41, 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrabajoRealizadoImagenes_TrabajoRealizadoId_Orden",
                table: "TrabajoRealizadoImagenes",
                columns: new[] { "TrabajoRealizadoId", "Orden" });

            migrationBuilder.CreateIndex(
                name: "IX_TrabajosRealizados_Categoria",
                table: "TrabajosRealizados",
                column: "Categoria");

            migrationBuilder.CreateIndex(
                name: "IX_TrabajosRealizados_Publicado",
                table: "TrabajosRealizados",
                column: "Publicado");

            migrationBuilder.CreateIndex(
                name: "IX_TrabajosRealizados_Publicado_Destacado",
                table: "TrabajosRealizados",
                columns: new[] { "Publicado", "Destacado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrabajoRealizadoImagenes");

            migrationBuilder.DropTable(
                name: "TrabajosRealizados");

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { 38, 1 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { 39, 1 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { 40, 1 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { 41, 1 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { 38, 2 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { 39, 2 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { 40, 2 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { 41, 2 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { 38, 3 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { 39, 3 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { 40, 3 });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { 41, 3 });

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 41);
        }
    }
}
