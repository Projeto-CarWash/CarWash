namespace CarWash.Application.Common.Exceptions;

/// <summary>
/// Reuse de refresh token detectado (CA011): o hash apresentado existe mas a
/// sessão correspondente já estava revogada e o token ainda não expirou por tempo.
/// Sinal forte de exfiltração. O serviço de refresh deve revogar toda a família
/// de sessões ativas do usuário antes de lançar esta exceção. Externamente é
/// indistinguível de <see cref="RefreshTokenInvalidoException"/> (mesmo 401, mesmo
/// payload) para não revelar ao atacante que o evento foi detectado.
/// </summary>
public sealed class RefreshTokenReuseDetectadoException : RefreshTokenInvalidoException
{
    public RefreshTokenReuseDetectadoException(Guid usuarioId, Guid sessaoComprometidaId, int sessoesAfetadas)
        : base("Refresh token inválido ou expirado.")
    {
        UsuarioId = usuarioId;
        SessaoComprometidaId = sessaoComprometidaId;
        SessoesAfetadas = sessoesAfetadas;
    }

    public Guid UsuarioId { get; }

    public Guid SessaoComprometidaId { get; }

    public int SessoesAfetadas { get; }
}
