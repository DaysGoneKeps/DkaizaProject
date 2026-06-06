using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DkaizaProject.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordTemporalCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordTemporal",
                table: "Clientes",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Clientes",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordTemporal",
                value: null);

            migrationBuilder.UpdateData(
                table: "Clientes",
                keyColumn: "Id",
                keyValue: 1000,
                column: "PasswordTemporal",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordTemporal",
                table: "Clientes");
        }
    }
}
