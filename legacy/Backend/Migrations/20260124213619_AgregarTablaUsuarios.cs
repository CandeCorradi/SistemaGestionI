using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTablaUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
        name: "Usuarios",
        columns: table => new
        {
            Id = table.Column<int>(type: "int", nullable: false)
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
            Nombre = table.Column<string>(type: "longtext", nullable: false),
            Apellido = table.Column<string>(type: "longtext", nullable: false),
            NombreUsuario = table.Column<string>(type: "longtext", nullable: false),
            Contraseña = table.Column<string>(type: "longtext", nullable: false),
            Mail = table.Column<string>(type: "longtext", nullable: false)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_Usuarios", x => x.Id);
        })
        .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
            name: "Usuarios");
        }
    }
}
