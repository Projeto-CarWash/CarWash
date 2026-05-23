using CarWash.Domain.Common;
using CarWash.Domain.Enums;

namespace CarWash.Domain.Entities;

/// <summary>
/// Preferência única por usuário (RF016, DB001 §13). UNIQUE <c>uk_pref_usuario_id</c>.
/// </summary>
public sealed class UsuarioPreferencia
{
    private UsuarioPreferencia()
    {
        // EF Core reidratação.
        TemaRaw = null!;
    }

    public Guid Id { get; private set; }

    public Guid UsuarioId { get; private set; }

    public string TemaRaw { get; private set; }

    public TemaPreferencia Tema => TemaPreferenciaExtensions.FromDbValue(TemaRaw);

    public DateTime AtualizadoEm { get; private set; }

    public static UsuarioPreferencia Criar(Guid id, Guid usuarioId, TemaPreferencia tema)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Id da preferência não pode ser vazio.");
        }

        if (usuarioId == Guid.Empty)
        {
            throw new DomainException("UsuarioId da preferência não pode ser vazio.");
        }

        return new UsuarioPreferencia
        {
            Id = id,
            UsuarioId = usuarioId,
            TemaRaw = tema.ToDbValue(),
            AtualizadoEm = DateTime.UtcNow,
        };
    }

    public void DefinirTema(TemaPreferencia tema)
    {
        TemaRaw = tema.ToDbValue();
        AtualizadoEm = DateTime.UtcNow;
    }
}
