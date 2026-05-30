using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agendamentos.Observacoes.Common;
using CarWash.Application.Common;
using CarWash.Application.Interfaces;
using CarWash.Domain.Entities;
using FluentValidation;

namespace CarWash.Application.Agendamentos.Observacoes.Criar;

public sealed class CriarObservacaoAgendamentoHandler
    : ICommandHandler<CriarObservacaoAgendamentoCommand, CriarObservacaoAgendamentoResponse>
{
    private readonly IAgendamentoObservacaoRepository repository;
    private readonly IValidator<CriarObservacaoAgendamentoCommand> validator;

    public CriarObservacaoAgendamentoHandler(
        IAgendamentoObservacaoRepository repository,
        IValidator<CriarObservacaoAgendamentoCommand> validator)
    {
        this.repository = repository;
        this.validator = validator;
    }

    public async Task<CriarObservacaoAgendamentoResponse> HandleAsync(
        CriarObservacaoAgendamentoCommand command,
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

        string texto = InputNormalizer.SanitizeTextOrNull(command.Texto)!;

        AgendamentoObservacao observacao = AgendamentoObservacao.Criar(
            command.AgendamentoId,
            texto,
            command.UsuarioId);

        await repository.AdicionarAsync(
            observacao,
            command.TraceId,
            cancellationToken);

        return new CriarObservacaoAgendamentoResponse
        {
            Message = "Observação logística registrada com sucesso.",
            Data = AgendamentoObservacaoMapper.ToResponse(observacao),
            TraceId = command.TraceId,
        };
    }
}
