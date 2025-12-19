using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotCarniceria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuarioLockoutFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessFailedCount",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEnd",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessFailedCount",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                table: "Usuarios");
        }
    }
}
