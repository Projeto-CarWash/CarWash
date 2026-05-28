using CarWash.Application.Servicos.Common;

namespace CarWash.Application.Servicos.Listar;

public class ListaServicosResponse
{
    public IReadOnlyList<ServicoResumoResponse> Itens { get; set; } = [];

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanhoPagina { get; set; }
}
