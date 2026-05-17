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

    /// <summary>
    /// Tentativas de login inválidas consecutivas desde o último sucesso (RF001 — lockout).
    /// Reseta para zero em qualquer login bem-sucedido.
    /// </summary>
    public int TentativasInvalidas { get; private set; }

    /// <summary>
    /// Instante (UTC) até quando o usuário está temporariamente bloqueado por exceder o
    /// limite de tentativas inválidas. <c>null</c> quando não há bloqueio ativo.
    /// </summary>
    public DateTime? BloqueadoAte { get; private set; }

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
            TentativasInvalidas = 0,
            CriadoEm = agora,
            AtualizadoEm = agora,
        };
    }

    public void Inativar() => Ativo = false;

    public void Ativar() => Ativo = true;

    /// <summary>
    /// Atualiza dados cadastrais — nome, e-mail e perfil. Não altera senha,
    /// status nem contadores de lockout (usar métodos dedicados para isso).
    /// </summary>
    public void AlterarDados(string nome, Email email, Enums.PerfilUsuario perfil)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Length > 100)
        {
            throw new DomainException("Nome é obrigatório e deve ter no máximo 100 caracteres.");
        }

        ArgumentNullException.ThrowIfNull(email);

        Nome = nome;
        EmailValor = email.Valor;
        PerfilRaw = perfil.ToDbValue();
    }

    public void TrocarSenha(string novoHash)
    {
        if (string.IsNullOrWhiteSpace(novoHash))
        {
            throw new DomainException("Hash de senha não pode ser vazio.");
        }

        SenhaHash = novoHash;
    }

    /// <summary>
    /// Verdadeiro quando há um <see cref="BloqueadoAte"/> futuro em relação a
    /// <paramref name="agoraUtc"/>. Tempo passado como parâmetro para permitir testes
    /// determinísticos.
    /// </summary>
    public bool EstaBloqueado(DateTime agoraUtc) =>
        BloqueadoAte.HasValue && BloqueadoAte.Value > agoraUtc;

    /// <summary>
    /// Incrementa o contador de falhas. Se atingir <paramref name="limiteTentativas"/>,
    /// aplica bloqueio temporário de <paramref name="duracaoBloqueio"/>. O caller decide
    /// quando persistir (uma única <c>SalvarAsync</c> ao final do fluxo).
    /// </summary>
    public void RegistrarFalhaDeLogin(
        DateTime agoraUtc,
        int limiteTentativas,
        TimeSpan duracaoBloqueio)
    {
        if (limiteTentativas <= 0)
        {
            throw new DomainException("Limite de tentativas inválidas deve ser positivo.");
        }

        if (duracaoBloqueio <= TimeSpan.Zero)
        {
            throw new DomainException("Duração do bloqueio deve ser positiva.");
        }

        TentativasInvalidas++;
        if (TentativasInvalidas >= limiteTentativas)
        {
            BloqueadoAte = agoraUtc.Add(duracaoBloqueio);
        }
    }

    /// <summary>Zera o contador e libera bloqueio. Chamar somente em login bem-sucedido.</summary>
    public void RegistrarLoginBemSucedido()
    {
        TentativasInvalidas = 0;
        BloqueadoAte = null;
    }

    void IAuditableSetter.SetCriadoEm(DateTime valor) => CriadoEm = valor;

    void IAuditableSetter.SetAtualizadoEm(DateTime valor) => AtualizadoEm = valor;
}
