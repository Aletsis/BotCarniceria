using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotCarniceria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFacturacionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FacturaTemp_Folio",
                table: "Conversaciones",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacturaTemp_Total",
                table: "Conversaciones",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacturaTemp_UsoCFDI",
                table: "Conversaciones",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Facturacion_Calle",
                table: "Clientes",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Facturacion_CodigoPostal",
                table: "Clientes",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Facturacion_Colonia",
                table: "Clientes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Facturacion_Correo",
                table: "Clientes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Facturacion_Numero",
                table: "Clientes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Facturacion_RazonSocial",
                table: "Clientes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Facturacion_RegimenFiscal",
                table: "Clientes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FacturaTemp_Folio",
                table: "Conversaciones");

            migrationBuilder.DropColumn(
                name: "FacturaTemp_Total",
                table: "Conversaciones");

            migrationBuilder.DropColumn(
                name: "FacturaTemp_UsoCFDI",
                table: "Conversaciones");

            migrationBuilder.DropColumn(
                name: "Facturacion_Calle",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Facturacion_CodigoPostal",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Facturacion_Colonia",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Facturacion_Correo",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Facturacion_Numero",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Facturacion_RazonSocial",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Facturacion_RegimenFiscal",
                table: "Clientes");
        }
    }
}
