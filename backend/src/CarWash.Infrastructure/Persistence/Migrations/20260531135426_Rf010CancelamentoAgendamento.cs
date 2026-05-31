using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarWash.Infrastructure.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class Rf010CancelamentoAgendamento : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropCheckConstraint(
				name: "ck_ag_status",
				schema: "public",
				table: "agendamentos");

			migrationBuilder.AddColumn<DateTime>(
				name: "cancelado_em",
				schema: "public",
				table: "agendamentos",
				type: "timestamptz",
				nullable: true);

			migrationBuilder.AddColumn<Guid>(
				name: "cancelado_por",
				schema: "public",
				table: "agendamentos",
				type: "uuid",
				nullable: true);

			migrationBuilder.AddColumn<string>(
				name: "motivo_cancelamento",
				schema: "public",
				table: "agendamentos",
				type: "text",
				nullable: true);

			migrationBuilder.AddCheckConstraint(
				name: "ck_ag_status",
				schema: "public",
				table: "agendamentos",
				sql: "status IN ('agendado','em_andamento','cancelado','finalizado')");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropCheckConstraint(
				name: "ck_ag_status",
				schema: "public",
				table: "agendamentos");

			migrationBuilder.DropColumn(
				name: "cancelado_em",
				schema: "public",
				table: "agendamentos");

			migrationBuilder.DropColumn(
				name: "cancelado_por",
				schema: "public",
				table: "agendamentos");

			migrationBuilder.DropColumn(
				name: "motivo_cancelamento",
				schema: "public",
				table: "agendamentos");

			migrationBuilder.AddCheckConstraint(
				name: "ck_ag_status",
				schema: "public",
				table: "agendamentos",
				sql: "status IN ('agendado','cancelado','finalizado')");
		}
	}
}
