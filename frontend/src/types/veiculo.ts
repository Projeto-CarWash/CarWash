

export interface VeiculoResumo {
  id: string;
  clienteId: string;
  placa: string;
  marca: string;
  modelo: string;
  cor?: string;
  ativo: boolean;
  fabricante?: string;
  observacoes?: string;
}

export interface ListaVeiculos {
  itens: VeiculoResumo[];
  total: number;
  pagina: number;
  tamanhoPagina: number;
}
