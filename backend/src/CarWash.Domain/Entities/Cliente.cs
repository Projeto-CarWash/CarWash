namespace CarWash.Domain.Entities;

public class Cliente
{
    public Guid Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public string? Cpf { get; private set; }

    public string? Cnpj { get; private set; }

    public string? Telefone { get; private set; }

    public string? Celular { get; private set; }

    public string? Email { get; private set; }

    public string? Endereco { get; private set; }

    public string? Observacoes { get; private set; }

    public bool Ativo { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }

    public DateTimeOffset AtualizadoEm { get; private set; }

    public Cliente(
        string nome,
        string? cpf,
        string? cnpj,
        string? telefone,
        string? celular,
        string? email,
        string? endereco,
        string? observacoes)
    {
        Id = Guid.NewGuid();
        Nome = nome;
        Cpf = cpf;
        Cnpj = cnpj;
        Telefone = telefone;
        Celular = celular;
        Email = email;
        Endereco = endereco;
        Observacoes = observacoes;
        Ativo = true;
        CriadoEm = DateTimeOffset.UtcNow;
        AtualizadoEm = DateTimeOffset.UtcNow;
    }

    protected Cliente()
    {
    }

    public void Atualizar(
        string nome,
        string? telefone,
        string? celular,
        string? email,
        string? endereco,
        string? observacoes)
    {
        Nome = nome;
        Telefone = telefone;
        Celular = celular;
        Email = email;
        Endereco = endereco;
        Observacoes = observacoes;
        AtualizadoEm = DateTimeOffset.UtcNow;
    }

    private readonly List<Veiculo> veiculos = [];

    public IReadOnlyCollection<Veiculo> Veiculos => veiculos;

    public void AdicionarVeiculo(Veiculo veiculo){
        veiculos.Add(veiculo);
    }
}
