using System.Text.Json;
using System.Text.Json.Serialization;
using CarWash.Application.Clientes.Common;

namespace CarWash.Application.Clientes.Atualizar;

/// <summary>
/// DTO de entrada do <c>PUT /api/v1/clientes/{id}</c>. CPF/CNPJ não são editáveis
/// — campos extras viram <see cref="CamposExtras"/> e são apenas logados.
/// </summary>
public class AtualizarClienteRequest
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
    /// presente no body (ex.: <c>cpf</c>, <c>cnpj</c>) — o handler usa para emitir
    /// warning, sinalizando ao cliente que CPF/CNPJ não são editáveis via PUT.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? CamposExtras { get; set; }
}
