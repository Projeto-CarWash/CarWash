# Relatório de Bugs — Semana 31-06 (31 de maio a 06 de junho)

**Projeto:** CarWash  ·  **Período:** 31/05 a 06/06 de 2026  ·  **Gerado em:** 05/06/2026  ·  **Base:** template oficial `relatorio-de-bugs-template.md`

## 1. Objetivo

Registrar e rastrear os defeitos identificados durante testes, homologação e revisões do projeto CarWash nesta semana, facilitando priorização e acompanhamento das correções.

## 2. Resumo da semana

- **Total de bugs identificados:** 26 (13 cards de bug explícitos `[FRONT]`/`[BACK]` + 13 reprovações de RF em homologação)
- **Alta severidade (URGENTE):** 11 cards `[FRONT]` marcados *URGENTE* + reprovações de fluxo de agendamento
- **Em aberto ao fim do período:** ~15 (em *BUGS*, *CODEREVIEW* ou *BLOQUEADO*)  ·  **Já corrigidos/validados:** 5 RFs voltaram a *CONCLUIDO*
- **Reportados majoritariamente por:** Lucas Gabriel (QA, reprovações de RF) e Lucas Arruda (abertura dos cards `[FRONT]`)

## 3. Classificação (severidade e status)

| Severidade | Descrição |
|---|---|
| Crítica | Impede uso do fluxo principal ou falha grave de negócio |
| Alta | Afeta fortemente funcionalidade importante do MVP (cards marcados *URGENTE*) |
| Média | Afeta comportamento relevante, mas há contorno |
| Baixa | Impacto pequeno, visual ou pontual |

**Status:** Aberto · Em análise · Em correção · Corrigido · Validado · Rejeitado

## 4. Cards de bug explícitos abertos na semana (`[FRONT]` / `[BACK]`)

Lote criado em **03/06**, em sua maioria por Lucas Arruda, a partir da revisão da área de clientes/veículos.

| ID | Card | Módulo | Severidade | Status | Coluna atual | Responsável |
|---|---|---|---|---|---|---|
| BUG-136 | [FRONT] Não é possível editar um cliente — ação não existe no front | FRONTEND | Alta | Em correção | CODEREVIEW | Lucas Arruda |
| BUG-137 | [FRONT] Área de clientes não possui actions para editar/inativar no histórico | FRONTEND | Alta | Em correção | CODEREVIEW | Lucas Arruda |
| BUG-138 | [FRONT] Uso incorreto da API de veículos | FRONTEND | Alta | Em correção | CODEREVIEW | Lucas Arruda |
| BUG-139 | [FRONT] Cadastro de clientes — campo número da casa valida errado | FRONTEND | Alta | Em correção | CODEREVIEW | Lucas Arruda |
| BUG-140 | [BACK] 409 inválido no cadastro de clientes novos | BACKEND | Alta | Bloqueado | BLOQUEADO | — |
| BUG-141 | [FRONT] Cadastro de clientes — formulário não permite digitar | FRONTEND | Alta | Em correção | CODEREVIEW | Lucas Arruda |
| BUG-142 | [FRONT] Cadastro de clientes — remover funcionalidade "salvar rascunho" | FRONTEND | Alta | Em correção | CODEREVIEW | Lucas Arruda |
| BUG-143 | [FRONT] Usuários internos — alterar switch para padrão do projeto | FRONTEND | Alta | Em correção | CODEREVIEW | Lucas Arruda |
| BUG-144 | [FRONT] Usuários internos e clientes — botão de edição em vez de clicar no nome | FRONTEND | Alta | Em correção | CODEREVIEW | Lucas Arruda |
| BUG-145 | [FRONT] Aba de veículos não está ativa porém card consta como concluído | FRONTEND | Alta | Bloqueado | BLOQUEADO | Lucas Arruda |
| BUG-146 | [BACK] CRUD Veículos | BACKEND | Alta | Em correção | CODEREVIEW | matheus moreira |
| BUG-147 | [FRONT] Cadastro de cliente — campo renavam não mostra erro | FRONTEND | Alta | Em correção | CODEREVIEW | Lucas Arruda |
| BUG-148 | [FRONT] Cadastro de clientes — payload de veículos difere do esperado pelo backend | FRONTEND, INTERLIGADOS | Alta | Em correção | CODEREVIEW | Lucas Arruda |

## 5. Reprovações de homologação (RF reabertos para *BUGS* pelo QA)

Identificados por **Lucas Gabriel** ao mover de *QUALIDADE/TEST EM ANDAMENTO* / *A FAZER - QA* → *BUGS*.

