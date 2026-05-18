using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarWash.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefatoraClienteEndereco : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "endereco",
                schema: "public",
                table: "clientes");

            migrationBuilder.DropColumn(
                name: "observacoes",
                schema: "public",
                table: "clientes");

            migrationBuilder.AlterColumn<string>(
                name: "celular",
                schema: "public",
                table: "clientes",
                type: "character varying(11)",
                maxLength: 11,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(11)",
                oldMaxLength: 11,
                oldNullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "data_nascimento",
                schema: "public",
                table: "clientes",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "endereco_bairro",
                schema: "public",
                table: "clientes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "endereco_cep",
                schema: "public",
                table: "clientes",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "endereco_cidade",
                schema: "public",
                table: "clientes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "endereco_complemento",
                schema: "public",
                table: "clientes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "endereco_logradouro",
                schema: "public",
                table: "clientes",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "endereco_numero",
                schema: "public",
                table: "clientes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "endereco_uf",
                schema: "public",
                table: "clientes",
                type: "character(2)",
                fixedLength: true,
                maxLength: 2,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "data_nascimento",
                schema: "public",
                table: "clientes");

            migrationBuilder.DropColumn(
                name: "endereco_bairro",
                schema: "public",
                table: "clientes");

            migrationBuilder.DropColumn(
                name: "endereco_cep",
                schema: "public",
                table: "clientes");

            migrationBuilder.DropColumn(
                name: "endereco_cidade",
                schema: "public",
                table: "clientes");

            migrationBuilder.DropColumn(
                name: "endereco_complemento",
                schema: "public",
                table: "clientes");

            migrationBuilder.DropColumn(
                name: "endereco_logradouro",
                schema: "public",
                table: "clientes");

            migrationBuilder.DropColumn(
                name: "endereco_numero",
                schema: "public",
                table: "clientes");

            migrationBuilder.DropColumn(
                name: "endereco_uf",
                schema: "public",
                table: "clientes");

            migrationBuilder.AlterColumn<string>(
                name: "celular",
                schema: "public",
                table: "clientes",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(11)",
                oldMaxLength: 11);

            migrationBuilder.AddColumn<string>(
                name: "endereco",
                schema: "public",
                table: "clientes",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "observacoes",
                schema: "public",
                table: "clientes",
                type: "text",
                nullable: true);
        }
    }
}
