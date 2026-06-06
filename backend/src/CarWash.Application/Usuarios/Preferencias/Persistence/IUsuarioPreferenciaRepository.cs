using CarWash.Domain.Entities;
using CarWash.Domain.Enums;

namespace CarWash.Application.Usuarios.Preferencias.Persistence;

public interface IUsuarioPreferenciaRepository
{
    Task<UsuarioPreferencia?> ObterPorUsuarioIdAsync(
        Guid usuarioId,
        CancellationToken cancellationToken);

    Task<bool> UsuarioExisteAsync(
        Guid usuarioId,
        CancellationToken cancellationToken);

    Task AdicionarAsync(
        UsuarioPreferencia preferencia,
        CancellationToken cancellationToken);

    Task SalvarAsync(CancellationToken cancellationToken);
}
