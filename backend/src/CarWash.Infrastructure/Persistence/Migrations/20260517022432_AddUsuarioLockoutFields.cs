using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarWash.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuarioLockoutFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "bloqueado_ate",
                schema: "public",
                table: "usuarios",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tentativas_invalidas",
                schema: "public",
                table: "usuarios",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "ck_usuarios_tentativas_invalidas",
                schema: "public",
                table: "usuarios",
                sql: "tentativas_invalidas >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_usuarios_tentativas_invalidas",
                schema: "public",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "bloqueado_ate",
                schema: "public",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "tentativas_invalidas",
                schema: "public",
                table: "usuarios");
        }
    }
}
