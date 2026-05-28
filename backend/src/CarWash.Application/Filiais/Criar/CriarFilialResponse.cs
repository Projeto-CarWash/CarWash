namespace CarWash.Application.Filiais.Criar;

/// <summary>
/// Resposta do <c>POST /api/v1/filiais</c>. Mantém o envelope canônico
/// (id + mensagem + traceId) do projeto (ADR-0007 §4.1).
/// </summary>
public class CriarFilialResponse
{
    public Guid Id { get; set; }

    public string Mensagem { get; set; } = "Filial cadastrada com sucesso.";

    public string TraceId { get; set; } = string.Empty;
}
