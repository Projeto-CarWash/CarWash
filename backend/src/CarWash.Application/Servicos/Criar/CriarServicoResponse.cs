namespace CarWash.Application.Servicos.Criar;

/// <summary>
/// Resposta do <c>POST /api/v1/servicos</c>. Mantém o contrato HTTP original
/// (id + mensagem + traceId), distinto do <c>ServicoResponse</c> de GET/PUT.
/// </summary>
public class CriarServicoResponse
{
    public Guid Id { get; set; }

    public string Mensagem { get; set; } = "Serviço cadastrado com sucesso.";

    public string TraceId { get; set; } = string.Empty;
}
