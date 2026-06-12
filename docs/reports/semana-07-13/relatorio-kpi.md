# Relatório de KPIs — Semana 07-13 (07 a 13 de junho)

**Projeto:** CarWash Devs  ·  **Quadro:** Trello `Carwash devs`  ·  **Período:** 2026-06-07 a 2026-06-13  ·  **Gerado em:** 12/06/2026

> ℹ️ **Cobertura de dados:** o export traz 1.000 ações cobrindo **07/06 07:55 → 12/06 18:02** (horário de Brasília). Como o export é limitado a 1.000 ações, eventuais ações da madrugada de 07/06 podem ter ficado fora; 13/06 não está coberto (relatório gerado em 12/06). Movimentações, comentários, anexos e itens de checklist vêm do log de ações; tempos médios (lead time) usam criação do card (ID) → data de entrada em *CONCLUIDO*.
>
> ⚠️ **Artefatos descartados:** 10 eventos `createCard` com nome `pdnd:android-fallback` (12/06, 17:39–18:01) são artefatos do drag-and-drop do Trello gerados durante a movimentação em lote para *CONCLUIDO* e foram **excluídos** da contagem de cards criados.

## 1. Resumo executivo

| Indicador | Valor |
|---|---:|
| Itens de backlog concluídos (acum.) | **34 / 34 (100%)** 🏁 |
| Avanço líquido de conclusão na semana | +21 itens |
| Cards movidos para *CONCLUIDO* na semana | 46 (46 cards distintos, incl. cópias front/back e cards de bug) |
| Movimentações entre colunas | 175 |
| Cards criados na semana | 1 (`[FRONT] RF008 - BUGs impossibilitando testes`) |
| Reprovações de QA na semana (→ *BUGS*) | 23 eventos (12 itens distintos, alguns em 2–3 rodadas) |
| Lead time médio (criação→conclusão) | 12.53 dias |
| Lead time mediano | 15.62 dias |
| Integrantes ativos | 7 |

**Leitura da semana:** semana de **encerramento do quadro** — ao fim de 12/06 todas as colunas de trabalho (*A FAZER*, *EM DESENVOLVIMENTO*, *BLOQUEADO*, *CODEREVIEW*, *A FAZER - QA*, *QUALIDADE/TEST EM ANDAMENTO*, *BUGS*) estavam **vazias** e 100% do backlog deduplicado em *CONCLUIDO*. O ciclo foi: (1) em 07/06 os 10 cards de bug `[FRONT]` da semana anterior **passaram no reteste** e foram concluídos; (2) o QA (Lucas Gabriel) rodou rodadas contínuas de homologação (07, 08 e 10–11/06) reprovando 12 itens — quase todas as reprovações por **lacunas de API** (falta de `PATCH` em agendamentos/filiais/responsáveis, status não exposto, logs incompletos); (3) matheus moreira e Vinicius Tomazi corrigiram o backend e Lucas Arruda fechou RF011/RF012/RF020 no front; (4) em 10/06 e 12/06 Guilherme validou e moveu os lotes finais para *CONCLUIDO*, fechando com o DB001.

## 2. KPIs por integrante

| Integrante | Movim. | Iniciados | Concluídos | Comentários | Anexos | Checklist ✔ | Criados |
|---|---:|---:|---:|---:|---:|---:|---:|
| Guilherme Brogio Macedo da Silva | 94 | 0 | 39 | 15 | 5 | 288 | 1 |
| Lucas Gabriel | 47 | 0 | 7 | 21 | 24 | 133 | 0 |
| matheus moreira | 16 | 7 | 0 | 2 | 0 | 0 | 0 |
| Thiago Cezario Da Silva | 11 | 1 | 0 | 0 | 0 | 71 | 0 |
| Lucas Arruda | 6 | 3 | 0 | 0 | 0 | 12 | 0 |
| Antonio Neto | 1 | 0 | 0 | 0 | 0 | 0 | 0 |
| Vinicius Tomazi | 0 | 0 | 0 | 2 | 0 | 0 | 0 |

> **Iniciados** = cards movidos para *EM DESENVOLVIMENTO*. **Concluídos** = cards movidos para *CONCLUIDO* (atribuído a quem executou a ação). **Criados** = `createCard`/`copyCard` (descartados os artefatos `pdnd:android-fallback`).

