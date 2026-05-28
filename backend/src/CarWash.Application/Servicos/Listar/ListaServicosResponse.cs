using CarWash.Application.Servicos.Common;

namespace CarWash.Application.Servicos.Listar;

public sealed class ListaServicosResponse
{
    public List<ServicoResponse> Itens { get; set; } = new();
    public int Total { get; set; }
}
