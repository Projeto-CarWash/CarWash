using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CarWash.Infrastructure.Security;

/// <summary>
/// Mascarador de campos sensíveis antes de serializar em <c>audit_logs.dados</c>
/// (DB001 §06.1.3). Sempre aplica antes do <c>JsonSerializer.Serialize</c>.
/// </summary>
public static class AuditDataMasker
{
    private static readonly HashSet<string> CamposTotalmenteMascarados =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "senha",
            "senha_hash",
            "senhahash",
            "password",
            "password_hash",
            "passwordhash",
            "refresh_token",
            "refreshtoken",
            "refresh_token_hash",
            "refreshtokenhash",
        };

    public static string Mask(object payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var node = JsonSerializer.SerializeToNode(payload);
        if (node is null)
        {
            return "{}";
        }

        MaskNode(node);
        return node.ToJsonString();
    }

    public static string MaskJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json;
        }

        var node = JsonNode.Parse(json);
        if (node is null)
        {
            return json;
        }

        MaskNode(node);
        return node.ToJsonString();
    }

    private static void MaskNode(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                MaskObject(obj);
                break;
            case JsonArray arr:
                foreach (var item in arr.Where(i => i is not null))
                {
                    MaskNode(item!);
                }

                break;
        }
    }

    private static void MaskObject(JsonObject obj)
    {
        var alteracoes = new List<KeyValuePair<string, JsonNode?>>();
        foreach (var kvp in obj)
        {
            if (CamposTotalmenteMascarados.Contains(kvp.Key))
            {
                alteracoes.Add(new KeyValuePair<string, JsonNode?>(kvp.Key, "***"));
                continue;
            }

            if (string.Equals(kvp.Key, "cpf", StringComparison.OrdinalIgnoreCase))
            {
                var valor = ReadTextValue(kvp.Value);
                alteracoes.Add(new KeyValuePair<string, JsonNode?>(kvp.Key, MaskCpf(valor)));
                continue;
            }

            if (string.Equals(kvp.Key, "cnpj", StringComparison.OrdinalIgnoreCase))
            {
                var valor = ReadTextValue(kvp.Value);
                alteracoes.Add(new KeyValuePair<string, JsonNode?>(kvp.Key, MaskCnpj(valor)));
                continue;
            }

            if (kvp.Value is JsonObject || kvp.Value is JsonArray)
            {
                MaskNode(kvp.Value!);
            }
        }

        foreach (var alt in alteracoes)
        {
            obj[alt.Key] = alt.Value;
        }
    }

    private static string? ReadTextValue(JsonNode? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is JsonValue jsonValue)
        {
            if (jsonValue.TryGetValue<string>(out var texto))
            {
                return texto;
            }

            return jsonValue.ToString();
        }

        return value.ToString();
    }

    private static string MaskCpf(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            return "***";
        }

        var digitos = new string([.. cpf.Where(char.IsDigit)]);
        if (digitos.Length != 11)
        {
            return "***";
        }

        return string.Format(CultureInfo.InvariantCulture, "***.***.{0}-**", digitos.Substring(6, 3));
    }

    private static string MaskCnpj(string? cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
        {
            return "***";
        }

        var digitos = new string([.. cnpj.Where(char.IsDigit)]);
        if (digitos.Length != 14)
        {
            return "***";
        }

        return string.Format(CultureInfo.InvariantCulture, "**.***.***/****-{0}", digitos.Substring(12, 2));
    }
}
