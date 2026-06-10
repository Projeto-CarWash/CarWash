# Relatório de KPIs — Semana 31-06 (31 de maio a 06 de junho)

**Projeto:** CarWash Devs  ·  **Quadro:** Trello `Carwash devs`  ·  **Período:** 2026-05-31 a 2026-06-06  ·  **Gerado em:** 05/06/2026

> ℹ️ **Cobertura de dados:** completa para a semana. O export traz 1.000 ações cobrindo **28/05 → 05/06**, então todo o intervalo 31/05–06/06 está coberto (06/06 ainda em curso na geração). Movimentações, comentários, anexos e itens de checklist vêm do log de ações; tempos médios (lead time) usam criação do card (ID) → data de entrada em *CONCLUIDO*.
>
> ⚠️ **Escopo de backlog:** o quadro contém cópias arquivadas (listas fechadas *Backend*/*Frontend*/*Sprint*) e cards de teste (ex.: `55953`, `sda`, `jjj`). As métricas de progresso usam o **backlog deduplicado de 34 itens** (requisitos RF + features nomeadas), descartando duplicatas e lixo. Métricas de atividade (movimentações, comentários etc.) contam todas as ações reais do período.

## 1. Resumo executivo

| Indicador | Valor |
|---|---:|
| Itens de backlog concluídos (acum.) | 13 / 34 (38.2%) |
| Avanço líquido de conclusão na semana | +3 itens |
| Cards movidos para *CONCLUIDO* na semana | 6 eventos (5 itens; 2 reabertos pelo QA) |
| Movimentações entre colunas | 154 |
| Cards/cópias criados na semana | 24 (inclui 13 cards de bug `[FRONT]`/`[BACK]`) |
| Lead time médio (criação→conclusão) | 13.85 dias |
| Lead time mediano | 16.42 dias |
| Integrantes ativos | 7 |

**Leitura da semana:** semana dominada por **QA + correção de bugs de frontend**. O QA (Lucas Gabriel) varreu o lote de RFs em homologação e reprovou vários (RF005, RF007, RF008, RF015, RF017–RF020), enquanto Lucas Arruda abriu e tratou 13 cards de bug de frontend (`[FRONT]`). Em paralelo, a frente de backend (matheus, Vinicius) avançou RF010–RF013, RF016 e RF023/RF024.

## 2. KPIs por integrante

| Integrante | Movim. | Iniciados | Concluídos | Comentários | Anexos | Checklist ✔ | Criados |
|---|---:|---:|---:|---:|---:|---:|---:|
| Lucas Arruda | 39 | 17 | 0 | 0 | 0 | 42 | 0 |
| Lucas Gabriel | 37 | 0 | 2 | 21 | 27 | 55 | 0 |
| matheus moreira | 25 | 7 | 0 | 7 | 0 | 49 | 0 |
| Guilherme Brogio Macedo da Silva | 22 | 1 | 4 | 3 | 14 | 53 | 13 |
| Thiago Cezario Da Silva | 19 | 10 | 0 | 0 | 0 | 25 | 0 |
| Vinicius Tomazi | 6 | 4 | 0 | 0 | 0 | 27 | 0 |
| Antonio Neto | 6 | 0 | 0 | 0 | 0 | 3 | 11 |

> **Iniciados** = cards movidos para *EM DESENVOLVIMENTO*. **Concluídos** = cards movidos para *CONCLUIDO* (atribuído a quem executou a ação). **Criados** = `createCard`/`copyCard`.

**Destaques:**
- Maior nº de movimentações — **Lucas Arruda** (39), puxado pela criação/tratamento do lote de bugs de frontend.
- Mais itens de checklist concluídos — **Lucas Gabriel** (55), refletindo a bateria de homologação.
- Mais comentários e anexos — **Lucas Gabriel** (21 comentários, 27 anexos): evidências de teste e reprovações.
- **Antonio Neto** retoma atividade (11 cards criados/copiados + reorganização de cards saídos da *REUNIÃO*).

