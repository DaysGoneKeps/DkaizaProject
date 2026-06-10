using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DkaizaProject.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CuponCodigo",
                table: "Pagos",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoDescuento",
                table: "Pagos",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "EsVip",
                table: "Clientes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "HoraFinAtencion",
                table: "Citas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HoraInicioAtencion",
                table: "Citas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Calificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CitaId = table.Column<int>(type: "INTEGER", nullable: false),
                    EstilistaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Estrellas = table.Column<int>(type: "INTEGER", nullable: false),
                    Comentario = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calificaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Calificaciones_Citas_CitaId",
                        column: x => x.CitaId,
                        principalTable: "Citas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Calificaciones_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Calificaciones_Estilistas_EstilistaId",
                        column: x => x.EstilistaId,
                        principalTable: "Estilistas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Cupones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PorcentajeDescuento = table.Column<decimal>(type: "TEXT", nullable: false),
                    MontoDescuento = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    EsPorcentaje = table.Column<bool>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsoMaximo = table.Column<int>(type: "INTEGER", nullable: false),
                    UsosActuales = table.Column<int>(type: "INTEGER", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cupones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotasCliente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    EstilistaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Contenido = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotasCliente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotasCliente_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotasCliente_Estilistas_EstilistaId",
                        column: x => x.EstilistaId,
                        principalTable: "Estilistas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    CitaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", nullable: false),
                    Mensaje = table.Column<string>(type: "TEXT", nullable: false),
                    Leida = table.Column<bool>(type: "INTEGER", nullable: false),
                    Procesada = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaAccion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AccionRealizada = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificaciones_Citas_CitaId",
                        column: x => x.CitaId,
                        principalTable: "Citas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notificaciones_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Clientes",
                keyColumn: "Id",
                keyValue: 1,
                column: "EsVip",
                value: false);

            migrationBuilder.UpdateData(
                table: "Clientes",
                keyColumn: "Id",
                keyValue: 1000,
                column: "EsVip",
                value: false);

            migrationBuilder.InsertData(
                table: "Cupones",
                columns: new[] { "Id", "Activo", "Codigo", "Descripcion", "EsPorcentaje", "FechaExpiracion", "MontoDescuento", "PorcentajeDescuento", "UsoMaximo", "UsosActuales" },
                values: new object[,]
                {
                    { 1, true, "DKAIZA10", "10% de descuento en cualquier servicio", true, new DateTime(2026, 12, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, 10m, 50, 0 },
                    { 2, true, "PROMO20", "S/20 de descuento fijo", false, new DateTime(2026, 12, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), 20m, 0m, 30, 0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Calificaciones_CitaId",
                table: "Calificaciones",
                column: "CitaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Calificaciones_ClienteId",
                table: "Calificaciones",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Calificaciones_EstilistaId",
                table: "Calificaciones",
                column: "EstilistaId");

            migrationBuilder.CreateIndex(
                name: "IX_NotasCliente_ClienteId",
                table: "NotasCliente",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_NotasCliente_EstilistaId",
                table: "NotasCliente",
                column: "EstilistaId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_CitaId",
                table: "Notificaciones",
                column: "CitaId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_ClienteId",
                table: "Notificaciones",
                column: "ClienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Calificaciones");

            migrationBuilder.DropTable(
                name: "Cupones");

            migrationBuilder.DropTable(
                name: "NotasCliente");

            migrationBuilder.DropTable(
                name: "Notificaciones");

            migrationBuilder.DropColumn(
                name: "CuponCodigo",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "MontoDescuento",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "EsVip",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "HoraFinAtencion",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "HoraInicioAtencion",
                table: "Citas");
        }
    }
}
