import api from '@/services/api';

export interface Servico {
  id: string;
  nome: string;
  preco: number;
  duracaoMin: number;
  ativo: boolean;
  criadoEm: string;
  atualizadoEm: string;
}

export interface ListaServicosResponse {
  itens: Servico[];
  total: number;
}

export const servicoService = {
  async listar(params?: { ativo?: boolean; query?: string }): Promise<ListaServicosResponse> {
    const response = await api.get('/servicos', { params });
    return response.data;
  },

  async cadastrar(data: { nome: string; preco: number; duracaoMin: number }): Promise<Servico> {
    const response = await api.post('/servicos', data);
    return response.data;
  },

  async atualizar(id: string, data: { nome: string; preco: number; duracaoMin: number }): Promise<Servico> {
    const response = await api.patch(`/servicos/${id}`, data);
    return response.data;
  },

  async alterarStatus(id: string, ativo: boolean): Promise<Servico> {
    const response = await api.patch(`/servicos/${id}/status`, { ativo });
    return response.data;
  },
};
