using CarWash.Domain.Common;
using CarWash.Domain.Enums;
using CarWash.Domain.ValueObjects;

namespace CarWash.Domain.Entities;

/// <summary>
/// Usuário interno que acessa o sistema (DB001 §01.1). Senha sempre persistida como hash Argon2id
/// (ADR 0002). A normalização do email é responsabilidade do value object <see cref="Email"/>.
/// </summary>
public sealed class Usuario : IAuditable, IAuditableSetter
{
    private Usuario()
    {
        // EF Core reidratação.
        Nome = null!;
        EmailValor = null!;
        SenhaHash = null!;
        PerfilRaw = null!;
    }

    public Guid Id { get; private set; }

    public string Nome { get; private set; }

    // Persistido como string lowercase (uk_usuarios_email).
    public string EmailValor { get; private set; }

    public Email Email => new(EmailValor);

    public string SenhaHash { get; private set; }

    // Persistido como 'ADMIN' / 'FUNCIONARIO' (ck_usuarios_perfil).
    public string PerfilRaw { get; private set; }

    public PerfilUsuario Perfil => PerfilUsuarioExtensions.FromDbValue(PerfilRaw);

    public bool Ativo { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public DateTime AtualizadoEm { get; private set; }

    public static Usuario Criar(
        Guid id,
        string nome,
        Email email,
        string senhaHash,
        PerfilUsuario perfil)
    {
        ArgumentNullException.ThrowIfNull(email);

        if (id == Guid.Empty)
        {
            throw new DomainException("Id do usuário não pode ser vazio.");
        }

        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DomainException("Nome do usuário é obrigatório.");
        }

        if (nome.Length > 120)
        {
            throw new DomainException("Nome do usuário excede 120 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(senhaHash))
        {
            throw new DomainException("Hash de senha não pode ser vazio.");
        }

        var agora = DateTime.UtcNow;

        return new Usuario
        {
            Id = id,
            Nome = nome,
            EmailValor = email.Valor,
            SenhaHash = senhaHash,
            PerfilRaw = perfil.ToDbValue(),
            Ativo = true,
            CriadoEm = agora,
            AtualizadoEm = agora,
        };
    }

    public void Inativar() => Ativo = false;

    public void Ativar() => Ativo = true;

    public void TrocarSenha(string novoHash)
    {
        if (string.IsNullOrWhiteSpace(novoHash))
        {
            throw new DomainException("Hash de senha não pode ser vazio.");
        }

        SenhaHash = novoHash;
    }

    void IAuditableSetter.SetCriadoEm(DateTime valor) => CriadoEm = valor;

    void IAuditableSetter.SetAtualizadoEm(DateTime valor) => AtualizadoEm = valor;
}
