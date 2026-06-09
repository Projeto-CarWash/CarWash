import { agendaService } from './agendaService';
import api from './api';
import { filialService } from './filialService';

import type { AgendaItemSimples } from '@/types/agenda';
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
  ResponsavelResumido,
  ServicoAtivo,
  VeiculoResumido,
  CancelarAgendamentoResponse,
} from '@/types/agendamento';

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
    const { data } = await api.get<{
      veiculos: { id: string; placa: string; modelo: string; fabricante: string; cor: string }[];
    }>(`/api/v1/clientes/${clienteId}`);

    return (data.veiculos ?? []).map((v) => ({
      id: v.id,
      placa: v.placa,
      modelo: v.modelo,
      cor: v.cor,
    }));
  },

  async listarServicosAtivos(): Promise<ServicoAtivo[]> {
    const { data } = await api.get<{
      itens: {
        id: string;
        nome: string;
        preco: number;
        duracaoMin: number;
        ativo: boolean;
        criadoEm: string;
        atualizadoEm: string;
      }[];
    }>('/api/v1/servicos', {
      params: { ativo: true },
    });

    return (data.itens ?? []).map((s) => ({
      id: s.id,
      nome: s.nome,
      preco: s.preco,
      duracao: s.duracaoMin,
      descricao: undefined,
    }));
  },

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

  async obterEstatisticasAno(ano: number): Promise<EstatisticasMes[]> {
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

    // Busca a primeira filial ativa para usar como filtro obrigatório da agenda.
    let filialId = '';
    try {
      const filiais = await filialService.listar();
      filialId = filiais.itens?.[0]?.id ?? '';
    } catch {
      // Falha ao buscar filiais - continuará com filialId vazio retornando valores padrão
    }

    if (!filialId) {
      return nomesMeses.map((nome, index) => ({
        mes: index + 1,
        nome,
        confirmados: 0,
        pendentes: 0,
        cancelados: 0,
        total: 0,
      }));
    }

    const resultados: EstatisticasMes[] = [];

    for (let m = 0; m < 12; m++) {
      const inicio = new Date(ano, m, 1);
      const fim = new Date(ano, m + 1, 0, 23, 59, 59);

      let confirmados = 0;
      let pendentes = 0;
      let cancelados = 0;

      try {
        const resp = await agendaService.consultarSimples({
          formato: 'simples',
          inicio: inicio.toISOString().slice(0, 16),
          fim: fim.toISOString().slice(0, 16),
          filialId,
        });

        for (const item of resp.data) {
          const s = item.status.toUpperCase();
          if (s === 'AGENDADO' || s === 'EM_ANDAMENTO' || s === 'CONCLUIDO') {
            confirmados++;
          } else if (s === 'CANCELADO') {
            cancelados++;
          } else {
            pendentes++;
          }
        }
      } catch {
        // Erro ao consultar agenda para o mês - valores padrão (0) serão usados
      }

      const total = confirmados + pendentes + cancelados;
      resultados.push({
        mes: m + 1,
        nome: nomesMeses[m]!,
        confirmados,
        pendentes,
        cancelados,
        total,
      });
    }

    return resultados;
  },

  async listarAgendamentosSemana(dataInicio: Date, dataFim: Date): Promise<AgendamentoSemana[]> {
    let filialId = '';
    try {
      const filiais = await filialService.listar();
      filialId = filiais.itens?.[0]?.id ?? '';
    } catch {
      return [];
    }

    if (!filialId) return [];

    try {
      const resp = await agendaService.consultarSimples({
        formato: 'simples',
        inicio: dataInicio.toISOString().slice(0, 16),
        fim: dataFim.toISOString().slice(0, 16),
        filialId,
      });

      return resp.data.map((item: AgendaItemSimples) => ({
        id: item.agendamentoId,
        titulo: item.titulo,
        cliente: item.clienteNome,
        inicio: item.inicio,
        fim: item.fim,
        status: item.status.toLowerCase() as AgendamentoSemana['status'],
      }));
    } catch {
      return [];
    }
  },

  buscarResponsaveisPorCliente(_clienteId: string): Promise<ResponsavelResumido[]> {
    return Promise.resolve([]);
  },

  async criarResponsavel(
    clienteId: string,
    dados: { nome: string; documento: string; grauVinculo: string },
  ): Promise<ResponsavelResumido> {
    const { data } = await api.post<{ id: string; nome: string }>(
      `/api/v1/clientes/${clienteId}/responsaveis`,
      dados,
    );
    return { id: data.id, nome: data.nome ?? dados.nome };
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
