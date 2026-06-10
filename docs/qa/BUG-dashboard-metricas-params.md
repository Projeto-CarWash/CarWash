# BUG — Dashboard (RF013) nunca carrega métricas: parâmetros de query divergentes (front × back)

**Severidade:** Alta (a tela principal do RF013 exibe "Erro ao carregar dados do painel" para todos os usuários, sempre).
**Card afetado:** RF013 — Dashboard com métricas operacionais e financeiras.
**Componentes:** frontend `dashboardService.obterMetricas`, backend `DashboardMetricasEndpoints`.
**Como foi encontrado:** passada de QA de **UI com screenshots** (Playwright) — não aparecia no QA só por API, porque a API foi testada com os nomes corretos, e o E2E só verifica o `<h1>Painel de Métricas</h1>`, não o carregamento das métricas.

## Descrição

O frontend chama `GET /api/v1/dashboard/metricas` enviando os parâmetros de período como **`inicio`/`fim`**:

```ts
const params = { inicio: filtros.inicio, fim: filtros.fim, filialId, status };
```

O backend, porém, lê **`dataInicio`/`dataFim`** (`DateTimeOffset? dataInicio, dataFim`) e retorna **400** quando ausentes (`"dataInicio é obrigatório."`). Resultado: a query do TanStack entra em `isError` e a tela renderiza o estado **"Erro ao carregar dados do painel"**.

## Evidência

- Screenshot `01-rf013-dashboard.png`: painel com o card de erro vermelho.
- API:
  - `GET /dashboard/metricas?inicio=2026-05-11&fim=2026-06-10` → **400**
  - `GET /dashboard/metricas?dataInicio=2026-05-11&dataFim=2026-06-10` → **200**

## Comportamento esperado

O painel deve carregar e exibir as métricas (total, pendentes, concluídos, cancelados, ocupação, tempo médio, faturamento, ticket médio) para o período padrão (últimos 30 dias).

## Correção

Frontend: enviar `dataInicio`/`dataFim` (datas no formato `YYYY-MM-DD`, que o backend aceita). `filialId`/`status` já estavam corretos. Sem alteração de backend.

- Teste de regressão: `dashboardService.test.ts` — garante que a query envia `dataInicio`/`dataFim` e **não** `inicio`/`fim`.
