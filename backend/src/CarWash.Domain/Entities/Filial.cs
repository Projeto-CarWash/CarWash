using CarWash.Domain.Common;

namespace CarWash.Domain.Entities;

/// <summary>
/// Unidade operacional. Capacidade controlada por <c>celulas_ativas BETWEEN 1 AND 100</c>
/// (RN009 / CHECK <c>ck_filiais_celulas_faixa</c>).
/// </summary>
public sealed class Filial : IAuditable, IAuditableSetter
{
    public const int MinCelulasAtivas = 1;
    public const int MaxCelulasAtivas = 100;

    private Filial()
    {
        Nome = null!;
        Timezone = null!;
    }

    public Guid Id { get; private set; }

    public string Nome { get; private set; }

    public bool Ativa { get; private set; }

    public int CelulasAtivas { get; private set; }

    public string Timezone { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public DateTime AtualizadoEm { get; private set; }

    public static Filial Criar(Guid id, string nome, int celulasAtivas, string? timezone = null)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Id da filial não pode ser vazio.");
        }

        if (string.IsNullOrWhiteSpace(nome) || nome.Length > 120)
        {
            throw new DomainException("Nome da filial é obrigatório e deve ter no máximo 120 caracteres.");
        }

        if (celulasAtivas is < MinCelulasAtivas or > MaxCelulasAtivas)
        {
            throw new DomainException(
                $"Células ativas deve estar entre {MinCelulasAtivas} e {MaxCelulasAtivas} (RN009).");
        }

        var agora = DateTime.UtcNow;
        return new Filial
        {
            Id = id,
            Nome = nome,
            CelulasAtivas = celulasAtivas,
            Timezone = string.IsNullOrWhiteSpace(timezone) ? "America/Sao_Paulo" : timezone,
            Ativa = true,
            CriadoEm = agora,
            AtualizadoEm = agora,
        };
    }

    public void AjustarCelulas(int novoValor)
    {
        if (novoValor is < MinCelulasAtivas or > MaxCelulasAtivas)
        {
            throw new DomainException(
                $"Células ativas deve estar entre {MinCelulasAtivas} e {MaxCelulasAtivas} (RN009).");
        }

        CelulasAtivas = novoValor;
    }

    public void Inativar() => Ativa = false;

    public void Ativar() => Ativa = true;

    void IAuditableSetter.SetCriadoEm(DateTime valor) => CriadoEm = valor;

    void IAuditableSetter.SetAtualizadoEm(DateTime valor) => AtualizadoEm = valor;
}
