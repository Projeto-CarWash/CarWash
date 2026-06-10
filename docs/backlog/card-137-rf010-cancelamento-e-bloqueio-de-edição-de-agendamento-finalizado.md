# Backlog — Card 137 / RF010 — Cancelamento e bloqueio de edição de agendamento finalizado

> Status: refinamento do analista. Pendente de decisão de CEO/Arquiteto nas lacunas L1–L4.
> Rastreabilidade base: P1 (agendamento desorganizado) → RF010 (Must) → UC006 → Módulo Agenda (DAT §4.1) / Serviço de Agenda (DAT §4 — Backend/API).
> Origem: o domínio (`Agendamento.cs`) já possui as invariáveis de cancelamento (`Cancelar`) e bloqueio de edição (`GarantirEstadoEditavel`), as exceções de aplicação (`CancelamentoStatusException`, `EdicaoBloqueadaException`) e o slice de cancelamento (`CancelarAgendamentoHandler`). O que falta é o slice de edição/reagendamento, o slice de finalização e a integração frontend — trilhos que este card completa.

## Resumo executivo

RF010 entrega duas capacidades complementares: (1) **cancelamento** de agendamento com motivo obrigatório e trilha de auditoria — o slice `Cancelar` já está implementado no backend (`PATCH /api/v1/agendamentos/{id}/cancelar`); (2) **bloqueio de edição** de agendamento com status `Finalizado`, `Cancelado` ou `EmAndamento` — a invariante `GarantirEstadoEditavel` já existe no domínio e a exceção `EdicaoBloqueadaException` já está mapeada, mas o slice de edição (`PATCH /api/v1/agendamentos/{id}`) e o slice de finalização (`PATCH /api/v1/agendamentos/{id}/finalizar`) ainda não foram implementados. Este card completa os slices faltantes, garante que o frontend consuma os endpoints existentes e novos, e cobre tudo com testes de integração Testcontainers. Há 4 lacunas que precisam de decisão antes do início (a mais crítica: o campo `origem` do cancelamento é string livre ou enum?).

## User stories

### US-137.1 — Cancelamento de agendamento com motivo obrigatório
**Como** Funcionário, **quero** cancelar um agendamento informando um motivo **para que** o registro permaneça na agenda com status "cancelado" e o histórico de operações seja preservado para auditoria. (Card: slice `Cancelar` já implementado; RF010; UC006; RN007.)

### US-137.2 — Bloqueio de edição de agendamento finalizado
**Como** Administrador, **quero** que agendamentos finalizados não possam ser editados **para que** o registro do atendimento permaneça íntegro como histórico e não seja alterado indevidamente. (Card: invariante `GarantirEstadoEditavel` + `EdicaoBloqueadaException`; RF010; RN004.)

### US-137.3 — Bloqueio de edição de agendamento cancelado ou em andamento
**Como** Funcionário, **quero** que o sistema impeça a edição de agendamentos cancelados ou em andamento **para que** a integridade operacional da agenda seja mantida — cancelamento é ato final, e alterações durante o atendimento seguem fluxo distinto. (Card: `EdicaoBloqueadaException`; RF010; RN004/RN006.)

### US-137.4 — Reagendamento de agendamento em status permitido
**Como** Funcionário, **quero** reagendar um agendamento que ainda está com status "agendado" **para que** eu ajuste a data/hora sem precisar cancelar e criar um novo do zero. (Card: slice `Reagendar`; RF010; RN006.)

### US-137.5 — Finalização de agendamento
**Como** Funcionário, **quero** finalizar um agendamento que está em andamento **para que** o atendimento seja registrado como concluído e o histórico fique disponível para consulta. (Card: slice `Finalizar`; RF010; UC006; RN004.)

### US-137.6 — Registro de histórico em todas as transições de status
**Como** Administrador, **quero** que toda mudança de status de agendamento gere um registro de histórico **para que** a trilha de auditoria (RN007) esteja completa — desde a criação até o cancelamento ou finalização. (Card: `AgendamentoHistorico` + `EventoHistorico`; RF010; RN007; RNF009.)

## Lacunas para decisão (CEO / Arquiteto)

