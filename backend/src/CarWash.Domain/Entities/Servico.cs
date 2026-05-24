using CarWash.Domain.Common;

namespace CarWash.Domain.Entities;

/// <summary>
/// Catálogo de serviços oferecidos. CHECK <c>preco &gt; 0</c> e <c>duracao_min &gt; 0</c>.
/// </summary>
public sealed class Servico : IAuditable, IAuditableSetter
{
    private Servico()
    {
        Nome = null!;
    }

    public Guid Id { get; private set; }

    public string Nome { get; private set; }

    public decimal Preco { get; private set; }

    public int DuracaoMin { get; private set; }

    public bool Ativo { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public DateTime AtualizadoEm { get; private set; }

    public static Servico Criar(Guid id, string nome, decimal preco, int duracaoMin)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Id do serviço não pode ser vazio.");
        }

        if (string.IsNullOrWhiteSpace(nome) || nome.Length > 120)
        {
            throw new DomainException("Nome do serviço é obrigatório e deve ter no máximo 120 caracteres.");
        }

        if (preco <= 0m)
        {
            throw new DomainException("Preço do serviço deve ser positivo.");
        }

        if (duracaoMin <= 0)
        {
            throw new DomainException("Duração do serviço deve ser positiva.");
        }

        var agora = DateTime.UtcNow;
        return new Servico
        {
            Id = id,
            Nome = nome,
            Preco = preco,
            DuracaoMin = duracaoMin,
            Ativo = true,
            CriadoEm = agora,
            AtualizadoEm = agora,
        };
    }

    public void Inativar() => Ativo = false;

    public void Ativar() => Ativo = true;

    void IAuditableSetter.SetCriadoEm(DateTime valor) => CriadoEm = valor;

    void IAuditableSetter.SetAtualizadoEm(DateTime valor) => AtualizadoEm = valor;
}
