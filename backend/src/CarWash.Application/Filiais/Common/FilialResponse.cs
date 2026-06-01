using CarWash.Domain.Entities;

namespace CarWash.Application.Filiais.Common;

/// <summary>
/// DTO de saída da Filial (RF017/RF018). Reaproveitado por POST (201),
/// PATCH /celulas-ativas (200) e GET /{id} (200).
/// </summary>
public sealed record FilialResponse(
    Guid Id,
    string Nome,
    int CelulasAtivas,
    string Timezone,
    bool Ativa,
    DateTime CriadoEm,
    DateTime AtualizadoEm)
{
    public static FilialResponse FromEntity(Filial filial)
    {
        ArgumentNullException.ThrowIfNull(filial);
        return new FilialResponse(
            filial.Id,
            filial.Nome,
            filial.CelulasAtivas,
            filial.Timezone,
            filial.Ativa,
            filial.CriadoEm,
            filial.AtualizadoEm);
    }
}
