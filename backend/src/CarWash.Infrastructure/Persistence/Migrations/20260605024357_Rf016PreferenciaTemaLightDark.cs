using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarWash.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Rf016PreferenciaTemaLightDark : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE usuario_preferencias
                DROP CONSTRAINT IF EXISTS ck_pref_tema;
            """);

            migrationBuilder.Sql("""
                UPDATE usuario_preferencias
                SET tema = CASE
                    WHEN tema = 'claro' THEN 'light'
                    WHEN tema = 'escuro' THEN 'dark'
                    ELSE tema
                END;
            """);

            migrationBuilder.Sql("""
                ALTER TABLE usuario_preferencias
                ALTER COLUMN tema SET DEFAULT 'light';
            """);

            migrationBuilder.Sql("""
                ALTER TABLE usuario_preferencias
                ADD CONSTRAINT ck_pref_tema
                CHECK (tema IN ('light','dark'));
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE usuario_preferencias
                DROP CONSTRAINT IF EXISTS ck_pref_tema;
            """);

            migrationBuilder.Sql("""
                UPDATE usuario_preferencias
                SET tema = CASE
                    WHEN tema = 'light' THEN 'claro'
                    WHEN tema = 'dark' THEN 'escuro'
                    ELSE tema
                END;
            """);

            migrationBuilder.Sql("""
                ALTER TABLE usuario_preferencias
                ALTER COLUMN tema SET DEFAULT 'claro';
            """);

            migrationBuilder.Sql("""
                ALTER TABLE usuario_preferencias
                ADD CONSTRAINT ck_pref_tema
                CHECK (tema IN ('claro','escuro'));
            """);
        }
    }
}
