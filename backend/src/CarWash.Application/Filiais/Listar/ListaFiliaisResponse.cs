using CarWash.Application.Filiais.Common;

namespace CarWash.Application.Filiais.Listar;

/// <summary>
/// Envelope de resposta do <c>GET /api/v1/filiais</c>. Compatível com
/// <c>frontend/src/types/filial.ts</c> (campos extras como <c>pagina</c> e
/// <c>tamanhoPagina</c> são ignorados pelo TS sem breaking change).
/// </summary>
public class ListaFiliaisResponse
{
    public IReadOnlyList<FilialResumoResponse> Itens { get; set; } = [];

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanhoPagina { get; set; }
}
