using CarWash.Domain.Common;

namespace CarWash.Domain.Entities;

/// <summary>
/// Feature flag por ambiente e (opcionalmente) filial. Schema criado no MVP,
/// sem consumidor.
/// </summary>
public sealed class FeatureFlag
{
    private FeatureFlag()
    {
        Nome = null!;
        Ambiente = null!;
    }

    public Guid Id { get; private set; }

    public string Nome { get; private set; }

    public string Ambiente { get; private set; }

    public Guid? FilialId { get; private set; }

    public bool Habilitada { get; private set; }

    public string? ValorJson { get; private set; }

    public Guid AtualizadoPor { get; private set; }

    public DateTime AtualizadoEm { get; private set; }

    public static FeatureFlag Criar(
        Guid id,
        string nome,
        string ambiente,
        Guid atualizadoPor,
        Guid? filialId = null,
        bool habilitada = false,
        string? valorJson = null)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Id da flag não pode ser vazio.");
        }

        if (string.IsNullOrWhiteSpace(nome) || nome.Length > 100)
        {
            throw new DomainException("Nome da flag é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(ambiente) || ambiente.Length > 30)
        {
            throw new DomainException("Ambiente da flag é obrigatório.");
        }

        if (atualizadoPor == Guid.Empty)
        {
            throw new DomainException("Usuário responsável pela flag é obrigatório.");
        }

        return new FeatureFlag
        {
            Id = id,
            Nome = nome,
            Ambiente = ambiente,
            FilialId = filialId,
            Habilitada = habilitada,
            ValorJson = valorJson,
            AtualizadoPor = atualizadoPor,
            AtualizadoEm = DateTime.UtcNow,
        };
    }
}
