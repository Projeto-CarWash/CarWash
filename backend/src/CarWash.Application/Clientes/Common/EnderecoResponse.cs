namespace CarWash.Application.Clientes.Common;

/// <summary>
/// Representação estruturada do endereço no payload de saída.
/// </summary>
public class EnderecoResponse
{
    public string Cep { get; set; } = string.Empty;

    public string Logradouro { get; set; } = string.Empty;

    public string Numero { get; set; } = string.Empty;

    public string? Complemento { get; set; }

    public string Bairro { get; set; } = string.Empty;

    public string Cidade { get; set; } = string.Empty;

    public string Uf { get; set; } = string.Empty;
}
