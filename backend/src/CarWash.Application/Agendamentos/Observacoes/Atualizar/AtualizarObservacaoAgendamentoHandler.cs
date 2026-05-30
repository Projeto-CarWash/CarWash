using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agendamentos.Observacoes.Common;
using CarWash.Application.Common;
using CarWash.Application.Interfaces;
using FluentValidation;

namespace CarWash.Application.Agendamentos.Observacoes.Atualizar;

public sealed class AtualizarObservacaoAgendamentoHandler
    : ICommandHandler<AtualizarObservacaoAgendamentoCommand, AtualizarObservacaoAgendamentoResponse>
{
    private readonly IAgendamentoObservacaoRepository repository;
    private readonly IValidator<AtualizarObservacaoAgendamentoCommand> validator;

    public AtualizarObservacaoAgendamentoHandler(
        IAgendamentoObservacaoRepository repository,
        IValidator<AtualizarObservacaoAgendamentoCommand> validator)
    {
        this.repository = repository;
        this.validator = validator;
    }

    /// <inheritdoc/>
    public async Task<AtualizarObservacaoAgendamentoResponse> HandleAsync(
        AtualizarObservacaoAgendamentoCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

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
        string textoNovo = InputNormalizer.SanitizeTextOrNull(command.Texto)!;

        observacao.AtualizarTexto(textoNovo, command.UsuarioId);

        await repository.AtualizarAsync(
            observacao,
            textoAnterior,
            command.TraceId,
            cancellationToken);

        return new AtualizarObservacaoAgendamentoResponse
        {
            Message = "Observação logística atualizada com sucesso.",
            Data = AgendamentoObservacaoMapper.ToResponse(observacao),
            TraceId = command.TraceId,
        };
    }
}
