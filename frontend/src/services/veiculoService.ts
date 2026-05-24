import api from './api';

import type { VeiculoFormData } from '@/schemas/veiculoSchema';
import type { ListaVeiculos } from '@/types/veiculo';

export interface Veiculo {
  id: string;
  clienteId: string;
  placa: string;
  modelo: string;
  fabricante: string;
  marca: string;
  cor: string;
  observacoes?: string;
  ativo: boolean;
  criadoEm?: string;
  atualizadoEm?: string;
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
      modelo: dados.modelo,
      fabricante: dados.fabricante,
      cor: dados.cor,
      observacoes: dados.observacoes ?? undefined,
    };
    const { data } = await api.post<CriarVeiculoResponse>(
      `/api/v1/clientes/${clienteId}/veiculos`,
      payload,
    );
    return data;
  },

  async listarPorCliente(clienteId: string): Promise<ListaVeiculos> {
    const { data } = await api.get<ListaVeiculos>('/api/v1/veiculos', {
      params: { clienteId, tamanhoPagina: 100 },
    });
    return data;
  },
};
