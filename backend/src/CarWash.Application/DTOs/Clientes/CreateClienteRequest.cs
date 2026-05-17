using System.Text.Json;
using System.Text.Json.Serialization;

namespace CarWash.Application.DTOs.Clientes;

public class CreateClienteRequest
{
    public string? Nome { get; set; }

    public DateOnly? DataNascimento { get; set; }

    public string? Cpf { get; set; }

    public string? Cnpj { get; set; }

    public string? Telefone { get; set; }

    public string? Celular { get; set; }

    public string? Email { get; set; }

    public EnderecoRequest? Endereco { get; set; }
}

public class EnderecoRequest
{
    public string? Cep { get; set; }

    public string? Logradouro { get; set; }

    public string? Numero { get; set; }

    public string? Complemento { get; set; }

    public string? Bairro { get; set; }

    public string? Cidade { get; set; }

    public string? Uf { get; set; }
}

public class UpdateClienteRequest
{
    public string? Nome { get; set; }

    public DateOnly? DataNascimento { get; set; }

    public string? Telefone { get; set; }

    public string? Celular { get; set; }

    public string? Email { get; set; }

    public EnderecoRequest? Endereco { get; set; }

    /// <summary>
    /// Campos não mapeados do JSON (Opção B do GAP-CW-CLI-PUT-CPF em .NET 8).
    /// O System.Text.Json popula este dicionário com qualquer propriedade extra
    /// presente no body (ex.: <c>cpf</c>, <c>cnpj</c>) — o Service usa para emitir
    /// warning, sinalizando ao cliente que CPF/CNPJ não são editáveis via PUT.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? CamposExtras { get; set; }
}

public class AlterarStatusClienteRequest
{
    /// <summary>
    /// Estado-alvo do cliente. Nullable porque o endpoint exige o campo presente
    /// no body — body <c>{}</c> deve falhar com 400 (GAP-CW-CLI-STA-EMP),
    /// não cair em <c>default(bool) = false</c> silenciosamente.
    /// </summary>
    public bool? Ativo { get; set; }
}
