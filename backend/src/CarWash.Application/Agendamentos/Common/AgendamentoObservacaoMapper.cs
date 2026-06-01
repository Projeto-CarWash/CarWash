using CarWash.Domain.Entities;

namespace CarWash.Application.Agendamentos.Observacoes.Common;

public static class AgendamentoObservacaoMapper
{
    public static AgendamentoObservacaoResponse ToResponse(AgendamentoObservacao observacao)
    {
        return new AgendamentoObservacaoResponse
        {
            Id = observacao.Id,
            AgendamentoId = observacao.AgendamentoId,
            Texto = observacao.Texto,
            Ativo = observacao.Ativo,
            CriadoEm = observacao.CriadoEm,
            CriadoPor = observacao.CriadoPor,
            AtualizadoEm = observacao.AtualizadoEm,
            AtualizadoPor = observacao.AtualizadoPor,
            ExcluidoEm = observacao.ExcluidoEm,
            ExcluidoPor = observacao.ExcluidoPor,
        };
    }
}
