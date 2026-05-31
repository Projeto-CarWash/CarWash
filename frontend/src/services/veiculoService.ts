import api from './api';

import type { VeiculoFormData } from '@/schemas/veiculoSchema';

export interface Veiculo {
  id: string;
  clienteId: string;
  placa: string;
  modelo: string;
  fabricante: string;
  cor: string;
  ano?: number | null;
  ativo: boolean;
  criadoEm: string;
  atualizadoEm: string;
}

export interface ListarVeiculosResponse {
  itens: Veiculo[];
}

// Contrato alinhado ao slice CriarVeiculo do backend (Application/Veiculos/Criar):
// retorna o agregado Veiculo completo, não { id, mensagem, traceId }.
export type CriarVeiculoResponse = Veiculo;

export const veiculoService = {
  async cadastrar(clienteId: string, dados: VeiculoFormData): Promise<CriarVeiculoResponse> {
    const payload = {
      placa: dados.placa, // já normalizado pelo transform do zod
      modelo: dados.modelo,
      fabricante: dados.fabricante,
      cor: dados.cor,
    };
    const { data } = await api.post<CriarVeiculoResponse>(
      `/api/v1/clientes/${clienteId}/veiculos`,
      payload,
    );
    return data;
  },

  async listarPorCliente(clienteId: string): Promise<Veiculo[]> {
    const { data } = await api.get<ListarVeiculosResponse>(
      `/api/v1/clientes/${clienteId}/veiculos`,
    );
    return data.itens;
  },
};