- **L1 (bloqueante)** — Campo `origem` no cancelamento: o `CancelarAgendamentoRequest` atual usa `string? Origem` livre. O handler recebe qualquer valor. O domínio não valida nem persiste esse campo além do `Payload` do histórico. Decisão: criar um enum `OrigemCancelamento` (ex.: `USUARIO_INTERNO`, `SISTEMA`) com validação no validator, ou manter string livre? Default analista: **enum com validação** — evita lixo no payload de histórico e facilita filtros futuros na agenda.
- **L2 (bloqueante)** — Edição de agendamento: o endpoint `PATCH /api/v1/agendamentos/{id}` é listado no documento de endpoints (`endpoints-e-regras-de-negocio.md` §4.8) com label `RN004, RN006`. O escopo exato do payload de edição precisa ser definido: reagendamento (data/hora) apenas, ou também troca de serviços, cliente, veículo, filial, responsável e observações? Default analista: **reagendamento + observações + serviços** — cobre RN006 ("serviços podem ser alterados enquanto o agendamento não estiver finalizado") sem abrir demais campos que impactam RN011/RN020 (conflito de veículo) e RF019 (filial obrigatória). Troca de cliente/veículo/filial/responsável fica para versão futura (RF-FUT004).
- **L3** — Finalização de agendamento: o `Agendamento.Finalizar()` exige status `EmAndamento`. Porém, o slice `Iniciar` (transição `Agendado → EmAndamento`) também não está implementado. Decisão: este card implementa ambos (`Iniciar` + `Finalizar`) ou apenas `Finalizar` assumindo que o status será mudado manualmente no banco? Default analista: **implementar ambos** — `PATCH /api/v1/agendamentos/{id}/iniciar` e `PATCH /api/v1/agendamentos/{id}/finalizar` — a máquina de estados do agendamento fica completa.
- **L4** — Cancelamento de agendamento em andamento: o domínio hoje bloqueia (`Agendamento.Cancelar` lança `DomainException` quando `Status == EmAndamento`). O DRP §3 RF010 diz apenas "cancelamento de agendamento" sem distinguir status. Decisão: permitir cancelamento de agendamento em andamento (remover bloqueio) ou manter o bloqueio (em andamento precisa ser finalizado ou revertido para agendado antes de cancelar)? Default analista: **manter bloqueio** — agendamento em andamento indica serviço sendo executado; cancelar nesse momento exige fluxo de "estorno" mais complexo. Confirmar com o proprietário.

## Critérios de aceite (Given/When/Then)

