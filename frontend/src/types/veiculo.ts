/**
 * Item da listagem de veículos retornada por `GET /api/v1/veiculos`.
 * Espelha `VeiculoListaItemResponse` do backend (slice Veiculos/Listar),
 * incluindo o cliente vinculado (RF005/RF022).
 */
export interface VeiculoListaItem {
  id: string;
  clienteId: string;
  clienteNome: string;
  clienteAtivo: boolean;
  placa: string;
  modelo: string;
  fabricante: string;
  cor: string;
  ano?: number | null;
  ativo: boolean;
  criadoEm: string;
}

export interface ListaVeiculos {
  itens: VeiculoListaItem[];
  total: number;
  pagina: number;
  tamanhoPagina: number;
}
