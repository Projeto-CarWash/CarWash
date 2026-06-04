using System.Linq;
using CarWash.Application.Agendamentos.Persistence;
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
                Documento = MascararDocumento(responsavel.Documento),
                GrauVinculo = responsavel.GrauVinculo,
            },
            Status = agendamento.Status.ToDbValue(),
            Inicio = agendamento.Inicio,
            Fim = agendamento.Fim,
            DuracaoTotalMin = agendamento.DuracaoTotalMin,
            ValorTotal = agendamento.ValorTotal,
            Observacoes = agendamento.Observacoes,
            Versao = agendamento.Versao,
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

    /// <summary>
    /// Mascara o documento do responsável para o payload de resposta (RF024):
    /// ex.: "123.456.789-00" → "123.***.***-**". CPF mantém os 3 primeiros
    /// dígitos e o DV; CNPJ mantém os 2 primeiros e o DV.
    /// </summary>
    private static string MascararDocumento(string documento)
    {
        if (string.IsNullOrWhiteSpace(documento))
        {
            return documento;
        }

        var digitos = new string(documento.Where(char.IsDigit).ToArray());

        if (digitos.Length == 11)
        {
            return $"{digitos[..3]}.***.***-{digitos[^2..]}";
        }

        if (digitos.Length == 14)
        {
            return $"{digitos[..2]}.***.***/{digitos[^6..4]}.{digitos[^2..]}";
        }

        return new string('*', documento.Length);
    }
}
