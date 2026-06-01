namespace CarWash.Application.Filiais.Common;

/// <summary>
/// Endereço estruturado opcional no payload de cadastro/edição de filial
/// (RF017). Distinto do <c>EnderecoRequest</c> de Clientes para evitar
/// acoplamento de validators entre slices.
/// </summary>
public class EnderecoFilialRequest
{
    public string? Cep { get; set; }

    public string? Logradouro { get; set; }

    public string? Numero { get; set; }

    public string? Complemento { get; set; }

    public string? Bairro { get; set; }

    public string? Cidade { get; set; }

    public string? Uf { get; set; }
}
