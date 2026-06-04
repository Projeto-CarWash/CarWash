using CarWash.Domain.Entities;

namespace CarWash.Application.Veiculos.Common;

/// <summary>
/// Representação compacta usada na listagem paginada de veículos.
/// </summary>
public class VeiculoResumoResponse
{
    public Guid Id { get; set; }

    public string Placa { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public string Fabricante { get; set; } = string.Empty;

    public string Cor { get; set; } = string.Empty;

    public int? Ano { get; set; }

    public bool Ativo { get; set; }

    public static VeiculoResumoResponse FromEntity(Veiculo veiculo)
    {
        ArgumentNullException.ThrowIfNull(veiculo);
        return new VeiculoResumoResponse
        {
            Id = veiculo.Id,
            Placa = veiculo.Placa,
            Modelo = veiculo.Modelo,
            Fabricante = veiculo.Fabricante,
            Cor = veiculo.Cor,
            Ano = veiculo.Ano,
            Ativo = veiculo.Ativo,
        };
    }
}
