using CarWash.Application.Abstractions;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Usuarios.Preferencias.Common;
using CarWash.Application.Usuarios.Preferencias.Persistence;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Usuarios.Preferencias.AtualizarTema;

public sealed class AtualizarTemaUsuarioHandler
    : ICommandHandler<AtualizarTemaUsuarioCommand, UsuarioPreferenciasResponse>
{
    private readonly IUsuarioPreferenciaRepository _repository;
    private readonly IValidator<AtualizarTemaUsuarioCommand> _validator;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<AtualizarTemaUsuarioHandler> _logger;

    public AtualizarTemaUsuarioHandler(
        IUsuarioPreferenciaRepository repository,
        IValidator<AtualizarTemaUsuarioCommand> validator,
        IAuditLogger auditLogger,
        ILogger<AtualizarTemaUsuarioHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<UsuarioPreferenciasResponse> HandleAsync(
        AtualizarTemaUsuarioCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await _validator
            .ValidateAsync(command, cancellationToken)
            .ConfigureAwait(false);

        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        bool usuarioExiste = await _repository
            .UsuarioExisteAsync(command.UsuarioId, cancellationToken)
            .ConfigureAwait(false);

        if (!usuarioExiste)
        {
            throw new UnauthorizedAccessException("Usuário autenticado não encontrado.");
        }

        var temaNovo = TemaPreferenciaExtensions.FromApiValue(command.Theme!);

        var preferencia = await _repository
            .ObterPorUsuarioIdAsync(command.UsuarioId, cancellationToken)
            .ConfigureAwait(false);

        string temaAnterior = preferencia?.TemaRaw ?? "light";

        if (preferencia is null)
        {
            preferencia = UsuarioPreferencia.Criar(
                Guid.NewGuid(),
                command.UsuarioId,
                temaNovo);

            await _repository
                .AdicionarAsync(preferencia, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            preferencia.DefinirTema(temaNovo);
        }

        await _repository
            .SalvarAsync(cancellationToken)
            .ConfigureAwait(false);

        string temaAtualizado = preferencia.TemaRaw;

        _logger.LogInformation(
            "Preferência de tema atualizada. TraceId={TraceId}, UsuarioId={UsuarioId}, TemaAnterior={TemaAnterior}, TemaNovo={TemaNovo}, TimestampUtc={TimestampUtc}, Origem=api",
            command.TraceId,
            command.UsuarioId,
            temaAnterior,
            temaAtualizado,
            DateTime.UtcNow);

        await _auditLogger.LogAsync(
            evento: "UsuarioPreferenciaTemaAlterada",
            entidade: "UsuarioPreferencia",
            entidadeId: preferencia.Id,
            dados: new
            {
                command.UsuarioId,
                TemaAnterior = temaAnterior,
                TemaNovo = temaAtualizado,
                TimestampUtc = DateTime.UtcNow,
                Origem = "api",
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new UsuarioPreferenciasResponse
        {
            Message = "Preferência de tema atualizada com sucesso.",
            Data = new UsuarioPreferenciasDataResponse
            {
                Theme = temaAtualizado,
            },
            TraceId = command.TraceId,
        };
    }
}
