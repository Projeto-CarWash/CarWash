import { agendaService } from './agendaService';
import api from './api';
import { clienteService } from './clienteService';
import { filialService } from './filialService';
import { servicoService } from './servicoService';

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

  /**
   * Busca os veículos vinculados ao cliente via API real.
   *
   * <p>Consome `GET /api/v1/clientes/{id}` e extrai o array `veiculos`,
   * mapeando para `VeiculoResumido`.</p>
   */
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

  /**
   * Lista os serviços ativos via API real.
   *
   * <p>Consome `GET /api/v1/servicos?ativo=true` e mapeia para `ServicoAtivo`.</p>
   */
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

  /**
   * Estatísticas do ano consultando a API real de agenda mês a mês.
   *
   * <p>Para cada mês do ano, faz uma consulta `GET /api/v1/agenda?formato=simples`
   * com início e fim do mês, usando a primeira filial ativa como filtro obrigatório.
   * Conta os itens por status para montar as estatísticas.</p>
   */
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
      // Sem filial, retorna estatísticas zeradas.
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

      // A API tem janela máxima de 31 dias — cada mês está dentro desse limite.
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
        // Mês sem dados ou erro de rede — contagem zerada.
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

  /**
   * Lista os agendamentos da semana via API real.
   *
   * <p>Consome `GET /api/v1/agenda?formato=simples` com a primeira filial ativa
   * e mapeia os itens para `AgendamentoSemana`.</p>
   */
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

  /**
   * Busca responsáveis vinculados ao cliente (RF024).
   *
   * <p>Não existe endpoint GET dedicado; busca via detalhe do cliente que pode
   * incluir responsáveis, ou retorna lista vazia para que a UI ofereça criação.</p>
   */
  buscarResponsaveisPorCliente(_clienteId: string): Promise<ResponsavelResumido[]> {
    // O backend não expõe GET /api/v1/clientes/{id}/responsaveis.
    // Retorna lista vazia — a UI oferece a criação inline.
    return Promise.resolve([]);
  },

  /**
   * Cria um responsável vinculado ao cliente — `POST /api/v1/clientes/{clienteId}/responsaveis`.
   *
   * <p>Usado pelo wizard de agendamento quando o cliente não possui responsável
   * cadastrado. O responsável é obrigatório (RF024).</p>
   */
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
