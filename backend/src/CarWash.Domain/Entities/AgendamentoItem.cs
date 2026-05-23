using CarWash.Domain.Common;

namespace CarWash.Domain.Entities;

/// <summary>
/// Serviço prestado dentro de um agendamento. UNIQUE <c>(agendamento_id, servico_id)</c>
/// impede duplicidade (CA007).
/// </summary>
public sealed class AgendamentoItem
{
    private AgendamentoItem()
    {
    }

    public Guid Id { get; private set; }

    public Guid AgendamentoId { get; private set; }

    public Guid ServicoId { get; private set; }

    public decimal PrecoAplicado { get; private set; }

    public int DuracaoAplicada { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public static AgendamentoItem Criar(
        Guid id,
        Guid agendamentoId,
        Guid servicoId,
        decimal precoAplicado,
        int duracaoAplicada)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Id do item não pode ser vazio.");
        }

        if (agendamentoId == Guid.Empty)
        {
            throw new DomainException("Agendamento do item não pode ser vazio.");
        }

        if (servicoId == Guid.Empty)
        {
            throw new DomainException("Serviço do item não pode ser vazio.");
        }

        if (precoAplicado < 0m)
        {
            throw new DomainException("Preço aplicado não pode ser negativo.");
        }

        if (duracaoAplicada <= 0)
        {
            throw new DomainException("Duração aplicada deve ser positiva.");
        }

        return new AgendamentoItem
        {
            Id = id,
            AgendamentoId = agendamentoId,
            ServicoId = servicoId,
            PrecoAplicado = precoAplicado,
            DuracaoAplicada = duracaoAplicada,
            CriadoEm = DateTime.UtcNow,
        };
    }
}
