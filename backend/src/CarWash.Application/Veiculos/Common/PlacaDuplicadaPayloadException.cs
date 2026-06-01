using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Veiculos.Common;

/// <summary>
/// RF005 — placa duplicada dentro do mesmo payload. Rejeitada antes de qualquer
/// insert. Mapeada para 400 BadRequest + ProblemDetails pelo middleware global.
/// </summary>
#pragma warning disable RCS1194
public sealed class PlacaDuplicadaPayloadException : ValidationException
#pragma warning restore RCS1194
{
    public const string MensagemPadrao = "O payload contém placas duplicadas.";

    public PlacaDuplicadaPayloadException()
        : base(MensagemPadrao, new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["placa"] = [MensagemPadrao],
        })
    {
    }
}
