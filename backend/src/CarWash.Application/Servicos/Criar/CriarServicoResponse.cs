namespace CarWash.Application.Servicos.Criar;

public sealed class CriarServicoResponse
{
    public Guid Id { get; set; }
    public string Mensagem { get; set; } = null!;
    public string TraceId { get; set; } = null!;
}
