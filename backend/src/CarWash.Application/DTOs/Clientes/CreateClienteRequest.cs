namespace CarWash.Application.DTOs.Clientes;

public class CreateClienteRequest{
    public string? Nome { get; set; }
    public string? Cpf { get; set; }
    public string? Cnpj { get; set; }
    public string? Telefone { get; set; }
    public string? Celular { get; set; }
    public string? Email { get; set; }
    public string? Endereco { get; set; }
    public string? Observacoes { get; set; }
    public List<CreateVeiculoRequest>? Veiculos { get; set; }
}
