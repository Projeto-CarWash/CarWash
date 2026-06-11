using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Common;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Responsaveis.Common;
using CarWash.Application.Responsaveis.Persistence;
using CarWash.Domain.Enums;
using CarWash.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Responsaveis.Atualizar;

public sealed class AtualizarResponsavelHandler : ICommandHandler<AtualizarResponsavelCommand, ResponsavelResponse>
{
    private readonly IResponsavelRepository _repositorio;
    private readonly ILogger<AtualizarResponsavelHandler> _log;

    public AtualizarResponsavelHandler(
        IResponsavelRepository repositorio,
        ILogger<AtualizarResponsavelHandler> log)
    {
        _repositorio = repositorio;
        _log = log;
    }

    public async Task<ResponsavelResponse> HandleAsync(AtualizarResponsavelCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CamposExtras is { Count: > 0 })
        {
            var camposNaoEditaveis = command.CamposExtras.Keys
                .Where(k => string.Equals(k, "documento", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(k, "cpf", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(k, "cnpj", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(k, "ativo", StringComparison.OrdinalIgnoreCase));

            foreach (string? campo in camposNaoEditaveis)
            {
                _log.LogWarning(
                    "PUT /responsaveis/{ResponsavelId} recebeu campo não editável '{Campo}' — ignorado. UsuarioId={UsuarioId}",
                    command.ResponsavelId,
                    campo,
                    command.UsuarioId);
            }
        }

        var responsavel = await _repositorio.ObterPorIdRastreadoAsync(command.ResponsavelId, command.ClienteTitularId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Responsável não encontrado.");

        string nome = InputNormalizer.SanitizeTextOrNull(command.Nome)!;
        string? telefoneDigits = InputNormalizer.OnlyDigitsOrNull(command.Telefone);
        string? emailNormalizado = InputNormalizer.EmailOrNull(command.Email);
        var grauVinculo = GrauVinculoExtensions.FromDbValue(command.GrauVinculo!);

        var telefone = telefoneDigits is null ? null : new Telefone(telefoneDigits);
        var email = emailNormalizado is null ? null : new Email(emailNormalizado);

        responsavel.AtualizarDados(nome, telefone?.Valor, email?.Valor, grauVinculo);

        await _repositorio.SalvarAsync(command.TraceId, command.UsuarioId, cancellationToken).ConfigureAwait(false);

        return ResponsavelResponse.FromEntity(responsavel);
    }
}
