using System.Text.Json;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Servicos.Common;
using CarWash.Application.Servicos.Persistence;

namespace CarWash.Application.Servicos.AlterarStatus;

/// <summary>
/// Use case de ativar/inativar serviço. <c>PATCH /api/v1/servicos/{id}/status</c>.
/// </summary>
public sealed class AlterarStatusServicoHandler
    : ICommandHandler<AlterarStatusServicoCommand, ServicoResponse>
{
    private readonly IServicoRepository _repositorio;

    public AlterarStatusServicoHandler(IServicoRepository repositorio)
    {
        _repositorio = repositorio;
    }

    /// <inheritdoc/>
    public async Task<ServicoResponse> HandleAsync(AlterarStatusServicoCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validator garante NotNull antes de chegar aqui. `.Value` é seguro.
        bool ativo = command.Ativo!.Value;

        var servico = await _repositorio.ObterPorIdAsync(command.ServicoId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Serviço não encontrado.");

        string evento = ativo ? "SERVICO_ATIVADO" : "SERVICO_DESATIVADO";

        if (ativo)
        {
            servico.Ativar();
        }
        else
        {
            servico.Inativar();
        }

        await _repositorio.SalvarAsync(cancellationToken).ConfigureAwait(false);

        await _repositorio.RegistrarAuditoriaAsync(
            evento: evento,
            entidadeId: servico.Id,
            correlationId: command.TraceId,
            usuarioId: command.UsuarioId,
            dados: JsonSerializer.Serialize(new
            {
                servico.Id,
                servico.Nome,
                Ativo = ativo,
            }),
            cancellationToken).ConfigureAwait(false);

        return ServicoResponse.FromEntity(servico);
    }
}
