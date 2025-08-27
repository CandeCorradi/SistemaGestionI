using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Medida",
                table: "Productos",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

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
                table: "Productos",
                columns: new[] { "Id", "Categoria", "Descripcion", "IsDeleted", "Medida", "Nombre", "Stock" },
                values: new object[,]
                {
                    { 1, "Melamina", "Melamina de alta calidad color blanco", false, 252, "Melamina Blanca", 50 },
                    { 2, "Melamina", "Melamina de alta calidad, textura granulada", false, 252, "Melamina Caoba", 15 },
                    { 3, "Melamina", "Melamina de alta calidad, textura mate", false, 252, "Melamina Negra", 43 }
                });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ClienteId", "Nombre" },
                values: new object[] { 0, "sergio cliente" });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "ClienteId", "IsDeleted", "Nombre", "Password", "TipoUsuario" },
                values: new object[] { 3, 0, false, "franco cliente", "cliente124", 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Mayoristas",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Mayoristas",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Mayoristas",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Productos",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Productos",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Productos",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.AlterColumn<string>(
                name: "Medida",
                table: "Productos",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ClienteId", "Nombre" },
                values: new object[] { 1, "cliente1" });
        }
    }
}
