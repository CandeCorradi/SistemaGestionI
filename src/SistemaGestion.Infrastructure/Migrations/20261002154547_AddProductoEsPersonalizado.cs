using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGestion.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductoEsPersonalizado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsPersonalizado",
                table: "Productos",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsPersonalizado",
                table: "Productos");
        }
    }
}