## 3. Tempo médio por card (lead time)

Considerando os **6 eventos** de conclusão (5 itens distintos) com data nesta semana, o tempo médio entre a criação do card e a entrada em *CONCLUIDO* foi de **13.85 dias** (mediana 16.42 dias). O valor é alto porque a maioria dos itens entregues (RF004, RF006, RF021, RF022) foi **criada em 18/05** e só fechou após o ciclo de homologação.

| Item concluído | Criado | Concluído | Lead (dias) |
|---|---|---|---:|
| RF023 - Cadastro de responsáveis vinculados ao cliente | 01/06 | 04/06 | 3.11 |
| RF021 - Adicionar veículo no fluxo de cadastro (1º fechamento) | 18/05 | 01/06 | 14.22 |
| RF022 - Exibir veículos do cliente na visualização detalhada | 18/05 | 04/06 | 16.40 |
| RF004 - Cadastro de veículos vinculados a cliente existente | 18/05 | 04/06 | 16.45 |
| RF006 - Catálogo de serviços com tipo, preço e duração | 18/05 | 04/06 | 16.45 |
| RF021 - Adicionar veículo no fluxo de cadastro (reabertura→fechamento) | 18/05 | 04/06 | 16.45 |

## 4. Distribuição por label/módulo

Movimentações ponderadas pelas labels dos cards trabalhados:

| Módulo/Label | Movimentações |
|---|---:|
| BACKEND | 76 |
| FRONTEND | 47 |
| URGENTE | 46 |
| INTERLIGADOS | 32 |
| BUG | 14 |

**Foco por integrante (top labels):**

- **Lucas Arruda:** URGENTE (28), FRONTEND (10), INTERLIGADOS (6) — frente de bugs de frontend.
- **Lucas Gabriel (QA):** BACKEND (31), INTERLIGADOS (6), FRONTEND (6), BUG (5).
- **matheus moreira:** BACKEND (23), INTERLIGADOS (7), URGENTE (2).
- **Guilherme Brogio Macedo da Silva:** BACKEND (12), FRONTEND (9), URGENTE (5), INTERLIGADOS (3).
- **Thiago Cezario Da Silva:** FRONTEND (17), INTERLIGADOS (10), BUG (6), URGENTE (6).
- **Vinicius Tomazi:** BACKEND (5), FRONTEND (1).
- **Antonio Neto:** FRONTEND (4), BACKEND (2).

## 5. Log de movimentações da semana

