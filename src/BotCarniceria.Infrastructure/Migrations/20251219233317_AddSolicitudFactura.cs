using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotCarniceria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSolicitudFactura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolicitudesFactura",
                columns: table => new
                {
                    SolicitudFacturaID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteID = table.Column<int>(type: "int", nullable: false),
                    Folio = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UsoCFDI = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DatosFacturacion_RazonSocial = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DatosFacturacion_Calle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DatosFacturacion_Numero = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DatosFacturacion_Colonia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DatosFacturacion_CodigoPostal = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DatosFacturacion_Correo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DatosFacturacion_RegimenFiscal = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    FechaProcesada = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesFactura", x => x.SolicitudFacturaID);
                    table.ForeignKey(
                        name: "FK_SolicitudesFactura_Clientes_ClienteID",
                        column: x => x.ClienteID,
                        principalTable: "Clientes",
                        principalColumn: "ClienteID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesFactura_ClienteID",
                table: "SolicitudesFactura",
                column: "ClienteID");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesFactura_Folio",
                table: "SolicitudesFactura",
                column: "Folio");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitudesFactura");
        }
    }
}
