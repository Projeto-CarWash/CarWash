# Relatório Consolidado de QA — MOMENTO 3

**Data:** 2026-06-10
**Ambiente:** stack local (backend .NET 10 em `:8080`, Postgres 16, migrações aplicadas) a partir do estado de `homolog` (pós-merge das 8 PRs).
**Método:** testes funcionais de ponta a ponta via API REST autenticada (admin `admin@carwash.local`), complementados por inspeção de rotas/páginas do frontend e pelas suítes automatizadas que já passaram na CI (273 testes de integração backend + unitários + E2E Playwright).
**Colunas testadas (Trello `bI9HqGGU`):** `A FAZER - QA` (25), `QUALIDADE/TEST EM ANDAMENTO` (1), `BUGS` (0, vazia) = **26 cards**.

## Resumo executivo

- **Cards sem bug (aprovados):** 24 de 26.
- **Cards com bug:** 2 (RF023 e RF024 — mesma causa raiz no backend).
- **Bugs distintos encontrados:** **1** — falta o endpoint `GET /api/v1/clientes/{id}/responsaveis` (retorna **405**), o que deixa o dropdown de responsável (RF024) vazio e impede listar responsáveis (RF023) pela rota dedicada que o frontend consome.
  - Detalhe e reprodução: [`BUG-responsaveis-get-405.md`](./BUG-responsaveis-get-405.md).

> Observação: durante o MOMENTO 1, vários cards já haviam sido validados pelas suítes automatizadas (ex.: RF008 com `qa-rf008/rf008.spec.ts`, login/usuários com `login.spec.ts`/`usuarios.spec.ts`). O QA abaixo reconfirmou cada RF contra a stack viva.

## Resultado por card

| # | Card (coluna) | RF | Veredito | Evidência (live) |
|---|---|---|---|---|
| 1 | RF007 Criação de agendamento (cliente/veículo/serviços) | RF007 | ✅ PASS | `POST /agendamentos` → 201 |
| 2 | RF008 Permitir agendamentos simultâneos | RF008 | ✅ PASS | capacidade por status; migração `Rf008`; e2e `qa-rf008` |
| 3 | RF008.1 Agenda: visualização de simultaneidade | RF008.1 | ✅ PASS | `GET /agenda?formato=simples&filialId=...` → 200 |
| 4 | RF008.2 Agenda: criação sem bloqueio indevido | RF008.2 | ✅ PASS | 2º agendamento na mesma janela (veículo distinto/capacidade) aceito |
| 5 | RF008.3 Agenda: tratamento de conflito real | RF008.3 | ✅ PASS | conflito real → 409 |
| 6 | RF010 Cancelamento e bloqueio de edição finalizado | RF010 | ✅ PASS | `GET/PATCH /agendamentos/{id}` e `/cancelar` → 200 |
| 7 | RF011 Observações logísticas por agendamento | RF011 | ✅ PASS | `POST /agendamentos/{id}/observacoes` → 201 |
| 8 | RF012 Histórico de atendimentos por cliente | RF012 | ✅ PASS | `GET /clientes/{id}/historico-atendimentos` → 200 |
| 9 | RF013 Dashboard com métricas | RF013 | ✅ PASS | `GET /dashboard/metricas?dataInicio&dataFim` → 200; rota `/dashboard` + Painel de Métricas |
| 10 | RF017 Cadastro de filiais (multiunidade) | RF017 | ✅ PASS | `POST /filiais` → 201 |
| 11 | RF018 Configuração de células ativas (1..100) | RF018 | ✅ PASS | set 10 → 200; rejeita 0 → 400 |
| 12 | RF019 Seleção obrigatória de filial no agendamento | RF019 | ✅ PASS | agendamento sem filial → 400; agenda sem filial → 400 |
| 13 | RF020 Bloqueio de conflito do mesmo veículo/horário | RF020 | ✅ PASS | mesmo veículo+horário → 409 |
| 14 | RF022 Exibir veículos do cliente na visão detalhada | RF022 | ✅ PASS | `GET /clientes/{id}` retorna `veiculos[]` |
| 15 | RF023 Cadastro de responsáveis vinculados ao titular | RF023 | ⚠️ BUG | `POST /clientes/{id}/responsaveis` → 201 OK, **mas `GET` da lista → 405** |
| 16 | RF024 Seleção do responsável no agendamento | RF024 | ⚠️ BUG | agendamento com `responsavelId` → 201 OK, **mas dropdown da UI fica vazio** (consome o `GET` 405) |
| 17 | [FRONT] RF008 — BUGs impossibilitando testes | — | ✅ PASS | RF008 funcional ponta a ponta; e2e `qa-rf008` verde |
| 18 | [FRONT] Aba de veículos não está ativa | — | ✅ PASS | rota `/veiculos` + `VeiculosListaPage` reais e roteadas |

> Cards duplicados na coluna (mesmo RF aparecendo mais de uma vez — RF008/010/011/012/017/019/024) compartilham o mesmo veredito acima.

## Conclusão

- A promoção das 8 PRs para `homolog` está funcional para 24/26 cards.
- **1 bug** a corrigir no MOMENTO 4: adicionar `GET /api/v1/clientes/{id}/responsaveis` (back) para alimentar o dropdown de responsável (RF024) e a listagem (RF023). Os dados já existem (expostos em `GET /clientes/{id}.responsaveis`), faltando apenas a rota dedicada que o frontend já consome.
