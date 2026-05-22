import api from './api';

import type { VeiculoFormData } from '@/schemas/veiculoSchema';

export interface Veiculo {
  id: string;
  clienteId: string;
  placa: string;
  modelo: string;
  fabricante: string;
  cor: string;
  ano?: number;
  ativo: boolean;
  criadoEm: string;
  atualizadoEm: string;
}

export interface ListarVeiculosResponse {
  itens: Veiculo[];
}

export interface CriarVeiculoResponse {
  id: string;
  mensagem: string;
  traceId: string;
}

export const veiculoService = {
  async cadastrar(clienteId: string, dados: VeiculoFormData): Promise<CriarVeiculoResponse> {
    const payload = {
      placa: dados.placa, // already normalized by zod schema transform
      modelo: dados.modelo.trim(),
      fabricante: dados.fabricante.trim(),
      cor: dados.cor.trim(),
      ano: dados.ano && dados.ano.trim() !== '' ? Number(dados.ano) : undefined,
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
