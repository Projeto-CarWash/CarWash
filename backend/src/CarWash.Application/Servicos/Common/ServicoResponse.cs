using CarWash.Domain.Entities;

namespace CarWash.Application.Servicos.Common;

public sealed class ServicoResponse
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = null!;
    public decimal Preco { get; set; }
    public decimal PrecoBase { get; set; }
    public int DuracaoMin { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }

    public static ServicoResponse FromEntity(Servico entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new ServicoResponse
        {
            Id = entity.Id,
            Nome = entity.Nome,
            Preco = entity.Preco,
            PrecoBase = entity.Preco, // Support both fields
            DuracaoMin = entity.DuracaoMin,
            Ativo = entity.Ativo,
            CriadoEm = entity.CriadoEm,
            AtualizadoEm = entity.AtualizadoEm
        };
    }
}