1. **CA-137.1 — Cancelamento com sucesso (RF010):** Dado um agendamento com status `agendado`, quando enviar `PATCH /api/v1/agendamentos/{id}/cancelar` com `motivoCancelamento` válido (5–500 chars) e `origem` informada, então o sistema responde `200 OK` com envelope `{ message, data: { id, status: "cancelado", canceladoEm, canceladoPor, motivoCancelamento }, traceId }` e o agendamento permanece na agenda com status `cancelado`.
2. **CA-137.2 — Cancelamento de finalizado bloqueado (RF010, RN004):** Dado um agendamento com status `finalizado`, quando tentar cancelar, então o sistema responde `409 Conflict` com slug `agendamento-cancelamento-status` e mensagem "Agendamento finalizado não pode ser cancelado."
3. **CA-137.3 — Cancelamento de cancelado bloqueado (RF010):** Dado um agendamento com status `cancelado`, quando tentar cancelar novamente, então o sistema responde `409 Conflict` com mensagem "Agendamento já cancelado não pode ser cancelado novamente."
4. **CA-137.4 — Cancelamento de em andamento bloqueado (RF010, L4):** Dado um agendamento com status `em_andamento`, quando tentar cancelar, então o sistema responde `409 Conflict` com mensagem "Agendamento em andamento não pode ser cancelado." (pendente confirmação L4).
5. **CA-137.5 — Motivo obrigatório (RF010):** Dado um payload de cancelamento com `motivoCancelamento` vazio, ausente ou com menos de 5 caracteres, então o sistema responde `400 Bad Request` com erro de campo `motivoCancelamento`.
6. **CA-137.6 — Motivo com limite de tamanho (RF010):** Dado um payload com `motivoCancelamento` com mais de 500 caracteres, então o sistema responde `400 Bad Request`.
7. **CA-137.7 — Agendamento inexistente (RF010):** Dado um `id` que não existe na base, quando tentar cancelar, então o sistema responde `404 Not Found` com mensagem "Agendamento não encontrado."
8. **CA-137.8 — Edição de agendado com sucesso (RF010, RN006):** Dado um agendamento com status `agendado`, quando enviar `PATCH /api/v1/agendamentos/{id}` com payload de edição válido (inicio, fim, servicoIds, observacoes — escopo L2), então o sistema responde `200 OK` com envelope `{ message, data: { agendamento }, traceId }` atualizado e o histórico registra evento `EDITADO`.
9. **CA-137.9 — Edição de finalizado bloqueada (RF010, RN004):** Dado um agendamento com status `finalizado`, quando tentar editar, então o sistema responde `409 Conflict` com slug `agendamento-edicao-bloqueada` e mensagem "Agendamento finalizado não pode ser editado."
10. **CA-137.10 — Edição de cancelado bloqueada (RF010):** Dado um agendamento com status `cancelado`, quando tentar editar, então o sistema responde `409 Conflict` com mensagem "Agendamento cancelado não pode ser editado."
11. **CA-137.11 — Edição de em andamento bloqueada (RF010, RN006):** Dado um agendamento com status `em_andamento`, quando tentar editar, então o sistema responde `409 Conflict` com mensagem "Agendamento no status atual não permite edição."
12. **CA-137.12 — Concorrência otimista na edição (RF010):** Dado que duas requisições tentam editar o mesmo agendamento simultaneamente, quando a segunda enviar `versao` desatualizada, então o sistema responde `409 Conflict` (DbUpdateConcurrencyException → 409 via middleware).
13. **CA-137.13 — Revalidação de RN011 na edição (RF010, RN011):** Dado um reagendamento que resulta em conflito de horário para o mesmo veículo, quando a edição for tentada, então o sistema responde `409 Conflict` com slug `agendamento-conflito-veiculo`.
14. **CA-137.14 — Início de agendamento (RF010, L3):** Dado um agendamento com status `agendado`, quando enviar `PATCH /api/v1/agendamentos/{id}/iniciar`, então o sistema responde `200 OK` com status `em_andamento` e o histórico registra evento com status anterior/novo.
15. **CA-137.15 — Finalização de agendamento (RF010, L3):** Dado um agendamento com status `em_andamento`, quando enviar `PATCH /api/v1/agendamentos/{id}/finalizar`, então o sistema responde `200 OK` com status `finalizado` e o histórico registra evento `FINALIZADO`.
16. **CA-137.16 — Finalização de agendado bloqueada (RF010):** Dado um agendamento com status `agendado`, quando tentar finalizar, então o sistema responde `409 Conflict` com mensagem "Apenas agendamentos com status 'em_andamento' podem ser finalizados."
17. **CA-137.17 — Histórico de transições (RN007, RNF009):** Dado um agendamento que passou por criação → edição → início → finalização, quando consultar `GET /api/v1/agendamentos/{id}/historico`, então a resposta contém os eventos `CRIADO`, `EDITADO`, e `FINALIZADO` em ordem cronológica, cada um com `usuarioId` e `ocorridoEm`.
18. **CA-137.18 — Autorização (RNF003/RNF004):** Sem token JWT válido → `401 Unauthorized`. Todos os endpoints de cancelamento, edição, início e finalização herdam `RequireAuthorization()` do grupo.
19. **CA-137.19 — Auditoria (RNF009):** o handler registra `AuditLog` com evento, `entidadeId`, `usuarioId` e `dados` (status anterior/novo, motivo cancelamento, campos alterados) — mesmo padrão do `ConfirmarAgendamentoHandler`.
20. **CA-137.20 — Cobertura de testes de negócio (CA011 do DRP):** suite de testes de integração Testcontainers contra Postgres real cobre CA-137.1 a CA-137.18 com pelo menos 1 teste por CA crítico.

## Tarefas — trilha Backend (.NET)

