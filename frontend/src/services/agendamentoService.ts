import { agendaService } from './agendaService';
import api from './api';
import { clienteService } from './clienteService';
import { filialService } from './filialService';
import { servicoService } from './servicoService';

import type { AgendaItemDetalhado, AgendaItemSimples } from '@/types/agenda';
import type {
  AgendamentoDetalhe,
  AgendamentoResponse,
  AgendamentoSemana,
  EditarAgendamentoPayload,
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

/**
 * Resolve a filial padrão (primeira ativa retornada pelo backend) quando a tela
 * não informa explicitamente uma filial. Retorna '' se não houver/erro.
 */
async function resolverFilialPadrao(): Promise<string> {
  try {
    const filiais = await filialService.listar();
    return filiais.itens?.[0]?.id ?? '';
  } catch {
    return '';
  }
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
    return cliente.veiculos.map((v) => {
      return {
        id: v.id,
        placa: v.placa,
        modelo: v.modelo,
        cor: v.cor,
      };
    });
  },

  async listarServicosAtivos(): Promise<ServicoAtivo[]> {
    const response = await servicoService.listar({ ativo: true });
    return response.itens.map((s) => {
      return {
        id: s.id,
        nome: s.nome,
        preco: s.preco,
        duracao: s.duracaoMin,
        descricao: '',
      };
    });
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

  async obterEstatisticasAno(ano: number, filialId?: string): Promise<EstatisticasMes[]> {
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

    // Usa a filial informada pela tela; sem ela, cai na filial padrão.
    const filialEfetiva = filialId ?? (await resolverFilialPadrao());

    if (!filialEfetiva) {
      return nomesMeses.map((nome, index) => ({
        mes: index + 1,
        nome,
        agendado: 0,
        emAndamento: 0,
        concluido: 0,
        cancelado: 0,
        total: 0,
      }));
    }

    const resultados: EstatisticasMes[] = [];

    for (let m = 0; m < 12; m++) {
      const inicio = new Date(ano, m, 1);
      const fim = new Date(ano, m + 1, 0, 23, 59, 59);

      let agendado = 0;
      let emAndamento = 0;
      let concluido = 0;
      let cancelado = 0;

      try {
        const resp = await agendaService.consultarSimples({
          formato: 'simples',
          inicio: inicio.toISOString().slice(0, 16),
          fim: fim.toISOString().slice(0, 16),
          filialId: filialEfetiva,
        });

        for (const item of resp.data) {
          const s = item.status.toUpperCase();
          if (s === 'AGENDADO') {
            agendado++;
          } else if (s === 'EM_ANDAMENTO') {
            emAndamento++;
          } else if (s === 'CONCLUIDO') {
            concluido++;
          } else if (s === 'CANCELADO') {
            cancelado++;
          }
        }
      } catch {
        // Erro ao consultar agenda para o mês - valores padrão (0) serão usados
      }

      const total = agendado + emAndamento + concluido + cancelado;
      resultados.push({
        mes: m + 1,
        nome: nomesMeses[m]!,
        agendado,
        emAndamento,
        concluido,
        cancelado,
        total,
      });
    }

    return resultados;
  },

  async listarAgendamentosSemana(
    dataInicio: Date,
    dataFim: Date,
    filialId?: string,
  ): Promise<AgendamentoSemana[]> {
    const filialEfetiva = filialId ?? (await resolverFilialPadrao());
    if (!filialEfetiva) return [];

    try {
      const resp = await agendaService.consultarSimples({
        formato: 'simples',
        inicio: dataInicio.toISOString().slice(0, 16),
        fim: dataFim.toISOString().slice(0, 16),
        filialId: filialEfetiva,
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

  /**
   * Busca o item DETALHADO de um agendamento dentro de uma janela de semana,
   * reaproveitando o endpoint `GET /api/v1/agenda` no formato `detalhado`
   * (mesma filial usada por {@link listarAgendamentosSemana}). Retorna `null`
   * quando não encontrado. Usado pelo modal de detalhe do calendário.
   */
  async obterDetalheNaSemana(
    agendamentoId: string,
    dataInicio: Date,
    dataFim: Date,
    filialId?: string,
  ): Promise<AgendaItemDetalhado | null> {
    const filialEfetiva = filialId ?? (await resolverFilialPadrao());
    if (!filialEfetiva) return null;

    const resp = await agendaService.consultarDetalhada({
      formato: 'detalhado',
      inicio: dataInicio.toISOString().slice(0, 16),
      fim: dataFim.toISOString().slice(0, 16),
      filialId: filialEfetiva,
    });

    return resp.data.find((item) => item.agendamentoId === agendamentoId) ?? null;
  },

  async buscarResponsaveisPorCliente(clienteId: string): Promise<ResponsavelResumido[]> {
    try {
      const { data } = await api.get<{ id: string; nome: string; documento: string }[]>(
        `/api/v1/clientes/${clienteId}/responsaveis`,
      );
      return data.map((r) => ({
        id: r.id,
        nome: r.nome,
        documento: r.documento,
      }));
    } catch {
      return [];
    }
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

  /**
   * Consulta detalhada de um agendamento — `GET /api/v1/agendamentos/{id}`
   * (RF010). Resposta no envelope `{ message, data, traceId }`.
   */
  async obterPorId(id: string): Promise<AgendamentoDetalhe> {
    const { data } = await api.get<{ data: AgendamentoDetalhe }>(`/api/v1/agendamentos/${id}`);
    return data.data;
  },

  /**
   * Edição de agendamento — `PATCH /api/v1/agendamentos/{id}` (RF010). Só os
   * campos enviados são alterados; o backend só permite quando status=AGENDADO.
   */
  async editar(id: string, payload: EditarAgendamentoPayload): Promise<AgendamentoResponse> {
    const { data } = await api.patch<AgendamentoResponse>(`/api/v1/agendamentos/${id}`, payload);
    return data;
  },
};
