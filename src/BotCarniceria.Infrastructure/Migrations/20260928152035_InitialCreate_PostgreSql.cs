using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BotCarniceria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_PostgreSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    ClienteID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NumeroTelefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Direccion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Facturacion_RazonSocial = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Facturacion_RFC = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    Facturacion_Calle = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Facturacion_Numero = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Facturacion_Colonia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Facturacion_CodigoPostal = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Facturacion_Correo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Facturacion_RegimenFiscal = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FechaAlta = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.ClienteID);
                });

            migrationBuilder.CreateTable(
                name: "Configuraciones",
                columns: table => new
                {
                    ConfigID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Clave = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Valor = table.Column<string>(type: "text", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: true),
                    Editable = table.Column<bool>(type: "boolean", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configuraciones", x => x.ConfigID);
                });

            migrationBuilder.CreateTable(
                name: "Conversaciones",
                columns: table => new
                {
                    NumeroTelefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Estado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Buffer = table.Column<string>(type: "text", nullable: true),
                    NombreTemporal = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FacturaTemp_Folio = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FacturaTemp_Total = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    FacturaTemp_UsoCFDI = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UltimaActividad = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TimeoutEnMinutos = table.Column<int>(type: "integer", nullable: false),
                    NotificacionTimeoutEnviada = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Notificacion24hEnviada = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversaciones", x => x.NumeroTelefono);
                });

            migrationBuilder.CreateTable(
                name: "Mensajes",
                columns: table => new
                {
                    MensajeID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NumeroTelefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Origen = table.Column<int>(type: "integer", nullable: false),
                    Contenido = table.Column<string>(type: "text", nullable: false),
                    TipoContenido = table.Column<int>(type: "integer", nullable: false),
                    MetadataWhatsApp = table.Column<string>(type: "text", nullable: true),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    FueLeido = table.Column<bool>(type: "boolean", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    WhatsAppMessageId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mensajes", x => x.MensajeID);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    UsuarioID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Rol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UltimoAcceso = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Telefono = table.Column<string>(type: "text", nullable: true),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false),
                    LockoutEnd = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.UsuarioID);
                });

            migrationBuilder.CreateTable(
                name: "Pedidos",
                columns: table => new
                {
                    PedidoID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClienteID = table.Column<int>(type: "integer", nullable: false),
                    Folio = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Contenido = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Notas = table.Column<string>(type: "text", nullable: true),
                    FormaPago = table.Column<string>(type: "text", nullable: true),
                    EstadoImpresion = table.Column<bool>(type: "boolean", nullable: false),
                    FechaImpresion = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedidos", x => x.PedidoID);
                    table.ForeignKey(
                        name: "FK_Pedidos_Clientes_ClienteID",
                        column: x => x.ClienteID,
                        principalTable: "Clientes",
                        principalColumn: "ClienteID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesFactura",
                columns: table => new
                {
                    SolicitudFacturaID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClienteID = table.Column<int>(type: "integer", nullable: false),
                    Folio = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UsoCFDI = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DatosFacturacion_RazonSocial = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DatosFacturacion_RFC = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    DatosFacturacion_Calle = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DatosFacturacion_Numero = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DatosFacturacion_Colonia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DatosFacturacion_CodigoPostal = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DatosFacturacion_Correo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DatosFacturacion_RegimenFiscal = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    FechaProcesada = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Notas = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
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
                name: "IX_Clientes_NumeroTelefono",
                table: "Clientes",
                column: "NumeroTelefono",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Configuraciones_Clave",
                table: "Configuraciones",
                column: "Clave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mensajes_Fecha",
                table: "Mensajes",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Mensajes_NumeroTelefono",
                table: "Mensajes",
                column: "NumeroTelefono");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_ClienteID",
                table: "Pedidos",
                column: "ClienteID");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_Folio",
                table: "Pedidos",
                column: "Folio",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesFactura_ClienteID",
                table: "SolicitudesFactura",
                column: "ClienteID");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesFactura_Folio",
                table: "SolicitudesFactura",
                column: "Folio");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Username",
                table: "Usuarios",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configuraciones");

            migrationBuilder.DropTable(
                name: "Conversaciones");

            migrationBuilder.DropTable(
                name: "Mensajes");

            migrationBuilder.DropTable(
                name: "Pedidos");

            migrationBuilder.DropTable(
                name: "SolicitudesFactura");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Clientes");
        }
    }
}
