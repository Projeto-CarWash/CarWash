using CarWash.Domain.Common;
using CarWash.Domain.ValueObjects;

namespace CarWash.Domain.Entities;

/// <summary>
/// Pessoa autorizada pelo titular ("Responsável" no DRP). Tabela física <c>filiados</c>
/// (decisão P08). Exige CPF ou RG (CHECK <c>ck_filiados_cpf_ou_rg</c>).
/// </summary>
public sealed class Filiado : IAuditable, IAuditableSetter
{
    private Filiado()
    {
        Nome = null!;
        Telefone = null!;
    }

    public Guid Id { get; private set; }

    public Guid ClienteId { get; private set; }

    public string Nome { get; private set; }

    public string Telefone { get; private set; }

    public string? Cpf { get; private set; }

    public string? Rg { get; private set; }

    public bool Ativo { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public DateTime AtualizadoEm { get; private set; }

    public static Filiado Criar(
        Guid id,
        Guid clienteId,
        string nome,
        Telefone telefone,
        Cpf? cpf = null,
        string? rg = null)
    {
        ArgumentNullException.ThrowIfNull(telefone);

        if (id == Guid.Empty)
        {
            throw new DomainException("Id do filiado não pode ser vazio.");
        }

        if (clienteId == Guid.Empty)
        {
            throw new DomainException("Filiado deve estar vinculado a um cliente.");
        }

        if (string.IsNullOrWhiteSpace(nome) || nome.Length > 120)
        {
            throw new DomainException("Nome do filiado é obrigatório e deve ter no máximo 120 caracteres.");
        }

        if (cpf is null && string.IsNullOrWhiteSpace(rg))
        {
            throw new DomainException("Filiado deve possuir CPF ou RG informado.");
        }

        var agora = DateTime.UtcNow;
        return new Filiado
        {
            Id = id,
            ClienteId = clienteId,
            Nome = nome,
            Telefone = telefone.Valor,
            Cpf = cpf?.Valor,
            Rg = rg,
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
