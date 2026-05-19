using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DkaizaProject.Migrations
{
    /// <inheritdoc />
    public partial class AddEstilistaRoleAndEnProceso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsEstilista",
                table: "Clientes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EstilistaId",
                table: "Clientes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Clientes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EsEstilista", "EstilistaId" },
                values: new object[] { false, null });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_EstilistaId",
                table: "Clientes",
                column: "EstilistaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clientes_Estilistas_EstilistaId",
                table: "Clientes",
                column: "EstilistaId",
                principalTable: "Estilistas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clientes_Estilistas_EstilistaId",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_EstilistaId",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "EsEstilista",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "EstilistaId",
                table: "Clientes");
        }
    }
}
