using CarWash.Application.Veiculos.Common;

namespace CarWash.Application.Veiculos.Listar;

public class ListaVeiculosResponse
{
    public IReadOnlyList<VeiculoResumoResponse> Itens { get; set; } = [];

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanhoPagina { get; set; }
}