| Data | Integrante | Card | De → Para |
|---|---|---|---|
| 31/05 00:58 | Lucas Gabriel | RF021 - Adicionar veículo no fluxo de cadastro | A FAZER - QA → BUGS |
| 31/05 01:00 | Lucas Gabriel | RF006 - Catálogo de serviços | A FAZER - QA → QUALIDADE/TEST EM ANDAMENTO |
| 31/05 01:04 | Lucas Gabriel | RF006 - Catálogo de serviços | QUALIDADE/TEST EM ANDAMENTO → BUGS |
| 31/05 01:04 | Lucas Gabriel | RF004 - Cadastro de veículos | A FAZER - QA → QUALIDADE/TEST EM ANDAMENTO |
| 31/05 01:17 | Lucas Gabriel | RF004 - Cadastro de veículos | QUALIDADE/TEST EM ANDAMENTO → BUGS |
| 31/05 01:17 | Lucas Gabriel | RF022 - Exibir veículos do cliente | A FAZER - QA → QUALIDADE/TEST EM ANDAMENTO |
| 31/05 16:30 | matheus moreira | RF010 - Cancelamento e bloqueio de edição | EM DESENVOLVIMENTO → CODEREVIEW |
| 31/05 21:55 | Thiago Cezario Da Silva | RF004 - Cadastro de veículos | BUGS → EM DESENVOLVIMENTO |
| 31/05 21:55 | Thiago Cezario Da Silva | RF006 - Catálogo de serviços | BUGS → EM DESENVOLVIMENTO |
| 31/05 21:55 | Thiago Cezario Da Silva | RF021 - Adicionar veículo no fluxo de cadastro | BUGS → EM DESENVOLVIMENTO |
| 31/05 21:55 | Thiago Cezario Da Silva | RF022 - Exibir veículos do cliente | BUGS → EM DESENVOLVIMENTO |
| 31/05 22:26 | Lucas Gabriel | RF022 - Exibir veículos do cliente | QUALIDADE/TEST EM ANDAMENTO → BUGS |
| 01/06 00:34 | matheus moreira | RF005 - Validação de placa e duplicidade | EM DESENVOLVIMENTO → A FAZER - QA |
| 01/06 08:39 | Antonio Neto | RF016 - Alternância entre tema claro e escuro | REUNIÃO → A FAZER |
| 01/06 09:44 | Guilherme Brogio Macedo da Silva | RF008/010/011/012/017/018/019/020 (lote) | CODEREVIEW → A FAZER - QA |
| 01/06 09:44 | Guilherme Brogio Macedo da Silva | RF007 / RF015 | BLOQUEADO → A FAZER - QA |
| 01/06 17:00 | Vinicius Tomazi | RF013 - Dashboard com métricas operacionais | A FAZER → EM DESENVOLVIMENTO |
| 01/06 18:33 | Lucas Arruda | RF019 - Seleção obrigatória de filial | A FAZER → EM DESENVOLVIMENTO |
| 01/06 20:21 | matheus moreira | RF023 - Cadastro de responsáveis | A FAZER → EM DESENVOLVIMENTO |
| 01/06 21:05 | Lucas Gabriel | RF021 - Adicionar veículo no fluxo de cadastro | BUGS → CONCLUIDO |
| 01/06 21:39 | Lucas Gabriel | RF007 - Criação de agendamento | A FAZER - QA → QUALIDADE/TEST EM ANDAMENTO |
| 02/06 06:28 | matheus moreira | RF023 - Cadastro de responsáveis | EM DESENVOLVIMENTO → CODEREVIEW |
| 02/06 17:03 | Thiago Cezario Da Silva | RF022 / RF021 | EM DESENVOLVIMENTO → A FAZER - QA |
| 02/06 17:04 | Thiago Cezario Da Silva | RF006 / RF004 | EM DESENVOLVIMENTO → A FAZER - QA |
| 03/06 06:14 | Guilherme Brogio Macedo da Silva | RF019 / RF013 | EM DESENVOLVIMENTO → A FAZER |
| 03/06 17:02 | Guilherme Brogio Macedo da Silva | [BACK] 409 inválido no cadastro de clientes | A FAZER → BLOQUEADO |
| 03/06 18:12–22:47 | Lucas Arruda | 10× cards `[FRONT]` de bug | A FAZER → EM DESENVOLVIMENTO → CODEREVIEW |
| 04/06 01:24 | Guilherme Brogio Macedo da Silva | RF021 / RF006 / RF004 | A FAZER - QA → CONCLUIDO |
| 04/06 01:27 | Guilherme Brogio Macedo da Silva | RF022 - Exibir veículos do cliente | BUGS → CONCLUIDO |
| 04/06 01:29 | Guilherme Brogio Macedo da Silva | RF007 - Criação de agendamento | A FAZER - QA → BLOQUEADO |
| 04/06 09:27 | matheus moreira | [BACK] CRUD Veículos | A FAZER → EM DESENVOLVIMENTO |
| 04/06 10:26 | Lucas Gabriel | RF010 - Cancelamento e bloqueio de edição | QUALIDADE/TEST EM ANDAMENTO → BUGS |
| 04/06 10:37 | Lucas Gabriel | RF017 - Cadastro de filiais | A FAZER - QA → BUGS |
| 04/06 11:14 | Lucas Gabriel | RF023 - Cadastro de responsáveis | QUALIDADE/TEST EM ANDAMENTO → CONCLUIDO |
| 04/06 11:31 | Lucas Gabriel | RF008 - Agendamentos simultâneos | QUALIDADE/TEST EM ANDAMENTO → BUGS |
| 04/06 11:50 | Lucas Gabriel | RF005 - Validação de placa | QUALIDADE/TEST EM ANDAMENTO → BUGS |
| 04/06 12:21 | Lucas Gabriel | RF018 - Configuração de células ativas | QUALIDADE/TEST EM ANDAMENTO → BUGS |
| 04/06 12:36 | Lucas Gabriel | RF015 - Confirmação das informações | QUALIDADE/TEST EM ANDAMENTO → BUGS |
| 04/06 12:37 | Lucas Gabriel | RF007 - Criação de agendamento | QUALIDADE/TEST EM ANDAMENTO → BUGS |
| 04/06 12:45 | Lucas Gabriel | RF019 - Seleção obrigatória de filial | QUALIDADE/TEST EM ANDAMENTO → BUGS |
| 04/06 13:06 | Lucas Gabriel | RF020 - Bloqueio de conflito do mesmo veículo | QUALIDADE/TEST EM ANDAMENTO → BUGS |
| 04/06 13:08 | matheus moreira | RF005 - Validação de placa | EM DESENVOLVIMENTO → A FAZER - QA |
| 04/06 14:26–14:41 | Lucas Arruda | RF017 / RF018 / RF019 | A FAZER → EM DESENVOLVIMENTO |
| 04/06 15:02 | Thiago Cezario Da Silva | RF008 / RF008.1 / RF008.2 / RF008.3 | A FAZER → EM DESENVOLVIMENTO |
| 04/06 19:53 | Thiago Cezario Da Silva | RF008 + subtarefas | EM DESENVOLVIMENTO → CODEREVIEW |
| 04/06 19:56 | Vinicius Tomazi | RF013 - Dashboard | EM DESENVOLVIMENTO → CODEREVIEW |
| 04/06 20:32–21:15 | Lucas Arruda | RF019 / RF017 / RF018 | EM DESENVOLVIMENTO → CODEREVIEW |
| 04/06 22:33 | Vinicius Tomazi | RF016 - Alternância tema claro/escuro | A FAZER → EM DESENVOLVIMENTO |
| 04/06 23:54 | Vinicius Tomazi | RF016 - Alternância tema claro/escuro | EM DESENVOLVIMENTO → CODEREVIEW |
| 05/06 02:50 | matheus moreira | RF008 - Agendamentos simultâneos | EM DESENVOLVIMENTO → A FAZER - QA |
| 05/06 11:14 | Antonio Neto | RF013 / RF016 / RF023 / RF024 | REUNIÃO → A FAZER |
| 05/06 14:17 | matheus moreira | RF010 - Cancelamento e bloqueio de edição | EM DESENVOLVIMENTO → CODEREVIEW |
| 05/06 14:19 | matheus moreira | RF013 / RF016 | CODEREVIEW → A FAZER - QA |
| 05/06 18:38 | Guilherme Brogio Macedo da Silva | Automação de testes do front (E2E + cobertura) — CA011 | A FAZER → EM DESENVOLVIMENTO |

> Log resumido: agrupei movimentações simultâneas de lote (mesmo horário/integrante) em linhas únicas. Total de 154 movimentações no período.

## 6. Metodologia e limitações

- **Fonte:** export JSON do quadro Trello `Carwash devs` (`bI9HqGGU`), gerado em 05/06/2026.
- **Fuso:** todos os horários convertidos para horário de Brasília (UTC−3).
- **Escopo de backlog (34 itens):** deduplicação por referência de requisito (RF) + features nomeadas; descartadas cópias arquivadas (listas fechadas), cards de instrução e cards de teste/lixo.
- **Movimentações / comentários / anexos / checklist:** derivados do log de ações (28/05–05/06), atribuídos ao autor da ação.
- **Lead time:** entrada em *CONCLUIDO* − criação do card (timestamp do ID).
- **Iniciados:** movidos para *EM DESENVOLVIMENTO*. **Concluídos:** movidos para *CONCLUIDO*.
