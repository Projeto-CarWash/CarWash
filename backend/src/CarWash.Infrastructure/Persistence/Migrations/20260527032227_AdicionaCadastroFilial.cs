using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarWash.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// RF017 — Cadastro de filiais. Migration estritamente aditiva e nullable
    /// (ADR-0007 §3.4 / RAT01): adiciona colunas <c>codigo</c>, <c>cnpj</c>,
    /// <c>endereco_*</c> e <c>criado_por_usuario_id</c> em <c>filiais</c>, troca o
    /// UNIQUE cru <c>uk_filiais_nome</c> pelo índice funcional case-insensitive
    /// <c>uk_filiais_nome_lower</c>, e instala os UNIQUE parciais
    /// <c>uk_filiais_codigo</c> / <c>uk_filiais_cnpj</c> + CHECK
    /// <c>ck_filiais_codigo_formato</c>. O card derivado 207 promoverá
    /// <c>codigo</c> para NOT NULL após backfill manual.
    /// </remarks>
    public partial class AdicionaCadastroFilial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            // Drop do UNIQUE cru — será substituído pelo índice funcional
            // case-insensitive criado abaixo via raw SQL (L1 do ADR-0007).
            migrationBuilder.DropIndex(
                name: "uk_filiais_nome",
                schema: "public",
                table: "filiais");

            migrationBuilder.AddColumn<string>(
                name: "cnpj",
                schema: "public",
                table: "filiais",
                type: "character varying(14)",
                maxLength: 14,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "codigo",
                schema: "public",
                table: "filiais",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "criado_por_usuario_id",
                schema: "public",
                table: "filiais",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "endereco_bairro",
                schema: "public",
                table: "filiais",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "endereco_cep",
                schema: "public",
                table: "filiais",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "endereco_cidade",
                schema: "public",
                table: "filiais",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "endereco_complemento",
                schema: "public",
                table: "filiais",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "endereco_logradouro",
                schema: "public",
                table: "filiais",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "endereco_numero",
                schema: "public",
                table: "filiais",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "endereco_uf",
                schema: "public",
                table: "filiais",
                type: "character(2)",
                fixedLength: true,
                maxLength: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_filiais_cidade_uf",
                schema: "public",
                table: "filiais",
                columns: new[] { "endereco_cidade", "endereco_uf" },
                filter: "endereco_cidade IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uk_filiais_cnpj",
                schema: "public",
                table: "filiais",
                column: "cnpj",
                unique: true,
                filter: "cnpj IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uk_filiais_codigo",
                schema: "public",
                table: "filiais",
                column: "codigo",
                unique: true,
                filter: "codigo IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_filiais_codigo_formato",
                schema: "public",
                table: "filiais",
                sql: "codigo IS NULL OR codigo ~ '^[A-Z0-9]{2,20}$'");

            // Índice funcional case-insensitive em LOWER(nome) — substituto do
            // antigo uk_filiais_nome. Bloqueia "Filial Centro" vs "FILIAL CENTRO".
            // EF Core 8 não emite índice funcional pelo modelo; criamos via SQL e
            // o snapshot não declara HasIndex(x => x.Nome) para evitar
            // divergência entre o modelo do EF e o DDL real.
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX uk_filiais_nome_lower ON public.filiais (LOWER(nome));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.Sql("DROP INDEX IF EXISTS public.uk_filiais_nome_lower;");

            migrationBuilder.DropIndex(
                name: "idx_filiais_cidade_uf",
                schema: "public",
                table: "filiais");

            migrationBuilder.DropIndex(
                name: "uk_filiais_cnpj",
                schema: "public",
                table: "filiais");

            migrationBuilder.DropIndex(
                name: "uk_filiais_codigo",
                schema: "public",
                table: "filiais");

            migrationBuilder.DropCheckConstraint(
                name: "ck_filiais_codigo_formato",
                schema: "public",
                table: "filiais");

            migrationBuilder.DropColumn(
                name: "cnpj",
                schema: "public",
                table: "filiais");

            migrationBuilder.DropColumn(
                name: "codigo",
                schema: "public",
                table: "filiais");

            migrationBuilder.DropColumn(
                name: "criado_por_usuario_id",
                schema: "public",
                table: "filiais");

            migrationBuilder.DropColumn(
                name: "endereco_bairro",
                schema: "public",
                table: "filiais");

            migrationBuilder.DropColumn(
                name: "endereco_cep",
                schema: "public",
                table: "filiais");

            migrationBuilder.DropColumn(
                name: "endereco_cidade",
                schema: "public",
                table: "filiais");

            migrationBuilder.DropColumn(
                name: "endereco_complemento",
                schema: "public",
                table: "filiais");

            migrationBuilder.DropColumn(
                name: "endereco_logradouro",
                schema: "public",
                table: "filiais");

            migrationBuilder.DropColumn(
                name: "endereco_numero",
                schema: "public",
                table: "filiais");

            migrationBuilder.DropColumn(
                name: "endereco_uf",
                schema: "public",
                table: "filiais");

            migrationBuilder.CreateIndex(
                name: "uk_filiais_nome",
                schema: "public",
                table: "filiais",
                column: "nome",
                unique: true);
        }
    }
}
