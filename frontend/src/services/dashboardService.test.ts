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
  const metricas = {
    total: 1,
    pendentes: 1,
    concluidos: 0,
    cancelados: 0,
    ocupacao: 10,
    tempoMedio: 30,
    faturamento: 100,
    ticketMedio: 100,
  };

  it('envia dataInicio/dataFim (não inicio/fim) na query', async () => {
    let capturada: URLSearchParams | null = null;
    server.use(
      http.get('*/api/v1/dashboard/metricas', ({ request }) => {
        capturada = new URL(request.url).searchParams;
        return HttpResponse.json(metricas);
      }),
    );

    const filtros: DashboardFiltros = { inicio: '2026-05-11', fim: '2026-06-10' };
    const data = await dashboardService.obterMetricas(filtros);

    expect(capturada).not.toBeNull();
    expect(capturada!.get('dataInicio')).toBe('2026-05-11');
    expect(capturada!.get('dataFim')).toBe('2026-06-10');
    // Garante que os nomes antigos (que causavam 400) não são mais enviados.
    expect(capturada!.has('inicio')).toBe(false);
    expect(capturada!.has('fim')).toBe(false);
    expect(data.total).toBe(1);
  });

  it('encaminha filialId e status quando informados', async () => {
    let capturada: URLSearchParams | null = null;
    server.use(
      http.get('*/api/v1/dashboard/metricas', ({ request }) => {
        capturada = new URL(request.url).searchParams;
        return HttpResponse.json(metricas);
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