| ID | Tarefa | Esforço | Dep. |
|----|--------|---------|------|
| BE-01 | Slice `Agendamentos/Iniciar/`: `IniciarAgendamentoCommand` (record, `ICommand<IniciarAgendamentoResponse>`) + `IniciarAgendamentoRequest` (DTO HTTP, body vazio ou `{}`) + `IniciarAgendamentoResponse` (`{ message, data: { id, status, atualizadoEm }, traceId }`) | PP | L3 |
| BE-02 | `IniciarAgendamentoCommandValidator`: `agendamentoId` preenchido (UUID). Regras de estado verificadas no handler/domínio. | PP | BE-01 |
| BE-03 | `IniciarAgendamentoHandler`: lookup por `ObterPorIdRastreadoAsync` → `agendamento.Iniciar()` (captura `DomainException` → mapeia para `ConflictException` com mensagem apropriada) → registra `AgendamentoHistorico` com `EventoHistorico.Editado` (ou novo evento `INICIADO` se L3 aprovar) → `SalvarAsync` com audit log | P | BE-02 |
| BE-04 | Slice `Agendamentos/Finalizar/`: `FinalizarAgendamentoCommand` + `FinalizarAgendamentoRequest` + `FinalizarAgendamentoResponse` (mesmo padrão de `Iniciar`) | PP | L3 |
| BE-05 | `FinalizarAgendamentoCommandValidator`: `agendamentoId` preenchido. | PP | BE-04 |
| BE-06 | `FinalizarAgendamentoHandler`: lookup → `agendamento.Finalizar()` (captura `DomainException` → `ConflictException`) → registra `AgendamentoHistorico` com `EventoHistorico.Finalizado` → `SalvarAsync` com audit log | P | BE-05 |
| BE-07 | Slice `Agendamentos/Editar/`: `EditarAgendamentoCommand` (record, `ICommand<EditarAgendamentoResponse>`) com campos: `AgendamentoId`, `Inicio`, `Fim`, `ServicoIds`, `Observacoes`, `Versao` (concurrency token), `TraceId`, `UsuarioId` | P | L2 |
| BE-08 | `EditarAgendamentoCommandValidator`: `agendamentoId` preenchido, `inicio < fim`, `inicio` no futuro, `servicoIds` ≥1 sem duplicatas, `observacoes` ≤500, `versao` > 0 | P | BE-07 |
| BE-09 | `EditarAgendamentoHandler`: lookup → `GarantirEstadoEditavel` (via `agendamento.Reagendar(inicio, fim)` — já chama `GarantirEstadoEditavel` internamente) → revalidar RN011 (`ExisteConflitoVeiculoAsync`) se `Inicio`/`Fim` mudaram → recalcular totais (`DefinirTotais`) se `ServicoIds` mudaram → registrar `AgendamentoHistorico` com `EventoHistorico.Editado` → `SalvarAsync` com audit log. Captura `DomainException` → mapeia para `EdicaoBloqueadaException` com `MotivoStatus`. | M | BE-07, BE-08 |
| BE-10 | Porta `IAgendamentoRepository`: adicionar `ObterItensPorAgendamentoIdAsync` para recomposição de itens na edição (se `ServicoIds` mudar) | PP | BE-09 |
| BE-11 | Implementação EF Core de `ObterItensPorAgendamentoIdAsync` em `AgendamentoRepository` | PP | BE-10 |
| BE-12 | Extensão do `AgendamentoRepository.SalvarAsync` para suportar eventos de edição/início/finalização (audit log parametrizável por evento, não hardcoded `"AGENDAMENTO_CANCELADO"`) | P | BE-03, BE-06, BE-09 |
| BE-13 | Endpoint `PATCH /api/v1/agendamentos/{id}/iniciar` em `AgendamentosEndpoints` — `.Produces<IniciarAgendamentoResponse>(200)`, `.ProducesProblem(400/401/404/409)` | PP | BE-03 |
| BE-14 | Endpoint `PATCH /api/v1/agendamentos/{id}/finalizar` em `AgendamentosEndpoints` — `.Produces<FinalizarAgendamentoResponse>(200)`, `.ProducesProblem(400/401/404/409)` | PP | BE-06 |
| BE-15 | Endpoint `PATCH /api/v1/agendamentos/{id}` em `AgendamentosEndpoints` — edição (reagendamento + serviços + observações). `.Produces<EditarAgendamentoResponse>(200)`, `.ProducesProblem(400/401/404/409)` | P | BE-09 |
| BE-16 | Ajustar `CancelarAgendamentoCommandValidator`: se L1 = enum, adicionar validação de `Origem` contra valores permitidos | PP | L1 |
| BE-17 | Logs estruturados (`ILogger`) com `TraceId`, `AgendamentoId`, `UsuarioId`, `StatusAnterior`, `StatusNovo` — RNF009 | PP | BE-13, BE-14, BE-15 |
| BE-18 | Testes de unidade do validator: `CancelarAgendamentoCommandValidator` (motivo <5, >500, vazio), `EditarAgendamentoCommandValidator` (inicio≥fim, servicos vazio, duplicado, versao 0) | P | BE-02, BE-08, BE-16 |
| BE-19 | Testes de integração (Testcontainers + Postgres): CA-137.1 a CA-137.18 — checklist CA-137.20 | M | BE-13, BE-14, BE-15 |

## Tarefas — trilha Frontend (React)

