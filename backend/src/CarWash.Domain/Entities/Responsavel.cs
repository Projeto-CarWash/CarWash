using CarWash.Domain.Common;
using CarWash.Domain.Enums;
using CarWash.Domain.ValueObjects;

namespace CarWash.Domain.Entities;

public sealed class Responsavel : IAuditable, IAuditableSetter
{
    private Responsavel()
    {
        Nome = null!;
        Documento = null!;
        GrauVinculo = null!;
    }

    public Guid Id { get; private set; }

    public Guid ClienteTitularId { get; private set; }

    public string Nome { get; private set; }

    public string Documento { get; private set; }

    public string? Telefone { get; private set; }

    public string? Email { get; private set; }

    public string GrauVinculo { get; private set; }

    public bool Ativo { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public DateTime AtualizadoEm { get; private set; }

    public static Responsavel Criar(
        Guid id,
        Guid clienteTitularId,
        string nome,
        string documento,
        GrauVinculo grauVinculo,
        Telefone? telefone = null,
        Email? email = null)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Id do responsável não pode ser vazio.");
        }

        if (clienteTitularId == Guid.Empty)
        {
            throw new DomainException("Responsável deve estar vinculado a um cliente titular.");
        }

        if (string.IsNullOrWhiteSpace(nome) || nome.Length < 3 || nome.Length > 100)
        {
            throw new DomainException("Nome do responsável deve ter entre 3 e 100 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(documento))
        {
            throw new DomainException("Documento do responsável é obrigatório.");
        }

        var agora = DateTime.UtcNow;
        return new Responsavel
        {
            Id = id,
            ClienteTitularId = clienteTitularId,
            Nome = nome,
            Documento = documento,
            Telefone = telefone?.Valor,
            Email = email?.Valor,
            GrauVinculo = grauVinculo.ToDbValue(),
            Ativo = true,
            CriadoEm = agora,
            AtualizadoEm = agora,
        };
    }

    public void AtualizarDados(string nome, string? telefone, string? email, GrauVinculo grauVinculo)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Length < 3 || nome.Length > 100)
        {
            throw new DomainException("Nome do responsável deve ter entre 3 e 100 caracteres.");
        }

        Nome = nome;
        Telefone = telefone;
        Email = email;
        GrauVinculo = grauVinculo.ToDbValue();
        AtualizadoEm = DateTime.UtcNow;
    }

    public void Inativar()
    {
        Ativo = false;
        AtualizadoEm = DateTime.UtcNow;
    }

    public void Ativar()
    {
        Ativo = true;
        AtualizadoEm = DateTime.UtcNow;
    }

    void IAuditableSetter.SetCriadoEm(DateTime valor) => CriadoEm = valor;

    void IAuditableSetter.SetAtualizadoEm(DateTime valor) => AtualizadoEm = valor;
}
