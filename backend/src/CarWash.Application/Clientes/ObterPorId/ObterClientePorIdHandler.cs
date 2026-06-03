using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Clientes.Common;
using CarWash.Application.Clientes.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Responsaveis.Common;
using CarWash.Application.Responsaveis.Persistence;

namespace CarWash.Application.Clientes.ObterPorId;

public sealed class ObterClientePorIdHandler : IQueryHandler<ObterClientePorIdQuery, ClienteResponse>
{
    public const string MensagemNaoEncontrado = "Cliente não encontrado.";

    private readonly IClienteRepository _repositorio;
    private readonly IResponsavelRepository _responsaveis;

    public ObterClientePorIdHandler(IClienteRepository repositorio, IResponsavelRepository responsaveis)
    {
        _repositorio = repositorio;
        _responsaveis = responsaveis;
    }

    /// <inheritdoc/>
    public async Task<ClienteResponse> HandleAsync(ObterClientePorIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var cliente = await _repositorio.ObterPorIdAsync(query.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(MensagemNaoEncontrado);

        var response = ClienteResponse.FromEntity(cliente);

        var responsaveis = await _responsaveis.ListarPorClienteTitularIdAsync(query.Id, cancellationToken).ConfigureAwait(false);
        response.Responsaveis = responsaveis.Select(r => new ResponsavelResponse
        {
            Id = r.Id,
            ClienteTitularId = r.ClienteTitularId,
            Nome = r.Nome,
            Documento = r.Documento,
            Telefone = r.Telefone,
            Email = r.Email,
            GrauVinculo = r.GrauVinculo,
            Ativo = r.Ativo,
            CriadoEm = r.CriadoEm,
            AtualizadoEm = r.AtualizadoEm,
        }).ToList();

        return response;
    }
}