| ID | Tarefa | Esforço | Dep. |
|----|--------|---------|------|
| FE-01 | Adicionar em `agendamentoService.ts`: `cancelarAgendamento(id, payload)` chamando `PATCH /api/v1/agendamentos/{id}/cancelar`, `iniciarAgendamento(id)` chamando `PATCH .../iniciar`, `finalizarAgendamento(id)` chamando `PATCH .../finalizar`, `editarAgendamento(id, payload)` chamando `PATCH /api/v1/agendamentos/{id}` | P | BE-13, BE-14, BE-15 |
| FE-02 | Adicionar em `useAgendamentoQueries.ts`: mutations `useCancelarAgendamento()`, `useIniciarAgendamento()`, `useFinalizarAgendamento()`, `useEditarAgendamento()` — invalidação de cache da agenda e do agendamento específico ao finalizar | P | FE-01 |
| FE-03 | Componente `CancelarAgendamentoDialog`: modal com campo `motivoCancelamento` (textarea, min 5/max 500 chars), botão confirmar, loading state, erro de validação e erro 409 (status não permite) | P | FE-02 |
| FE-04 | Integração na `AgendaPage.tsx` (RF009): botões de ação por agendamento — "Iniciar", "Finalizar", "Cancelar", "Editar" — visibilidade condicional por status (`agendado` → iniciar/cancelar/editar; `em_andamento` → finalizar; `finalizado`/`cancelado` → nenhum) | M | FE-02, FE-03 |
| FE-05 | Modal/inline de edição de agendamento: formulário com data/hora, serviços e observações — reuso do schema Zod (`agendamentoSchema`) adaptado para edição | M | FE-02 |
| FE-06 | Tratamento de erros: mapear 409 `agendamento-edicao-bloqueada` e 409 `agendamento-cancelamento-status` para feedback visual (toast/banner); 409 `agendamento-conflito-veiculo` (RN011) reuso do padrão existente | P | FE-02 |
| FE-07 | Atualizar MSW handlers em `test/handlers.ts`: adicionar `PATCH /agendamentos/:id/cancelar`, `PATCH /agendamentos/:id/iniciar`, `PATCH /agendamentos/:id/finalizar`, `PATCH /agendamentos/:id` | P | FE-01 |
| FE-08 | Responsividade (RNF007) e tema claro/escuro (RNF010) nos novos componentes de diálogo/ação | P | FE-03, FE-05 |

## Definition of Ready

- RF010 vinculado; lacunas L1–L4 decididas (L1 e L2 são bloqueantes para BE; L3 bloqueia BE-01/BE-04; L4 bloqueia CA-137.4).
- Contrato HTTP de edição (escopo do payload) congelado após L2.
- Contrato de `origem` do cancelamento congelado após L1.
- Slice `Cancelar` existente revisado pelo dev responsável como referência de padrão.
- Critérios de aceite escritos e estimados.

## Definition of Done

- 3 endpoints implementados conforme contrato (`/iniciar`, `/finalizar`, `/{id}` edição); endpoint de cancelamento já existente revisado.
- 20 itens do checklist de implementação cobertos (CA-137.1 a CA-137.20).
- Testes de integração com Testcontainers contra Postgres real cobrindo CA-137.1 a CA-137.18.
- Validação server-side (RAT03), HTTPS (RNF004), respostas com `traceId`, logs estruturados (RNF009).
- Histórico de transições (`AgendamentoHistorico`) registrado em todas as mudanças de status — RN007.
- Concorrência otimista por `Versao` funcionando na edição — 409 em conflito de versão.
- Frontend: botões de ação na agenda com visibilidade condicional por status; diálogo de cancelamento com motivo; formulário de edição.
- Responsividade (RNF007) e tema claro/escuro (RNF010) nos novos componentes.
- Homologação com o proprietário (premissa A1 / CA005).

## Prioridade e estimativa

- **Prioridade:** Must (P1 → RF010 → CA001).
- **Esforço total:** M (BE) + M (FE) → ~1,5 sprint de 1 dev fullstack ou 1 sprint de 1 backend + 0,5 sprint de 1 frontend.
- **Dependências externas:** decisão L1/L2/L3/L4.
- **Bloqueia:** RF012 (histórico depende de finalização registrada), RF013 (dashboard depende de agendamentos finalizados para métricas).

## Rastreabilidade resumida

| Rastreável | ID |
|------------|----|
| Problema (DVP-E §4.1) | P1 |
| Requisito (DRP §3) | RF010 (Must) |
| Regra de negócio (DRP §4) | RN004, RN006, RN007, RN011 |
| Critério de aceite global (DRP §10) | CA001, CA011 |
| Risco mitigado (DAT §11) | RAT03 (validação server-side) |
| Módulo (DAT §4.1) | Agenda |
| ADR base | ADR 0003 (Minimal API + CQRS), ADR 0001 (UUID em app) |
