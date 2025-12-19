using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotCarniceria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SessionTimeoutNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotificacionTimeoutEnviada",
                table: "Conversaciones",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificacionTimeoutEnviada",
                table: "Conversaciones");
        }
    }
}
