using System.Text.Json;
using System.Text.Json.Serialization;

namespace CarWash.Application.Responsaveis.Atualizar;

public class AtualizarResponsavelRequest
{
    public string? Nome { get; set; }

    public string? Telefone { get; set; }

    public string? Email { get; set; }

    public string? GrauVinculo { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? CamposExtras { get; set; }
}
