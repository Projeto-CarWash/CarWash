/**
 * Service do histórico de atendimentos por cliente (RF012).
 *
 * <p>Não cria endpoints — compõe sobre `agendaService.consultarDetalhada()`
 * (`GET /api/v1/agenda?formato=detalhado`) filtrado por `clienteId`.</p>
 *
 * <p>O filtro `ultimosDias` é convertido em intervalo `inicio`/`fim` no
 * frontend antes de chamar a API.</p>
 */

import { agendaService } from './agendaService';
import { filialService } from './filialService';

import type { AgendaFiltros } from '@/types/agenda';
import type { HistoricoFiltros, HistoricoResponse } from '@/types/historico';

/**
 * Converte um valor de `<input type="date">` (`YYYY-MM-DD`) em datetime-local
 * no início do dia para uso como `inicio` do filtro da agenda.
 */
function dataParaInicioLocal(dataStr: string): string {
  return `${dataStr}T00:00`;
}

/**
 * Converte um valor de `<input type="date">` (`YYYY-MM-DD`) em datetime-local
 * no final do dia para uso como `fim` do filtro da agenda.
 */
function dataParaFimLocal(dataStr: string): string {
  return `${dataStr}T23:59`;
}

/**
 * Calcula o intervalo de datas a partir de `ultimosDias` dias atrás até agora.
 * Retorna par `[inicioLocal, fimLocal]` no formato `YYYY-MM-DDTHH:mm`.
 */
function calcularIntervaloUltimosDias(dias: number): [string, string] {
  const agora = new Date();
  agora.setSeconds(0, 0);

  const inicio = new Date(agora);
  inicio.setDate(inicio.getDate() - dias);

  const offset = agora.getTimezoneOffset() * 60_000;
  const fimLocal = new Date(agora.getTime() - offset).toISOString().slice(0, 16);
  const inicioLocal = new Date(inicio.getTime() - offset).toISOString().slice(0, 16);

  return [inicioLocal, fimLocal];
}

/**
 * Obtém o ID da primeira filial ativa para uso como filtro obrigatório.
 * Mesmo padrão adotado em `agendamentoService.obterEstatisticasAno()`.
 */
async function obterFilialPadrao(): Promise<string> {
  const filiais = await filialService.listar();
  return filiais.itens?.[0]?.id ?? '';
}

export const historicoService = {
  /**
   * Consulta o histórico de atendimentos de um cliente.
   *
   * <p>Monta os filtros de `AgendaFiltros` a partir de `HistoricoFiltros` e
   * chama `agendaService.consultarDetalhada()`. Resolve a filial automaticamente
   * quando não informada.</p>
   */
  async consultarHistorico(filtros: HistoricoFiltros): Promise<HistoricoResponse> {
    let filialId = filtros.filialId;
    if (!filialId) {
      filialId = await obterFilialPadrao();
    }

    if (!filialId) {
      return {
        itens: [],
        mensagem: 'Nenhuma filial ativa encontrada para consulta.',
      };
    }

    // Resolve o intervalo de datas
    let inicio: string;
    let fim: string;

    if (filtros.ultimosDias) {
      [inicio, fim] = calcularIntervaloUltimosDias(filtros.ultimosDias);
    } else if (filtros.dataInicio && filtros.dataFim) {
      inicio = dataParaInicioLocal(filtros.dataInicio);
      fim = dataParaFimLocal(filtros.dataFim);
    } else {
      // Padrão: últimos 30 dias
      [inicio, fim] = calcularIntervaloUltimosDias(30);
    }

    const agendaFiltros: AgendaFiltros = {
      formato: 'detalhado',
      inicio,
      fim,
      filialId,
      clienteId: filtros.clienteId,
      status: filtros.status ?? undefined,
    };

    const resposta = await agendaService.consultarDetalhada(agendaFiltros);

    // Ordena por data mais recente primeiro (estabilidade de ordenação)
    const itensOrdenados = [...resposta.data].sort(
      (a, b) => new Date(b.inicio).getTime() - new Date(a.inicio).getTime(),
    );

    return {
      itens: itensOrdenados,
      mensagem: resposta.message,
    };
  },

  /** Obtém o ID da primeira filial ativa (usado para inicialização). */
  obterFilialPadrao,
};
