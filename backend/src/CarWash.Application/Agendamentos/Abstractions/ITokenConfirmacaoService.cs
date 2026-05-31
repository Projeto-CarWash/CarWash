using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Agendamentos.Abstractions;

/// <summary>
/// Emite e valida o <c>tokenConfirmacao</c> da confirmação de agendamento em duas
/// etapas (RF015 / ADR 0004). O token é stateless, assinado com HMAC-SHA256, e
/// carrega o hash do resumo, o usuário, o trace e a expiração (15 min).
/// </summary>
public interface ITokenConfirmacaoService
{
    /// <summary>
    /// Gera um token assinado para o resumo informado. A expiração é fixada em
    /// 15 minutos a partir de agora (UTC).
    /// </summary>
    /// <returns></returns>
    string Gerar(string hashResumo, Guid usuarioId, string traceId);

    /// <summary>
    /// Valida o token contra o usuário autenticado. Formato/assinatura inválidos
    /// ou usuário divergente lançam <see cref="TokenConfirmacaoInvalidoException"/>
    /// (400); token íntegro porém expirado lança
    /// <see cref="SessaoConfirmacaoExpiradaException"/> (410). A expiração só é
    /// avaliada após a assinatura conferir.
    /// </summary>
    /// <returns></returns>
    TokenConfirmacaoPayload Validar(string token, Guid usuarioAutenticadoId);
}

/// <summary>
/// Conteúdo verificado de um <c>tokenConfirmacao</c> — devolvido por
/// <see cref="ITokenConfirmacaoService.Validar"/> após a assinatura e a
/// expiração conferirem.
/// </summary>
public sealed record TokenConfirmacaoPayload(string HashResumo, Guid UsuarioId, string TraceId, DateTime ExpiraEm);
