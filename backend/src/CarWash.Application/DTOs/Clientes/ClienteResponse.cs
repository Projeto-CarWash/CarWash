namespace CarWash.Application.DTOs.Clientes;public class ClienteResponse{
    public Guid Id{ get; set; }
    public string? Nome { get; set; } = string.Empty;
    public string? Cpf { get; set; }
    public string? Cnpj { get; set; }
    public string? Telefone { get; set; }
    public string? Celular { get; set; }
    public string? Email { get; set; }
    public string? Endereco { get; set; }
    public string? Observacoes { get; set; }
    public bool Ativo { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset AtualizadoEm { get; set; }
    public List<VeiculoResponse> Veiculos { get; set; } = [];
}
