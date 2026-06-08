using System.Linq;
using CarWash.Application.Agendamentos.Persistence;
using CarWash.Application.Common;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;

namespace CarWash.Application.Agendamentos.Common;

/// <summary>
/// Monta o <see cref="AgendamentoResponse"/> a partir do agregado persistido.
/// Compartilhado entre a criação direta (RF007) e a confirmação (RF015) para que
/// ambos os caminhos devolvam exatamente o mesmo contrato HTTP.
/// </summary>
public static class AgendamentoResponseFactory
{
    public const string MensagemSucesso = "Agendamento criado com sucesso.";

    public static AgendamentoResponse Montar(
        Agendamento agendamento,
        IReadOnlyCollection<AgendamentoItem> itens,
        IReadOnlyCollection<ServicoSnapshot> servicos,
        ResponsavelResumoSnapshot responsavel,
        string traceId)
    {
        ArgumentNullException.ThrowIfNull(agendamento);
        ArgumentNullException.ThrowIfNull(itens);
        ArgumentNullException.ThrowIfNull(servicos);
        ArgumentNullException.ThrowIfNull(responsavel);

        return new AgendamentoResponse
        {
            Id = agendamento.Id,
            FilialId = agendamento.FilialId,
            ClienteId = agendamento.ClienteId,
            VeiculoId = agendamento.VeiculoId,
            ResponsavelId = agendamento.ResponsavelId,
            Responsavel = new ResponsavelDto
            {
                Id = responsavel.Id,
                Nome = responsavel.Nome,
                Documento = DocumentoMasker.Mascarar(responsavel.Documento),
                GrauVinculo = responsavel.GrauVinculo,
            },
            Status = agendamento.Status.ToDbValue(),
            Inicio = agendamento.Inicio,
            Fim = agendamento.Fim,
            DuracaoTotalMin = agendamento.DuracaoTotalMin,
            ValorTotal = agendamento.ValorTotal,
            Observacoes = agendamento.Observacoes,
            Versao = agendamento.Versao,
            CanceladoEm = agendamento.CanceladoEm,
            CanceladoPor = agendamento.CanceladoPor,
            MotivoCancelamento = agendamento.MotivoCancelamento,
            Itens = itens
                    .Select(item => new AgendamentoServicoResponse
                    {
                        Id = item.Id,
                        ServicoId = item.ServicoId,
                        NomeServico = servicos.First(s => s.Id == item.ServicoId).Nome,
                        PrecoAplicado = item.PrecoAplicado,
                        DuracaoAplicada = item.DuracaoAplicada,
                    })
                    .ToList(),
            CriadoEm = agendamento.CriadoEm,
            Mensagem = MensagemSucesso,
            TraceId = traceId,
        };
    }
}
