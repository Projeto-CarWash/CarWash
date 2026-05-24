using CarWash.Domain.Entities;

namespace CarWash.Application.Servicos.Common;

/// <summary>
/// Representação compacta usada na listagem paginada.
/// </summary>
public class ServicoResumoResponse
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public decimal Preco { get; set; }

    public int DuracaoMin { get; set; }

    public bool Ativo { get; set; }

    public DateTime CriadoEm { get; set; }

    public static ServicoResumoResponse FromEntity(Servico servico)
    {
        ArgumentNullException.ThrowIfNull(servico);
        return new ServicoResumoResponse
        {
            Id = servico.Id,
            Nome = servico.Nome,
            Preco = servico.Preco,
            DuracaoMin = servico.DuracaoMin,
            Ativo = servico.Ativo,
            CriadoEm = servico.CriadoEm,
        };
    }
}
