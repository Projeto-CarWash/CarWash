using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarWash.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Habilita o ciclo de atendimento agendado → em_andamento → finalizado
    /// (RF008/RF010/RF013):
    /// <list type="bullet">
    /// <item><c>ck_hist_evento</c> passa a aceitar <c>INICIADO</c> — evento de
    /// histórico da transição para <c>em_andamento</c> (RN007).</item>
    /// <item><c>ex_ag_veiculo_janela</c> (RN011) passa a cobrir também
    /// <c>em_andamento</c>: atendimento em execução continua ocupando o
    /// veículo; concluído/cancelado liberam a janela.</item>
    /// </list>
    /// </summary>
    public partial class AtendimentoEmAndamentoEConclusao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.agendamento_historico
DROP CONSTRAINT ck_hist_evento;");

            migrationBuilder.Sql(@"
ALTER TABLE public.agendamento_historico
ADD CONSTRAINT ck_hist_evento
CHECK (evento IN ('CRIADO','EDITADO','INICIADO','CANCELADO','FINALIZADO'));");

            migrationBuilder.Sql(@"
ALTER TABLE public.agendamentos
DROP CONSTRAINT ex_ag_veiculo_janela;");

            migrationBuilder.Sql(@"
ALTER TABLE public.agendamentos
ADD CONSTRAINT ex_ag_veiculo_janela
EXCLUDE USING gist (
    veiculo_id WITH =,
    tstzrange(inicio, fim, '[)') WITH &&
)
WHERE (status IN ('agendado', 'em_andamento'));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.agendamentos
DROP CONSTRAINT ex_ag_veiculo_janela;");

            migrationBuilder.Sql(@"
ALTER TABLE public.agendamentos
ADD CONSTRAINT ex_ag_veiculo_janela
EXCLUDE USING gist (
    veiculo_id WITH =,
    tstzrange(inicio, fim, '[)') WITH &&
)
WHERE (status = 'agendado');");

            migrationBuilder.Sql(@"
ALTER TABLE public.agendamento_historico
DROP CONSTRAINT ck_hist_evento;");

            migrationBuilder.Sql(@"
ALTER TABLE public.agendamento_historico
ADD CONSTRAINT ck_hist_evento
CHECK (evento IN ('CRIADO','EDITADO','CANCELADO','FINALIZADO'));");
        }
    }
}
