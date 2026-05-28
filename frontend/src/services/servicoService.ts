import api from './api';

import type { ListaServicos, ServicoResumo } from '@/types/servico';

export interface CriarServicoRequest {
  nome: string;
  preco: number;
  duracaoMin: number;
}

export interface AtualizarServicoRequest {
  nome: string;
  preco: number;
  duracaoMin: number;
}

export const servicoService = {
  /**
   * Lista os serviços do catálogo (opcionalmente filtrando por ativo).
   */
  async listar(params?: { ativo?: boolean }): Promise<ListaServicos> {
    const { data } = await api.get<ListaServicos>('/api/v1/servicos', {
      params,
    });
    return data;
  },

  /**
   * Cadastra um novo serviço no catálogo.
   */
  async criar(payload: CriarServicoRequest): Promise<{ id: string; mensagem: string }> {
    const { data } = await api.post<{ id: string; mensagem: string }>('/api/v1/servicos', payload);
    return data;
  },

  /**
   * Atualiza um serviço existente no catálogo.
   */
  async atualizar(id: string, payload: AtualizarServicoRequest): Promise<ServicoResumo> {
    const { data } = await api.put<ServicoResumo>(`/api/v1/servicos/${id}`, payload);
    return data;
  },

  /**
   * Ativa ou desativa um serviço no catálogo.
   */
  async alterarStatus(id: string, ativo: boolean): Promise<ServicoResumo> {
    const { data } = await api.patch<ServicoResumo>(`/api/v1/servicos/${id}/status`, { ativo });
    return data;
  },
};