**Destaques:**
- Maior nº de movimentações e conclusões — **Guilherme Brogio** (94 mov., 39 conclusões), responsável pelas validações em lote de 07/06, 10/06 e pelo fechamento final de 12/06; também liderou checklist (288 ✔).
- **Lucas Gabriel (QA)** sustentou a bateria final de homologação: 21 comentários e 24 anexos de evidência, 23 reprovações → *BUGS* e 7 aprovações diretas → *CONCLUIDO* (RF005, RF007, RF011, RF015, RF016, RF018, `[BACK] CRUD Veículos`).
- **matheus moreira** foi o principal corretor do backend: 7 inícios de desenvolvimento, todos sobre itens reprovados (RF008, RF010, RF024) — ciclo reprova→correção→reenvio em menos de 24h em todas as rodadas.
- **Lucas Arruda** fechou a frente front das pendências: RF011, RF012 e RF020 desenvolvidos e enviados a CODEREVIEW entre 08 e 09/06.
- **Vinicius Tomazi** atuou nas correções de RF012/RF020 via comentários e fixes diretos no GitHub (sem movimentar cards).

## 3. Tempo médio por card (lead time)

Considerando as **46 conclusões** da semana, o tempo médio entre a criação do card e a entrada em *CONCLUIDO* foi de **12.53 dias** (mediana 15.62). A média mistura dois grupos bem distintos: o lote de bugs `[FRONT]` criado em 03/06 fechou rápido (~3.8 dias), enquanto os RFs do módulo de agendamento/filiais, criados em 18/05 e 25/05, carregaram o ciclo completo de homologação (15–23 dias).

| Item concluído (grupos) | Criado | Concluído | Lead (dias) |
|---|---|---|---:|
| Lote de 10 bugs `[FRONT]` da área de clientes (reteste OK) | 03/06 | 07/06 | 3.66–3.81 |
| `[BACK]` CRUD Veículos | 03/06 | 07/06 | 4.06 |
| RF007 - Criação de agendamento (backend) | 18/05 | 07/06 | 19.92 |
| RF005 - Validação de placa e duplicidade | 18/05 | 07/06 | 20.01 |
| RF018 - Configuração de células ativas (backend) | 25/05 | 07/06 | 13.26 |
| RF015 - Confirmação antes de concluir agendamento | 18/05 | 08/06 | 20.35 |
| RF016 - Tema claro/escuro (1ª cópia) | 01/06 | 08/06 | 6.66 |
| `[FRONT]` RF008 - BUGs impossibilitando testes | 07/06 | 10/06 | 2.88 |
| RF013 / RF016 / RF023 / RF024 (cópias front) | 05/06 | 10/06 | 4.89 |
| RF008 + RF008.1/.2/.3 (agenda/simultaneidade, front) | 25/05 | 10/06 | 15.62–15.63 |
| RF010 / RF011 / RF012 / RF017 / RF018 / RF019 / RF020 (cópias front) | 25/05 | 10/06 | 15.62–15.63 |
| RF007 (Card Pai) / RF022 (front) | 18/05 | 10/06 | 22.75 |
| `[FRONT]` Aba de veículos inativa (ex-bloqueado) | 03/06 | 10/06 | 6.77 |
| Automação de testes do front (E2E + cobertura) — CA011 | 29/05 | 10/06 | 11.55 |
| RF011 - Observações logísticas (backend, pós-correção) | 25/05 | 11/06 | 16.29 |
| RF008 / RF010 / RF012 / RF017 / RF019 / RF020 / RF024 (backend, lote final) | 25/05–01/06 | 12/06 | 11.38–18.00 |
| RF013 - Dashboard (backend, lote final) | 01/06 | 12/06 | 11.38 |
| DB001 - Estruturação completa do banco de dados | 24/04 | 12/06 | **49.11** |

> O DB001 é o card mais antigo do quadro (24/04) e estava em *BLOQUEADO*; seu fechamento em 12/06 marcou o encerramento formal do backlog.

## 4. Distribuição por label/módulo

Movimentações ponderadas pelas labels dos cards trabalhados:

| Módulo/Label | Movimentações |
|---|---:|
| BACKEND | 77 |
| FRONTEND | 71 |
| INTERLIGADOS | 31 |
| URGENTE | 29 |
| BUG | 3 |
| BANCO DE DADOS | 1 |

**Foco por integrante (top labels):**

