using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarWash.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaAuditoriaUsuarioCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            // GAP-UNACCENT-ASSIM: instala a extensão unaccent (CREATE EXTENSION
            // IF NOT EXISTS é idempotente). Mapeada como DbFunction no
            // CarWashDbContext para uso em LINQ — fecha a assimetria de busca
            // por nome com/sem acento ("joão" passa a casar "Joao").
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS unaccent;");

            migrationBuilder.DropIndex(
                name: "idx_clientes_email",
                schema: "public",
                table: "clientes");

            migrationBuilder.AddColumn<Guid>(
                name: "atualizado_por_usuario_id",
                schema: "public",
                table: "clientes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "criado_por_usuario_id",
                schema: "public",
                table: "clientes",
                type: "uuid",
                nullable: true);

            // GAP-CW-CLI-EMAIL-1: índice único parcial em e-mail. Antes de criar,
            // limpamos duplicatas existentes (mantém o registro mais antigo e
            // NULL-a o e-mail dos demais para não quebrar a criação do índice).
            // Dados legados de QA podem ter colidido antes da regra entrar em vigor.
            migrationBuilder.Sql(@"
                UPDATE public.clientes c
                SET email = NULL
                WHERE c.email IS NOT NULL
                  AND c.id NOT IN (
                    SELECT DISTINCT ON (email) id
                    FROM public.clientes
                    WHERE email IS NOT NULL
                    ORDER BY email, criado_em ASC
                  );");

            migrationBuilder.CreateIndex(
                name: "ux_clientes_email",
                schema: "public",
                table: "clientes",
                column: "email",
                unique: true,
                filter: "email IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropIndex(
                name: "ux_clientes_email",
                schema: "public",
                table: "clientes");

            migrationBuilder.DropColumn(
                name: "atualizado_por_usuario_id",
                schema: "public",
                table: "clientes");

            migrationBuilder.DropColumn(
                name: "criado_por_usuario_id",
                schema: "public",
                table: "clientes");

            migrationBuilder.CreateIndex(
                name: "idx_clientes_email",
                schema: "public",
                table: "clientes",
                column: "email",
                filter: "email IS NOT NULL");

            // Não removemos a extensão unaccent no Down — outras features podem
            // depender dela e DROP EXTENSION é destrutivo em prod. Idempotência
            // do Up garante que reaplicar a migration funcione.
        }
    }
}
