using CarWash.Application.Abstractions;

namespace CarWash.Infrastructure.Auditing;

/// <summary>
/// Implementação default de <see cref="ICurrentRequestContext"/> baseada em
/// <see cref="AsyncLocal{T}"/>. Usada em background jobs, testes e como fallback
/// quando não há <c>HttpContext</c>. A implementação HTTP em
/// <c>CarWash.Api</c> sobrescreve este registro no escopo da request.
/// </summary>
public sealed class AmbientRequestContext : ICurrentRequestContext
{
    private static readonly AsyncLocal<RequestState?> Estado = new();

    public string CorrelationId
    {
        get
        {
            var atual = Estado.Value;
            if (atual is null)
            {
                atual = new RequestState { CorrelationId = Guid.NewGuid().ToString("N") };
                Estado.Value = atual;
            }

            return atual.CorrelationId;
        }
    }

    public Guid? UsuarioId => Estado.Value?.UsuarioId;

    public string? EventoAtual => Estado.Value?.EventoAtual;

    public void DefinirEvento(string evento)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evento);
        var atual = Estado.Value ?? new RequestState { CorrelationId = Guid.NewGuid().ToString("N") };
        atual.EventoAtual = evento;
        Estado.Value = atual;
    }

    public static void DefinirUsuario(Guid? usuarioId)
    {
        var atual = Estado.Value ?? new RequestState { CorrelationId = Guid.NewGuid().ToString("N") };
        atual.UsuarioId = usuarioId;
        Estado.Value = atual;
    }

    public static void DefinirCorrelationId(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        var atual = Estado.Value ?? new RequestState { CorrelationId = correlationId };
        atual.CorrelationId = correlationId;
        Estado.Value = atual;
    }

    public static void Reset() => Estado.Value = null;

    private sealed class RequestState
    {
        public required string CorrelationId { get; set; }

        public Guid? UsuarioId { get; set; }

        public string? EventoAtual { get; set; }
    }
}