- **Guilherme Brogio Macedo da Silva:** FRONTEND (53), URGENTE (27), INTERLIGADOS (23), BACKEND (16) — validação dos lotes front + fechamento final.
- **Lucas Gabriel (QA):** BACKEND (44), INTERLIGADOS (4), FRONTEND (2) — homologação concentrada nos RFs de backend.
- **matheus moreira:** BACKEND (16) — correções de RF008/RF010/RF024.
- **Thiago Cezario Da Silva:** FRONTEND (10), INTERLIGADOS (4) — apoio no fluxo front e re-encaminhamento de lotes para QA.
- **Lucas Arruda:** FRONTEND (6) — RF011/RF012/RF020 front.
- **Antonio Neto:** BACKEND (1) — triagem do RF020 para *BLOQUEADO*.

## 5. Log de movimentações da semana

| Data | Integrante | Card | De → Para |
|---|---|---|---|
| 07/06 07:55 | Guilherme Brogio | Automação de testes do front — CA011 | EM DESENVOLVIMENTO → A FAZER |
| 07/06 07:56 | Guilherme Brogio | 10× bugs `[FRONT]` (lote da área de clientes) | A FAZER - QA → QUALIDADE/TEST EM ANDAMENTO |
| 07/06 09:01–09:03 | Guilherme Brogio | 10× bugs `[FRONT]` — reteste **PASSOU** | QUALIDADE/TEST EM ANDAMENTO → CONCLUIDO |
| 07/06 09:03 | Guilherme Brogio | RF008 + RF008.1/.2/.3 | A FAZER - QA → QUALIDADE/TEST EM ANDAMENTO |
| 07/06 11:37–11:39 | Guilherme Brogio | RF008 + RF008.1/.2/.3 — **FALHOU** (UI sem simultaneidade) | QUALIDADE/TEST EM ANDAMENTO → BUGS |
| 07/06 11:39–11:42 | Guilherme Brogio | RF017/RF018/RF019 p/ teste; RF007/RF022/`[FRONT]` aba veículos desbloqueados | A FAZER - QA / BLOQUEADO → fluxo de QA |
| 07/06 13:50 | Lucas Gabriel | RF007 - Criação de agendamento (backend) | QUALIDADE/TEST EM ANDAMENTO → CONCLUIDO |
| 07/06 14:33 | Lucas Gabriel | RF008 - Agendamentos simultâneos | QUALIDADE/TEST EM ANDAMENTO → BUGS |
| 07/06 15:12 | Thiago Cezario | RF010 - Cancelamento/bloqueio de edição | EM DESENVOLVIMENTO → A FAZER - QA |
| 07/06 15:14 | Thiago Cezario | RF016 - Tema claro/escuro | A FAZER → EM DESENVOLVIMENTO |
| 07/06 15:14–15:43 | Lucas Gabriel | RF010 — reprovado (endpoints ausentes) | QUALIDADE/TEST EM ANDAMENTO → BUGS |
| 07/06 15:44 | Lucas Gabriel | `[BACK]` CRUD Veículos | A FAZER - QA → CONCLUIDO |
| 07/06 16:02 | Lucas Gabriel | RF024 — reprovado (sem endpoint de vínculo) | QUALIDADE/TEST EM ANDAMENTO → BUGS |
| 07/06 16:06 | Lucas Gabriel | RF005 - Validação de placa | QUALIDADE/TEST EM ANDAMENTO → CONCLUIDO |
| 07/06 16:59–19:51 | matheus moreira | RF010 — correção e reenvio | BUGS → EM DESENVOLVIMENTO → A FAZER - QA |
| 07/06 19:57–23:06 | matheus moreira | RF024 — correção e reenvio | BUGS → EM DESENVOLVIMENTO → A FAZER - QA |
| 07/06 23:02 | Lucas Gabriel | RF012 — reprovado (sem filtro de ordem / log) | QUALIDADE/TEST EM ANDAMENTO → BUGS |
| 07/06 23:06–23:47 | Lucas Gabriel | RF024 e RF017/RF019 — reprovados | QUALIDADE/TEST EM ANDAMENTO → BUGS |
| 07/06 23:09 | matheus moreira | RF008 | BUGS → BLOQUEADO |
| 07/06 23:44 | Lucas Gabriel | RF018 - Células ativas (backend) | A FAZER - QA → CONCLUIDO |
| 08/06 00:22–00:27 | Lucas Gabriel | RF015 e RF016 | → CONCLUIDO |
| 08/06 00:45 | Lucas Gabriel | RF011 — reprovado (sem log de criação) | QUALIDADE/TEST EM ANDAMENTO → BUGS |
| 08/06 06:26–13:09 | matheus moreira | RF008 — correção e reenvio | BLOQUEADO → EM DESENVOLVIMENTO → A FAZER - QA |
| 08/06 13:10 | matheus moreira | RF024 — nova rodada de correção | BUGS → EM DESENVOLVIMENTO |
| 08/06 15:36–21:17 | Lucas Arruda | RF011 (front) — dev e codereview; RF012 iniciado | A FAZER → EM DESENVOLVIMENTO → CODEREVIEW |
| 08/06 20:22–20:23 | Lucas Gabriel | RF020 e RF010 — reprovados (sem `PATCH /agendamentos/{id}`) | QUALIDADE/TEST EM ANDAMENTO → BUGS |
| 08/06 20:37 | Thiago Cezario | RF008 + RF008.1/.2/.3 — retorno ao fluxo | BUGS → A FAZER - QA |
| 08/06 21:21 | Antonio Neto | RF020 | BUGS → BLOQUEADO |
| 09/06 22:12 | Thiago Cezario | RF013/RF023/RF024 (front) + `[FRONT]` RF008 bugs | A FAZER / BUGS → A FAZER - QA |
| 09/06 22:27–22:52 | Lucas Arruda | RF012 e RF020 (front) — dev e codereview | EM DESENVOLVIMENTO → CODEREVIEW |
| 10/06 00:44 | Guilherme Brogio | RF010/RF011/RF012/RF017/RF019/RF020 — liberados p/ QA | CODEREVIEW / BUGS → A FAZER - QA |
| 10/06 07:50 | Thiago Cezario | RF016 (front) | EM DESENVOLVIMENTO → A FAZER - QA |
| 10/06 08:36–08:43 | Guilherme Brogio | **Lote de 20 cards front validados** (RF007 Pai, RF008+sub, RF010–RF013, RF016–RF020, RF022–RF024, aba veículos, CA011) | A FAZER - QA → CONCLUIDO |
| 10/06 23:30–11/06 01:12 | Lucas Gabriel | RF012, RF020, RF017, RF010, RF019, RF008, RF024 — **rodada final de reprovações** (backend) | A FAZER - QA / QUALIDADE → BUGS |
| 11/06 00:31 | Lucas Gabriel | RF011 (backend) | QUALIDADE/TEST EM ANDAMENTO → CONCLUIDO |
| 11/06 01:03–17:48 | matheus moreira | RF008, RF010, RF024 — correções e reenvio | BUGS → EM DESENVOLVIMENTO → A FAZER - QA |
| 11/06 20:22 | Lucas Gabriel | RF013 — reprovado (cálculos sem validação) | QUALIDADE/TEST EM ANDAMENTO → BUGS |
| 12/06 17:39–17:40 | Guilherme Brogio | **Lote final backend:** RF008, RF010, RF012, RF013, RF017, RF019, RF020, RF024 | BUGS / A FAZER - QA → CONCLUIDO |
| 12/06 18:01 | Guilherme Brogio | DB001 - Estruturação completa do banco de dados | BLOQUEADO → CONCLUIDO 🏁 |

> Log resumido: agrupei movimentações simultâneas de lote (mesmo horário/integrante) em linhas únicas. Total de 175 movimentações no período.

## 6. Metodologia e limitações

- **Fonte:** export JSON do quadro Trello `Carwash devs` (`bI9HqGGU`), gerado em 12/06/2026.
- **Fuso:** todos os horários convertidos para horário de Brasília (UTC−3).
- **Escopo de backlog (34 itens):** deduplicação por referência de requisito (RF) + features nomeadas (DB001, CA011, subcards RF007.x/RF008.x e ajustes nomeados); descartadas cópias arquivadas (listas fechadas), cards de instrução e artefatos `pdnd:android-fallback`.
- **Movimentações / comentários / anexos / checklist:** derivados do log de ações (07/06–12/06), atribuídos ao autor da ação.
- **Lead time:** entrada em *CONCLUIDO* − criação do card (timestamp do ID); para cards reconcluidos, vale a última entrada.
- **Iniciados:** movidos para *EM DESENVOLVIMENTO*. **Concluídos:** movidos para *CONCLUIDO*.
- **13/06 não coberto:** relatório gerado em 12/06 à noite; como o quadro foi zerado em 12/06 às 18:01, não são esperadas novas movimentações.
