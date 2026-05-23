namespace CarWash.Application.Clientes.Criar;

/// <summary>
/// Resposta do <c>POST /api/v1/clientes</c>. Mantém o contrato HTTP original
/// (id + mensagem + traceId), distinto do <c>ClienteResponse</c> de GET/PUT.
/// </summary>
public class CriarClienteResponse
{
    public Guid Id { get; set; }

    public string Mensagem { get; set; } = "Dados do cliente validados e salvos com sucesso!";

    public string TraceId { get; set; } = string.Empty;
}
