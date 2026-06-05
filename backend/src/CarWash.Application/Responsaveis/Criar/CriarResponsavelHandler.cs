using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Clientes.Persistence;
using CarWash.Application.Common;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Responsaveis.Common;
using CarWash.Application.Responsaveis.Persistence;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using CarWash.Domain.ValueObjects;

namespace CarWash.Application.Responsaveis.Criar;

public sealed class CriarResponsavelHandler : ICommandHandler<CriarResponsavelCommand, CriarResponsavelResponse>
{
    private readonly IClienteRepository _clientes;
    private readonly IResponsavelRepository _responsaveis;

    public CriarResponsavelHandler(IClienteRepository clientes, IResponsavelRepository responsaveis)
    {
        _clientes = clientes;
        _responsaveis = responsaveis;
    }

    public async Task<CriarResponsavelResponse> HandleAsync(CriarResponsavelCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var cliente = await _clientes.ObterPorIdAsync(command.ClienteTitularId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Cliente titular não encontrado.");

        var nome = InputNormalizer.SanitizeTextOrNull(command.Nome)!;
        var documentoDigits = InputNormalizer.OnlyDigitsOrNull(command.Documento)!;
        var telefoneDigits = InputNormalizer.OnlyDigitsOrNull(command.Telefone);
        var emailNormalizado = InputNormalizer.EmailOrNull(command.Email);

        if (await _responsaveis.ExisteDocumentoAsync(documentoDigits, cancellationToken).ConfigureAwait(false))
        {
            throw new DocumentoResponsavelJaExisteException();
        }

        Telefone? telefone = telefoneDigits is null ? null : new Telefone(telefoneDigits);
        Email? email = emailNormalizado is null ? null : new Email(emailNormalizado);

        var grauVinculo = GrauVinculoExtensions.FromDbValue(command.GrauVinculo!);

        var responsavel = Responsavel.Criar(
            id: Guid.NewGuid(),
            clienteTitularId: cliente.Id,
            nome: nome,
            documento: documentoDigits,
            grauVinculo: grauVinculo,
            telefone: telefone,
            email: email);

        await _responsaveis.AdicionarAsync(responsavel, command.TraceId, command.UsuarioId, cancellationToken).ConfigureAwait(false);

        return new CriarResponsavelResponse
        {
            Message = "Responsável cadastrado com sucesso.",
            Data = new CriarResponsavelData
            {
                ResponsavelId = responsavel.Id,
                ClienteTitularId = responsavel.ClienteTitularId,
                Nome = responsavel.Nome,
                Documento = responsavel.Documento,
                Telefone = responsavel.Telefone,
                Email = responsavel.Email,
                GrauVinculo = responsavel.GrauVinculo,
            },
            TraceId = command.TraceId,
        };
    }
}
