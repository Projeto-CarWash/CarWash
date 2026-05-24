using CarWash.Domain.Entities;

namespace CarWash.Application.Servicos.Common;

/// <summary>
/// DTO de saída de serviço (GET / PUT / PATCH de status).
/// </summary>
public class ServicoResponse
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public decimal Preco { get; set; }

    public int DuracaoMin { get; set; }

    public bool Ativo { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime AtualizadoEm { get; set; }

    public static ServicoResponse FromEntity(Servico servico)
    {
        ArgumentNullException.ThrowIfNull(servico);
        return new ServicoResponse
        {
            Id = servico.Id,
            Nome = servico.Nome,
            Preco = servico.Preco,
            DuracaoMin = servico.DuracaoMin,
            Ativo = servico.Ativo,
            CriadoEm = servico.CriadoEm,
            AtualizadoEm = servico.AtualizadoEm,
        };
    }
}
