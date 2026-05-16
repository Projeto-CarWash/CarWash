using CarWash.Domain.Common;

namespace CarWash.Domain.Entities;

/// <summary>
/// Sessão / refresh token persistida apenas como SHA-256 do token bruto
/// (DB001 §06.5, decisão P05). Append + revogação por <c>revogado_em</c>.
/// </summary>
public sealed class UsuarioSessao
{
    private UsuarioSessao()
    {
        // EF Core reidratação.
        RefreshTokenHash = null!;
    }

    public Guid Id { get; private set; }

    public Guid UsuarioId { get; private set; }

    public string RefreshTokenHash { get; private set; }

    public DateTime ExpiraEm { get; private set; }

    public DateTime? RevogadoEm { get; private set; }

    public string? IpOrigem { get; private set; }

    public string? UserAgent { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public bool EstaAtiva(DateTime referencia) =>
        RevogadoEm is null && ExpiraEm > referencia;

    public static UsuarioSessao Criar(
        Guid id,
        Guid usuarioId,
        string refreshTokenHash,
        DateTime expiraEm,
        string? ipOrigem = null,
        string? userAgent = null)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Id da sessão não pode ser vazio.");
        }

        if (usuarioId == Guid.Empty)
        {
            throw new DomainException("UsuarioId da sessão não pode ser vazio.");
        }

        if (string.IsNullOrWhiteSpace(refreshTokenHash))
        {
            throw new DomainException("Hash do refresh token não pode ser vazio.");
        }

        if (expiraEm <= DateTime.UtcNow)
        {
            throw new DomainException("Data de expiração deve estar no futuro.");
        }

        return new UsuarioSessao
        {
            Id = id,
            UsuarioId = usuarioId,
            RefreshTokenHash = refreshTokenHash,
            ExpiraEm = expiraEm,
            IpOrigem = ipOrigem,
            UserAgent = userAgent,
            CriadoEm = DateTime.UtcNow,
        };
    }

    public void Revogar(DateTime quando)
    {
        if (RevogadoEm is not null)
        {
            return;
        }

        RevogadoEm = quando;
    }
}
