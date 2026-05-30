using System.Text.RegularExpressions;
using CarWash.Domain.Common;
using CarWash.Domain.ValueObjects;

namespace CarWash.Domain.Entities;

/// <summary>
/// Unidade operacional (RF017/RF018). Capacidade controlada por
/// <c>celulas_ativas BETWEEN 1 AND 100</c> (RN009 / CHECK <c>ck_filiais_celulas_faixa</c>).
/// Identificação operacional via <c>Codigo</c> (regex <c>^[A-Z0-9]{2,20}$</c>),
/// CNPJ e endereço estruturado opcionais durante o rollout aditivo do RF017.
/// </summary>
public sealed class Filial : IAuditable, IAuditableSetter
{
    public const int MinCelulasAtivas = 1;
    public const int MaxCelulasAtivas = 100;
    public const int NomeMinChars = 3;
    public const int NomeMaxChars = 120;
    public const int CodigoMinChars = 2;
    public const int CodigoMaxChars = 20;

    private static readonly Regex CodigoRegex = new(
        "^[A-Z0-9]{2,20}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(50));

    private Filial()
    {
        Nome = null!;
        Codigo = null!;
        Timezone = null!;
    }

    public Guid Id { get; private set; }

    public string Nome { get; private set; }

    /// <summary>
    /// Identificador operacional curto da filial (RF017). Único case-sensitive
    /// no banco (UK parcial <c>uk_filiais_codigo</c> + CHECK
    /// <c>ck_filiais_codigo_formato</c>). Obrigatório no domínio mesmo que a
    /// coluna esteja nullable durante o rollout aditivo (ADR 0007 §3.4).
    /// </summary>
    public string Codigo { get; private set; }

    /// <summary>
    /// CNPJ opcional (L2 do ADR 0007). Quando presente, único (UK parcial
    /// <c>uk_filiais_cnpj</c>). Armazenado em 14 dígitos sem máscara.
    /// </summary>
    public string? Cnpj { get; private set; }

    public bool Ativa { get; private set; }

    public int CelulasAtivas { get; private set; }

    public string Timezone { get; private set; }

    public string? EnderecoCep { get; private set; }

    public string? EnderecoLogradouro { get; private set; }

    public string? EnderecoNumero { get; private set; }

    public string? EnderecoComplemento { get; private set; }

    public string? EnderecoBairro { get; private set; }

    public string? EnderecoCidade { get; private set; }

    public string? EnderecoUf { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public DateTime AtualizadoEm { get; private set; }

    /// <summary>
    /// Id do usuário autenticado que criou a filial (auditoria — espelha
    /// <c>Cliente.CriadoPorUsuarioId</c>). Nullable para tolerar registros
    /// legados anteriores à migration <c>AdicionaCadastroFilial</c>.
    /// </summary>
    public Guid? CriadoPorUsuarioId { get; private set; }

    /// <summary>
    /// Getter computado equivalente ao VO <see cref="Endereco"/>. Retorna
    /// <c>null</c> quando o endereço estruturado não foi informado (rollout
    /// aditivo). Não persistido — <c>FilialConfiguration</c> aplica
    /// <c>Ignore(x =&gt; x.Endereco)</c>.
    /// </summary>
    public Endereco? Endereco
    {
        get
        {
            // Durante o rollout aditivo, as colunas de endereço podem existir de forma parcial.
            // Só materializa o VO quando todos os campos obrigatórios estiverem presentes.
            if (string.IsNullOrWhiteSpace(EnderecoCep)
                || string.IsNullOrWhiteSpace(EnderecoLogradouro)
                || string.IsNullOrWhiteSpace(EnderecoNumero)
                || string.IsNullOrWhiteSpace(EnderecoBairro)
                || string.IsNullOrWhiteSpace(EnderecoCidade)
                || string.IsNullOrWhiteSpace(EnderecoUf))
            {
                return null;
            }

            return new Endereco(
                EnderecoCep,
                EnderecoLogradouro,
                EnderecoNumero,
                EnderecoComplemento,
                EnderecoBairro,
                EnderecoCidade,
                EnderecoUf);
        }
    }

    /// <summary>
    /// Fábrica do agregado <see cref="Filial"/> (RF017 + RF018). Aplica as
    /// invariantes do domínio antes da persistência. <paramref name="codigo"/>
    /// é obrigatório (regra de negócio crítica, ADR 0007 §2.4) — mesmo que a
    /// coluna esteja nullable em produção durante o rollout aditivo.
    /// </summary>
    public static Filial Criar(
        Guid id,
        string nome,
        string codigo,
        int celulasAtivas,
        Endereco? endereco = null,
        Cnpj? cnpj = null,
        string? timezone = null)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Id da filial não pode ser vazio.");
        }

        if (string.IsNullOrWhiteSpace(nome)
            || nome.Trim().Length < NomeMinChars
            || nome.Trim().Length > NomeMaxChars)
        {
            throw new DomainException(
                $"Nome da filial deve ter entre {NomeMinChars} e {NomeMaxChars} caracteres.");
        }

        if (string.IsNullOrWhiteSpace(codigo) || !CodigoRegex.IsMatch(codigo))
        {
            throw new DomainException(
                "Código da filial é obrigatório e deve conter de 2 a 20 caracteres alfanuméricos maiúsculos (A-Z, 0-9).");
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
            Nome = nome.Trim(),
            Codigo = codigo,
            Cnpj = cnpj?.Valor,
            CelulasAtivas = celulasAtivas,
            Timezone = string.IsNullOrWhiteSpace(timezone) ? "America/Sao_Paulo" : timezone,
            Ativa = true,
            EnderecoCep = endereco?.Cep,
            EnderecoLogradouro = endereco?.Logradouro,
            EnderecoNumero = endereco?.Numero,
            EnderecoComplemento = endereco?.Complemento,
            EnderecoBairro = endereco?.Bairro,
            EnderecoCidade = endereco?.Cidade,
            EnderecoUf = endereco?.Uf,
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

    /// <summary>
    /// Registra o usuário responsável pela criação (auditoria — espelha
    /// <c>Cliente.RegistrarCriadoPor</c>). Chamado pelo handler ao persistir
    /// uma filial nova.
    /// </summary>
    public void RegistrarCriadoPor(Guid? usuarioId)
    {
        CriadoPorUsuarioId = usuarioId;
    }

    void IAuditableSetter.SetCriadoEm(DateTime valor) => CriadoEm = valor;

    void IAuditableSetter.SetAtualizadoEm(DateTime valor) => AtualizadoEm = valor;
}
