using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Usuarios.Common;
using CarWash.Application.Usuarios.Persistence;

namespace CarWash.Application.Usuarios.ObterUsuarioPorId;

public sealed class ObterUsuarioPorIdHandler : IQueryHandler<ObterUsuarioPorIdQuery, UsuarioResponse>
{
    public const string MensagemNaoEncontrado = "Usuário não encontrado.";

    private readonly IUsuarioRepository _repositorio;

    public ObterUsuarioPorIdHandler(IUsuarioRepository repositorio)
    {
        _repositorio = repositorio;
    }

    /// <inheritdoc/>
    public async Task<UsuarioResponse> HandleAsync(ObterUsuarioPorIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var usuario = await _repositorio.ObterPorIdAsync(query.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(MensagemNaoEncontrado);

        return UsuarioResponse.FromEntity(usuario);
    }
}