| ID | Requisito | Reportado em | Severidade | Status / Coluna atual |
|---|---|---|---|---|
| BUG-149 | RF021 - Adicionar veículo no fluxo de cadastro | 31/05 | Média | Corrigido → CONCLUIDO |
| BUG-150 | RF006 - Catálogo de serviços | 31/05 | Média | Corrigido → CONCLUIDO |
| BUG-151 | RF004 - Cadastro de veículos vinculados a cliente | 31/05 | Média | Corrigido → CONCLUIDO |
| BUG-152 | RF022 - Exibir veículos do cliente na visualização detalhada | 31/05 | Média | Corrigido → CONCLUIDO |
| BUG-153 | RF010 - Cancelamento e bloqueio de edição de agendamento | 04/06 | Média | Em revisão → CODEREVIEW |
| BUG-154 | RF017 - Cadastro de filiais para operação multiunidade | 04/06 | Alta | Aberto → BUGS |
| BUG-155 | RF018 - Configuração de células ativas por filial | 04/06 | Alta | Aberto → BUGS |
| BUG-156 | RF019 - Seleção obrigatória de filial no agendamento | 04/06 | Alta | Aberto → BUGS |
| BUG-157 | RF020 - Bloqueio de conflito do mesmo veículo | 04/06 | Alta | Aberto → BUGS |
| BUG-158 | RF015 - Confirmação das informações antes de concluir agendamento | 04/06 | Alta | Aberto → BUGS |
| BUG-159 | RF007 - Criação de agendamento com cliente, veículo e serviços | 04/06 | Alta | Aberto → BUGS |
| BUG-160 | RF008 - Permitir agendamentos simultâneos no mesmo horário | 04/06 | Média | Retorno ao fluxo → A FAZER - QA |
| BUG-161 | RF005 - Validação de placa e bloqueio de duplicidade | 04/06 | Média | Retorno ao fluxo → A FAZER - QA |

## 6. Detalhamento (bugs de alta severidade em aberto)

### BUG-140 — [BACK] 409 inválido no cadastro de clientes novos

- **Labels:** — (backend)
- **Severidade:** Alta  ·  **Status:** Bloqueado
- **Identificado em:** 03/06 (movido para *BLOQUEADO* por Guilherme Brogio Macedo da Silva)
- **Coluna atual:** BLOQUEADO
- **Impacto:** o backend retorna HTTP 409 em cadastro de clientes novos, travando a integração com o `[FRONT] payload de veículos` (BUG-148).

### BUG-145 — [FRONT] Aba de veículos não está ativa porém card consta como concluído

- **Labels:** FRONTEND, URGENTE
- **Severidade:** Alta  ·  **Status:** Bloqueado
- **Identificado em:** 03/06 por Lucas Arruda
- **Coluna atual:** BLOQUEADO
- **Observação:** discrepância entre estado real da funcionalidade e o card marcado como concluído — risco de "falso verde" no quadro.

### BUG-154 a BUG-159 — Lote do módulo de filiais e agendamento (RF015, RF017–RF020, RF007)

- **Labels:** BACKEND / FRONTEND / INTERLIGADOS / URGENTE (conforme o RF)
- **Severidade:** Alta  ·  **Status:** Aberto (*BUGS*)
- **Identificado em:** 04/06 por Lucas Gabriel (QA)
- **Impacto:** o fluxo de **agendamento multiunidade** (filial + células + conflito de veículo) reprovou em bloco na homologação. É o principal foco de correção para a próxima semana, por concentrar regras de negócio críticas (RN011/CA006–CA008).

### BUG-148 — [FRONT] Payload de veículos difere do esperado pelo backend

- **Labels:** FRONTEND, INTERLIGADOS, URGENTE
- **Severidade:** Alta  ·  **Status:** Em correção
- **Identificado em:** 03/06 por Lucas Arruda
- **Coluna atual:** CODEREVIEW
- **Impacto:** contrato front↔back divergente no cadastro de veículos; relacionado ao BUG-140 (409 no backend).

## 7. Riscos prováveis a monitorar (referência do template)

| ID sugerido | Risco monitorado | Relação documental |
|---|---|---|
| BUG-001 | Agendamento salvo sem filial | CA007 |
| BUG-002 | Capacidade de filial aceita valor fora da faixa | CA008 |
| BUG-003 | Mesmo veículo agendado no mesmo horário em filiais diferentes | CA006 / RN011 |
| BUG-004 | Filiado não aparece no agendamento após cadastro | CA010 |
| BUG-005 | Agendamento finalizado permite edição | RN004 |

> A reprovação em bloco de RF017–RF020 nesta semana confirma os riscos **BUG-001/002/003** como prioritários — o módulo de filiais/células/conflito é onde se concentram as falhas.

## 8. Processo recomendado

1. Registrar o bug com o máximo de contexto. 2. Associar ao requisito/regra/CA. 3. Priorizar por severidade. 4. Atualizar status conforme a correção. 5. Registrar reteste e evidências.

## 9. Referências

- `plano-de-testes-mvp.md` · `drp.md` · `5-gdr.md` · `relatorio-de-bugs-template.md`

> **Nota de numeração:** IDs BUG-136+ dão sequência ao último ID da semana 24-30 (BUG-135). Cards `[FRONT]`/`[BACK]` correspondem aos cartões #165–#178 do quadro; reprovações de RF não têm card próprio (são o mesmo card do requisito movido para *BUGS*).
