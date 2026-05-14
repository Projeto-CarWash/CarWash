using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarWash.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjustaConstraintClienteCpfCnpj : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_clientes_cpf_ou_cnpj",
                schema: "public",
                table: "clientes");

            migrationBuilder.AddCheckConstraint(
                name: "ck_clientes_cpf_ou_cnpj",
                schema: "public",
                table: "clientes",
                sql: "(cpf is not null and cnpj is null) or (cpf is null and cnpj is not null)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_clientes_cpf_ou_cnpj",
                schema: "public",
                table: "clientes");

            migrationBuilder.AddCheckConstraint(
                name: "ck_clientes_cpf_ou_cnpj",
                schema: "public",
                table: "clientes",
                sql: "cpf IS NOT NULL OR cnpj IS NOT NULL");
        }
    }
}
