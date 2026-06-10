# BUG — Dashboard (RF013) nunca carrega métricas: parâmetros de query divergentes (front × back)

**Severidade:** Alta (a tela principal do RF013 exibe "Erro ao carregar dados do painel" para todos os usuários, sempre).
**Card afetado:** RF013 — Dashboard com métricas operacionais e financeiras.
**Componentes:** frontend `dashboardService.obterMetricas`, backend `DashboardMetricasEndpoints`.
**Como foi encontrado:** passada de QA de **UI com screenshots** (Playwright) — não aparecia no QA só por API, porque a API foi testada com os nomes corretos, e o E2E só verifica o `<h1>Painel de Métricas</h1>`, não o carregamento das métricas.

## Descrição

> **Atualização (QA de UI):** a investigação visual mostrou que o bug tinha **duas camadas**:
> 1. **Parâmetros** — o front enviava `inicio`/`fim`, o back espera `dataInicio`/`dataFim` → **400** → card "Erro ao carregar dados do painel".
> 2. **Forma da resposta** — mesmo após corrigir os parâmetros (200), o painel ficava **em branco** (crash): o front consumia a resposta como objeto achatado `{ total, faturamento, ... }`, mas o backend devolve um **envelope aninhado** (`data.operacional.totalAtendimentos`, `data.financeiro.faturamentoTotal`, ...). Assim `metricas.total` era `undefined` e `metricas.total.toLocaleString()` lançava → tela branca (sem error boundary).
>
> A divergência passou despercebida porque **o mock de dev (`src/mocks/handlers.ts`) também estava no formato achatado e com `inicio`/`fim`** — ou seja, o mock acompanhava a suposição errada do front, então em modo mock "funcionava".

### Camada 1 — parâmetros

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

Frontend (`dashboardService.obterMetricas`), sem alteração de backend:
1. Enviar `dataInicio`/`dataFim` (datas `YYYY-MM-DD`, aceitas pelo backend). `filialId`/`status` já estavam corretos.
2. **Mapear** o envelope do backend para a forma achatada `DashboardMetricas` que o painel consome:
   `total ← data.operacional.totalAtendimentos`, `pendentes/concluidos/cancelados` idem, `ocupacao ← taxaConclusao`, `tempoMedio ← tempoMedioAtendimentoMin`, `faturamento ← data.financeiro.faturamentoTotal`, `ticketMedio ← data.financeiro.ticketMedio`.
3. Alinhar o mock de dev (`src/mocks/handlers.ts`) ao envelope real (e a `dataInicio`/`dataFim`), para o modo mock refletir a API verdadeira.

- Teste de regressão: `dashboardService.test.ts` — garante os parâmetros `dataInicio`/`dataFim` **e** o mapeamento envelope→achatado.

## Verificação

- Screenshot pós-fix (`dash-happy.png`): painel renderiza header, filtros (com filiais carregadas) e o **empty-state** correto ("Nenhum dado encontrado para o período") — sem card de erro e sem tela branca. Confirma que `metricas.total` agora é número.
- API: `GET /dashboard/metricas?dataInicio=...&dataFim=...` → 200 com o envelope.
