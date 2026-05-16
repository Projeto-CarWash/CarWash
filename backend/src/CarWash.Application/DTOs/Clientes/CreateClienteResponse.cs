namespace CarWash.Application.DTOs.Clientes;

public class CreateClienteResponse
{
    public Guid Id { get; set; }

    public string Mensagem { get; set; } = "Cliente Cadastrado";

    public string TraceId { get; set; } = string.Empty;
}
