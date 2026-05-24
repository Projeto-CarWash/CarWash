using CarWash.Domain.Common;
using CarWash.Domain.ValueObjects;

namespace CarWash.Domain.Entities;

/// <summary>
/// Cliente titular (PF/PJ). Exige <c>cpf</c> ou <c>cnpj</c> (CHECK <c>ck_clientes_cpf_ou_cnpj</c>),
/// celular obrigatório (RF003 — alinhamento com a tela do Lucas após PR #15) e
/// endereço estruturado (CEP + logradouro + número + bairro + cidade + UF).
/// </summary>
public sealed class Cliente : IAuditable, IAuditableSetter
{
    public const int IdadeMinima = 18;
    public const int IdadeMaxima = 110;

    private Cliente()
    {
        Nome = null!;
        Celular = null!;
        EnderecoCep = null!;
        EnderecoLogradouro = null!;
        EnderecoNumero = null!;
        EnderecoBairro = null!;
        EnderecoCidade = null!;
        EnderecoUf = null!;
    }

    public Guid Id { get; private set; }

    public string Nome { get; private set; }

    public DateOnly DataNascimento { get; private set; }

    public string? Cpf { get; private set; }

    public string? Cnpj { get; private set; }

    public string? Telefone { get; private set; }

    public string Celular { get; private set; }

    public string? Email { get; private set; }

    public string EnderecoCep { get; private set; }

    public string EnderecoLogradouro { get; private set; }

    public string EnderecoNumero { get; private set; }

    public string? EnderecoComplemento { get; private set; }

    public string EnderecoBairro { get; private set; }

    public string EnderecoCidade { get; private set; }

    public string EnderecoUf { get; private set; }

    public bool Ativo { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public DateTime AtualizadoEm { get; private set; }

    /// <summary>
    /// Id do usuário autenticado que criou o cliente.
    /// Pode ser <c>null</c> em registros legados (anteriores à migration
    /// <c>AdicionaAuditoriaUsuarioCliente</c>); para releases futuras deve virar NOT NULL.
    /// </summary>
    public Guid? CriadoPorUsuarioId { get; private set; }

    /// <summary>
    /// Id do usuário autenticado que efetuou a última alteração do cliente
    /// (atualização de dados ou mudança de status).
    /// </summary>
    public Guid? AtualizadoPorUsuarioId { get; private set; }

    public Endereco Endereco => new(
        EnderecoCep,
        EnderecoLogradouro,
        EnderecoNumero,
        EnderecoComplemento,
        EnderecoBairro,
        EnderecoCidade,
        EnderecoUf);

    public static Cliente Criar(
        Guid id,
        string nome,
        DateOnly dataNascimento,
        Telefone celular,
        Endereco endereco,
        Cpf? cpf = null,
        Cnpj? cnpj = null,
        Telefone? telefone = null,
        Email? email = null)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Id do cliente não pode ser vazio.");
        }

        if (string.IsNullOrWhiteSpace(nome) || nome.Length < 3 || nome.Length > 100)
        {
            throw new DomainException("Nome do cliente deve ter entre 3 e 100 caracteres.");
        }

        if (cpf is null && cnpj is null)
        {
            throw new DomainException("Cliente deve ter CPF ou CNPJ informado.");
        }

        if (cpf is not null && cnpj is not null)
        {
            throw new DomainException("Informe apenas CPF ou CNPJ, não ambos.");
        }

        ArgumentNullException.ThrowIfNull(celular);
        ArgumentNullException.ThrowIfNull(endereco);

        ValidarIdade(dataNascimento);

        var agora = DateTime.UtcNow;
        return new Cliente
        {
            Id = id,
            Nome = nome,
            DataNascimento = dataNascimento,
            Cpf = cpf?.Valor,
            Cnpj = cnpj?.Valor,
            Telefone = telefone?.Valor,
            Celular = celular.Valor,
            Email = email?.Valor,
            EnderecoCep = endereco.Cep,
            EnderecoLogradouro = endereco.Logradouro,
            EnderecoNumero = endereco.Numero,
            EnderecoComplemento = endereco.Complemento,
            EnderecoBairro = endereco.Bairro,
            EnderecoCidade = endereco.Cidade,
            EnderecoUf = endereco.Uf,
            Ativo = true,
            CriadoEm = agora,
            AtualizadoEm = agora,
        };
    }

    public void AtualizarDados(
        string nome,
        DateOnly dataNascimento,
        Telefone celular,
        Endereco endereco,
        Telefone? telefone = null,
        Email? email = null)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Length < 3 || nome.Length > 100)
        {
            throw new DomainException("Nome do cliente deve ter entre 3 e 100 caracteres.");
        }

        ArgumentNullException.ThrowIfNull(celular);
        ArgumentNullException.ThrowIfNull(endereco);

        ValidarIdade(dataNascimento);

        Nome = nome;
        DataNascimento = dataNascimento;
        Telefone = telefone?.Valor;
        Celular = celular.Valor;
        Email = email?.Valor;
        EnderecoCep = endereco.Cep;
        EnderecoLogradouro = endereco.Logradouro;
        EnderecoNumero = endereco.Numero;
        EnderecoComplemento = endereco.Complemento;
        EnderecoBairro = endereco.Bairro;
        EnderecoCidade = endereco.Cidade;
        EnderecoUf = endereco.Uf;
    }

    public void Inativar() => Ativo = false;

    public void Ativar() => Ativo = true;

    /// <summary>
    /// Registra o usuário responsável pela criação (auditoria).
    /// Chamado pelo Service ao persistir um cliente novo.
    /// </summary>
    public void RegistrarCriadoPor(Guid? usuarioId)
    {
        CriadoPorUsuarioId = usuarioId;
        AtualizadoPorUsuarioId = usuarioId;
    }

    /// <summary>
    /// Registra o usuário responsável pela última alteração (auditoria).
    /// Chamado pelo Service em atualizações de dados ou de status.
    /// </summary>
    public void RegistrarAtualizadoPor(Guid? usuarioId)
    {
        AtualizadoPorUsuarioId = usuarioId;
    }

    void IAuditableSetter.SetCriadoEm(DateTime valor) => CriadoEm = valor;

    void IAuditableSetter.SetAtualizadoEm(DateTime valor) => AtualizadoEm = valor;

    private static void ValidarIdade(DateOnly dataNascimento)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        if (dataNascimento > hoje)
        {
            throw new DomainException("Data de nascimento não pode ser futura.");
        }

        var idade = hoje.Year - dataNascimento.Year;
        if (dataNascimento > hoje.AddYears(-idade))
        {
            idade--;
        }

        if (idade < IdadeMinima)
        {
            throw new DomainException($"Cliente deve ter pelo menos {IdadeMinima} anos.");
        }

        if (idade > IdadeMaxima)
        {
            throw new DomainException($"Cliente deve ter no máximo {IdadeMaxima} anos.");
        }
    }
}
