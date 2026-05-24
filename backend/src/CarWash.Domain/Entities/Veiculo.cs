namespace CarWash.Domain.Entities;

public class Veiculo
{
    public Guid Id { get; private set; }

    public Guid ClienteId { get; private set; }

    public Cliente Cliente { get; private set; } = null!;

    public string Placa { get; private set; } = string.Empty;

    public string Modelo { get; private set; } = string.Empty;

    public string Fabricante { get; private set; } = string.Empty;

    public string Cor { get; private set; } = string.Empty;

    public int? Ano { get; private set; }

    public bool Ativo { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }

    public DateTimeOffset AtualizadoEm { get; private set; }

    public Veiculo(
        Guid clienteId,
        string placa,
        string modelo,
        string fabricante,
        string cor,
        int? ano = null)
    {
        Id = Guid.NewGuid();
        ClienteId = clienteId;
        Placa = placa;
        Modelo = modelo;
        Fabricante = fabricante;
        Cor = cor;
        Ano = ano;
        Ativo = true;
        CriadoEm = DateTimeOffset.UtcNow;
        AtualizadoEm = DateTimeOffset.UtcNow;
    }

    protected Veiculo()
    {
    }
}
