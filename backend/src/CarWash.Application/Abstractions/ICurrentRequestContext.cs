namespace CarWash.Application.Abstractions;

/// <summary>
/// Abstração única para o contexto da requisição em curso (DB001 §06.1).
/// Consolidação de <c>IAuditContext</c> + <c>ICurrentUserService</c> em uma única
/// dependência scoped — reduz injeção repetitiva nas Use Cases.
/// </summary>
public interface ICurrentRequestContext
{
    /// <summary>
    /// Identificador único da request — propagado pelo <c>CorrelationIdMiddleware</c>.
    /// Em background jobs/tests, recebe um Guid recém-gerado.
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// Usuário autenticado (claims). Null quando anônimo (ex.: login mal-sucedido).
    /// </summary>
    Guid? UsuarioId { get; }

    /// <summary>
    /// Evento de aplicação atualmente em curso (ex.: <c>"AgendamentoCriado"</c>).
    /// Usado pelo <c>AuditLogInterceptor</c> para preencher <c>audit_logs.evento</c>.
    /// </summary>
    string? EventoAtual { get; }

    /// <summary>
    /// Define o evento atual — geralmente chamado no início de um Use Case
    /// para que o interceptor saiba sob qual nome registrar.
    /// </summary>
    void DefinirEvento(string evento);

    /// <summary>
    /// IP de origem da request (lido de <c>HttpContext.Connection.RemoteIpAddress</c>,
    /// considerando <c>X-Forwarded-For</c> se houver proxy reverso configurado).
    /// Persistido em <c>usuario_sessoes.ip_origem</c> no login para rastreabilidade.
    /// </summary>
    string? IpOrigem { get; }

    /// <summary>
    /// User-Agent da request (lido do header HTTP). Persistido em
    /// <c>usuario_sessoes.user_agent</c> no login para rastreabilidade.
    /// </summary>
    string? UserAgent { get; }
}
