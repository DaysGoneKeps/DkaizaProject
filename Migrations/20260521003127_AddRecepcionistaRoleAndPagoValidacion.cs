using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DkaizaProject.Migrations
{
    /// <inheritdoc />
    public partial class AddRecepcionistaRoleAndPagoValidacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaValidacion",
                table: "Pagos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroOperacion",
                table: "Pagos",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Validado",
                table: "Pagos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ValidadoPorClienteId",
                table: "Pagos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsRecepcionista",
                table: "Clientes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Clientes",
                keyColumn: "Id",
                keyValue: 1,
                column: "EsRecepcionista",
                value: false);

            migrationBuilder.InsertData(
                table: "Clientes",
                columns: new[] { "Id", "Apellido", "Email", "EsAdmin", "EsEstilista", "EsRecepcionista", "EstilistaId", "FechaRegistro", "Nombre", "PasswordHash", "Telefono" },
                values: new object[] { 1000, "Dkaiza", "recepcion@dkaiza.com", false, false, true, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Recepcion", "$2a$11$rImcKUDCn6N6xavnKyabG.NFBP/BWszxCHLiu3IPkVii7A6JUWEC6", "" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Clientes",
                keyColumn: "Id",
                keyValue: 1000);

            migrationBuilder.DropColumn(
                name: "FechaValidacion",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "NumeroOperacion",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "Validado",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "ValidadoPorClienteId",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "EsRecepcionista",
                table: "Clientes");
        }
    }
}
