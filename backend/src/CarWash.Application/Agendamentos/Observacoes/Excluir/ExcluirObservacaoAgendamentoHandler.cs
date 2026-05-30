using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agendamentos.Observacoes.Common;
using CarWash.Application.Interfaces;

namespace CarWash.Application.Agendamentos.Observacoes.Excluir;

public sealed class ExcluirObservacaoAgendamentoHandler
    : ICommandHandler<ExcluirObservacaoAgendamentoCommand, ExcluirObservacaoAgendamentoResponse>
{
    private readonly IAgendamentoObservacaoRepository repository;

    public ExcluirObservacaoAgendamentoHandler(IAgendamentoObservacaoRepository repository)
    {
        this.repository = repository;
    }

    /// <inheritdoc/>
    public async Task<ExcluirObservacaoAgendamentoResponse> HandleAsync(
        ExcluirObservacaoAgendamentoCommand command,
        CancellationToken cancellationToken)
    {
        bool agendamentoExiste = await repository.AgendamentoExisteAsync(
            command.AgendamentoId,
            cancellationToken);

        if (!agendamentoExiste)
        {
            throw new AgendamentoNaoEncontradoException();
        }

        var observacao = await repository.ObterPorIdEAgendamentoAsync(
            command.ObservacaoId,
            command.AgendamentoId,
            cancellationToken);

        if (observacao is null)
        {
            throw new ObservacaoAgendamentoNaoEncontradaException();
        }

        if (!observacao.Ativo)
        {
            throw new ObservacaoAgendamentoEstadoInvalidoException();
        }

        string textoAnterior = observacao.Texto;

        observacao.Excluir(command.UsuarioId);

        await repository.ExcluirAsync(
            observacao,
            textoAnterior,
            command.TraceId,
            cancellationToken);

        return new ExcluirObservacaoAgendamentoResponse
        {
            Message = "Observação logística removida com sucesso.",
            TraceId = command.TraceId,
        };
    }
}
