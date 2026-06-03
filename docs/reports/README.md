# Relatórios semanais — CarWash Devs (maio/2026)

Relatórios gerados a partir do export do quadro Trello `Carwash devs` (`bI9HqGGU - carwash-devs.json`).
Horários em fuso de Brasília (UTC−3). Gerado em 29/05/2026.

## Estrutura

Uma pasta por semana, cada uma com 3 entregáveis:

| Pasta | Período | KPIs | Bugs | Burndown |
|---|---|---|---|---|
| `semana-03-09/` | 03–09 mai | `relatorio-kpi.md` | `relatorio-de-bugs.md` | `painel-burndown.html` |
| `semana-10-16/` | 10–16 mai | idem | idem | idem |
| `semana-17-23/` | 17–23 mai | idem | idem | idem |
| `semana-24-30/` | 24–30 mai (atual) | idem | idem | idem |

- **`relatorio-kpi.md`** — KPIs por integrante (movimentações, cards iniciados/concluídos, comentários, anexos, itens de checklist), tempo médio por card (lead time), distribuição por label/módulo e log de movimentações.
- **`relatorio-de-bugs.md`** — bugs identificados na semana (cards que entraram na coluna *BUGS* ou com label *BUG*), com severidade, status, referência (RF/RN/CA/DB) e responsáveis. Baseado em `relatorio-de-bugs-template.md`.
- **`painel-burndown.html`** — painel visual de burndown da semana (Chart.js, abre direto no navegador, sem dependências locais).

## ⚠️ Cobertura de dados (importante)

O export do Trello traz no máximo **1.000 ações**, cobrindo apenas **22/05 → 29/05**. Por isso:

| Semana | Movimentações/atividade | Observação |
|---|---|---|
| 03–09 | ❌ ausente | Fase de backlog; nenhum card criado/movido no quadro |
| 10–16 | ❌ ausente | Idem |
| 17–23 | ⚠️ parcial | Histórico de ações só para 22 e 23/05 |
| 24–30 | ✅ completa | Cobertura total |

Métricas zeradas nas duas primeiras semanas refletem **ausência de registro no quadro**, não inatividade real da equipe. Tempos de lead nas semanas iniciais usam proxies (timestamp do ID do card e `dateCompleted`).
