using CarWash.Domain.Entities;

namespace CarWash.Application.Filiais.Common;

/// <summary>
/// Representação compacta da filial usada no GET /api/v1/filiais. Compatível
/// com <c>frontend/src/types/filial.ts</c> (campos extras como <c>codigo</c>
/// são ignorados pelo TS atual sem breaking change).
/// </summary>
public class FilialResumoResponse
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Codigo { get; set; }

    public string? Cidade { get; set; }

    public string? Uf { get; set; }

    public bool Ativo { get; set; }

    public static FilialResumoResponse FromEntity(Filial filial)
    {
        ArgumentNullException.ThrowIfNull(filial);
        return new FilialResumoResponse
        {
            Id = filial.Id,
            Nome = filial.Nome,
            Codigo = filial.Codigo,
            Cidade = filial.EnderecoCidade,
            Uf = filial.EnderecoUf,
            Ativo = filial.Ativa,
        };
    }
}
