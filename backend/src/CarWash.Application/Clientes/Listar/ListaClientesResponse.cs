using CarWash.Application.Clientes.Common;

namespace CarWash.Application.Clientes.Listar;

public class ListaClientesResponse
{
    public IReadOnlyList<ClienteResumoResponse> Itens { get; set; } = [];

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanhoPagina { get; set; }
}
