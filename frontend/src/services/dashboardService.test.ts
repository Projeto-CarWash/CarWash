import { HttpResponse, http } from 'msw';
import { describe, expect, it } from 'vitest';

import { dashboardService } from '@/services/dashboardService';
import { server } from '@/test/mswServer';

import type { DashboardFiltros } from '@/types/dashboard';

/**
 * RF013 — guarda o contrato dos parâmetros do dashboard.
 *
 * Bug de QA (UI): o service enviava `inicio`/`fim`, mas o backend espera
 * `dataInicio`/`dataFim` → 400 → "Erro ao carregar dados do painel". Este teste
 * fixa os nomes corretos dos parâmetros enviados.
 */
describe('dashboardService.obterMetricas', () => {
  // Envelope no formato REAL do backend (DashboardMetricasResponse).
  const envelope = {
    message: 'ok',
    data: {
      periodo: { dataInicio: '2026-05-11', dataFim: '2026-06-10' },
      filtrosAplicados: { filialId: null, clienteId: null, status: null },
      operacional: {
        totalAtendimentos: 7,
        pendentes: 2,
        concluidos: 4,
        cancelados: 1,
        taxaConclusao: 57.1,
        tempoMedioAtendimentoMin: 33,
      },
      financeiro: {
        faturamentoTotal: 980.5,
        ticketMedio: 140.07,
        faturamentoPorFilial: [],
        faturamentoPorServico: [],
        valorMedioPorCliente: 0,
      },
    },
    traceId: 't',
  };

  it('envia dataInicio/dataFim (não inicio/fim) e mapeia o envelope para a forma achatada', async () => {
    let capturada: URLSearchParams | null = null;
    server.use(
      http.get('*/api/v1/dashboard/metricas', ({ request }) => {
        capturada = new URL(request.url).searchParams;
        return HttpResponse.json(envelope);
      }),
    );

    const filtros: DashboardFiltros = { inicio: '2026-05-11', fim: '2026-06-10' };
    const data = await dashboardService.obterMetricas(filtros);

    // params corretos
    expect(capturada).not.toBeNull();
    expect(capturada!.get('dataInicio')).toBe('2026-05-11');
    expect(capturada!.get('dataFim')).toBe('2026-06-10');
    expect(capturada!.has('inicio')).toBe(false);
    expect(capturada!.has('fim')).toBe(false);

    // mapeamento envelope → DashboardMetricas (o que o painel renderiza)
    expect(data).toEqual({
      total: 7,
      pendentes: 2,
      concluidos: 4,
      cancelados: 1,
      ocupacao: 57.1,
      tempoMedio: 33,
      faturamento: 980.5,
      ticketMedio: 140.07,
    });
  });

  it('encaminha filialId e status quando informados', async () => {
    let capturada: URLSearchParams | null = null;
    server.use(
      http.get('*/api/v1/dashboard/metricas', ({ request }) => {
        capturada = new URL(request.url).searchParams;
        return HttpResponse.json(envelope);
      }),
    );

    const filtros: DashboardFiltros = {
      inicio: '2026-05-11',
      fim: '2026-06-10',
      filialId: '22222222-2222-2222-2222-222222222222',
      status: 'PENDENTE',
    };
    await dashboardService.obterMetricas(filtros);

    expect(capturada!.get('filialId')).toBe('22222222-2222-2222-2222-222222222222');
    expect(capturada!.get('status')).toBe('PENDENTE');
  });
});
