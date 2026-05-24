/**
 * Tipos de veículo (RF004/RF005) usados no formulário de agendamento.
 *
 * <p>O contrato de listagem ainda é dependência pendente do backend
 * (ver `services/veiculoService.ts`).</p>
 */

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
