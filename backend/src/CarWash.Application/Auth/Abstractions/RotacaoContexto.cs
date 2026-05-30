using CarWash.Application.Auth.Persistence;
using CarWash.Domain.Entities;

namespace CarWash.Application.Auth.Abstractions;

/// <summary>
/// Contexto de rotação retornado por <see cref="IRefreshTokenService.ValidarParaRotacaoAsync"/>:
/// sessão atual (bloqueada por <c>FOR UPDATE</c>) + transação que o chamador deve
/// commitar após persistir a nova sessão. Sempre disposer (idealmente
/// <c>await using</c>) — dispose sem commit equivale a rollback.
/// </summary>
public sealed record RotacaoContexto(UsuarioSessao SessaoAtual, IUsuarioSessaoTransacao Transacao);
