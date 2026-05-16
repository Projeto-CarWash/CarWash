namespace CarWash.Application.DTOs.Clientes;

public class CreateClienteResponse
{
    public Guid Id { get; set; }

    public string Mensagem { get; set; } = "Dados do cliente validados e salvos com sucesso!";

    public string TraceId { get; set; } = string.Empty;
}
