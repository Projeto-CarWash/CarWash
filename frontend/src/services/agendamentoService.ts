import api from './api';
import { clienteService } from './clienteService';
import { servicoService } from './servicoService';

import type {
  AgendamentoResponse,
  AgendamentoSemana,
  ClienteResumido,
  ConfirmarAgendamentoRequest,
  CriarAgendamentoPayload,
  CriarAgendamentoRequest,
  CriarAgendamentoResponse,
  EstatisticasMes,
  PreConfirmacaoResponse,
  ServicoAtivo,
  VeiculoResumido,
  CancelarAgendamentoResponse,
} from '@/types/agendamento';

function delay(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

export const agendamentoService = {
  async buscarClientes(busca: string): Promise<ClienteResumido[]> {
    const { data } = await api.get<{ itens: ClienteResumido[] }>('/api/v1/clientes', {
      params: {
        ...(busca.trim() ? { busca: busca.trim() } : {}),
        pagina: 1,
        tamanhoPagina: 50,
      },
    });

    return data.itens;
  },

  async buscarVeiculosPorCliente(clienteId: string): Promise<VeiculoResumido[]> {
    const cliente = await clienteService.obterPorId(clienteId);
    return cliente.veiculos.map((v) => ({
      id: v.id,
      placa: v.placa,
      modelo: v.modelo,
      cor: v.cor,
    }));
  },

  async listarServicosAtivos(): Promise<ServicoAtivo[]> {
    const response = await servicoService.listar({ ativo: true });
    return response.itens.map((s) => ({
      id: s.id,
      nome: s.nome,
      preco: s.preco,
      duracao: s.duracaoMin,
      descricao: '',
    }));
  },

  /**
   * Cria o agendamento em passo único — `POST /api/v1/agendamentos` (RF007/RF019).
   *
   * <p>Envia o payload real (incluindo `filialId`); os erros HTTP (400/401/403/
   * 404/409/500) são propagados para a UI tratar — sem mock.</p>
   */
  async criarAgendamento(payload: CriarAgendamentoPayload): Promise<CriarAgendamentoResponse> {
    const { data } = await api.post<AgendamentoResponse>('/api/v1/agendamentos', payload);
    return { id: data.id };
  },

  async criar(payload: CriarAgendamentoRequest): Promise<AgendamentoResponse> {
    const { data } = await api.post<AgendamentoResponse>('/api/v1/agendamentos', payload);
    return data;
  },

  async preConfirmar(payload: CriarAgendamentoRequest): Promise<PreConfirmacaoResponse> {
    const { data } = await api.post<PreConfirmacaoResponse>(
      '/api/v1/agendamentos/pre-confirmacao',
      payload,
    );
    return data;
  },

  async confirmar(payload: ConfirmarAgendamentoRequest): Promise<AgendamentoResponse> {
    const { data } = await api.post<AgendamentoResponse>('/api/v1/agendamentos/confirmar', payload);
    return data;
  },

  async obterEstatisticasAno(_ano: number): Promise<EstatisticasMes[]> {
    await delay(500);

    const nomesMeses = [
      'JANEIRO',
      'FEVEREIRO',
      'MARCO',
      'ABRIL',
      'MAIO',
      'JUNHO',
      'JULHO',
      'AGOSTO',
      'SETEMBRO',
      'OUTUBRO',
      'NOVEMBRO',
      'DEZEMBRO',
    ];

    return nomesMeses.map((nome, index) => ({
      mes: index + 1,
      nome,
      confirmados: 0,
      pendentes: 0,
      cancelados: 0,
      total: 0,
    }));
  },

  async listarAgendamentosSemana(_dataInicio: Date, _dataFim: Date): Promise<AgendamentoSemana[]> {
    await delay(600);
    return [];
  },

  async cancelar(id: string, motivoCancelamento: string): Promise<CancelarAgendamentoResponse> {
    const { data } = await api.patch<CancelarAgendamentoResponse>(
      `/api/v1/agendamentos/${id}/cancelar`,
      {
        motivoCancelamento,
        origem: 'CLIENTE',
      },
    );
    return data;
  },

  async atualizar(
    id: string,
    payload: { observacoes: string | null },
  ): Promise<AgendamentoResponse> {
    const { data } = await api.put<AgendamentoResponse>(`/api/v1/agendamentos/${id}`, payload);
    return data;
  },
};
