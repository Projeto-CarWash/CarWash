using CarWash.Domain.Common;
using CarWash.Domain.ValueObjects;

namespace CarWash.Domain.Entities;

/// <summary>
/// Cliente titular (PF/PJ). Exige <c>cpf</c> ou <c>cnpj</c> (CHECK <c>ck_clientes_cpf_ou_cnpj</c>)
/// e respeita uniques parciais quando preenchidos.
/// </summary>
public sealed class Cliente : IAuditable, IAuditableSetter
{
    private Cliente()
    {
        Nome = null!;
    }

    public Guid Id { get; private set; }

    public string Nome { get; private set; }

    public string? Cpf { get; private set; }

    public string? Cnpj { get; private set; }

    public string? Telefone { get; private set; }

    public string? Celular { get; private set; }

    public string? Email { get; private set; }

    public string? Endereco { get; private set; }

    public string? Observacoes { get; private set; }

    public bool Ativo { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public DateTime AtualizadoEm { get; private set; }

    public static Cliente Criar(
        Guid id,
        string nome,
        Cpf? cpf = null,
        Cnpj? cnpj = null,
        Telefone? telefone = null,
        Telefone? celular = null,
        Email? email = null,
        string? endereco = null,
        string? observacoes = null)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Id do cliente não pode ser vazio.");
        }

        if (string.IsNullOrWhiteSpace(nome) || nome.Length > 100)
        {
            throw new DomainException("Nome do cliente é obrigatório e deve ter no máximo 100 caracteres.");
        }

        if (cpf is null && cnpj is null)
        {
            throw new DomainException("Cliente deve ter CPF ou CNPJ informado.");
        }

        var agora = DateTime.UtcNow;
        return new Cliente
        {
            Id = id,
            Nome = nome,
            Cpf = cpf?.Valor,
            Cnpj = cnpj?.Valor,
            Telefone = telefone?.Valor,
            Celular = celular?.Valor,
            Email = email?.Valor,
            Endereco = endereco,
            Observacoes = observacoes,
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
