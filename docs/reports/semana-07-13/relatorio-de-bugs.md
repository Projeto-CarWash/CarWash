# Relatório de Bugs — Semana 07-13 (07 a 13 de junho)

**Projeto:** CarWash  ·  **Período:** 07/06 a 13/06 de 2026  ·  **Gerado em:** 12/06/2026  ·  **Base:** template oficial `relatorio-de-bugs-template.md`

## 1. Objetivo

Registrar e rastrear os defeitos identificados durante testes, homologação e revisões do projeto CarWash nesta semana, facilitando priorização e acompanhamento das correções.

## 2. Resumo da semana

- **Total de bugs identificados:** 13 (12 reprovações distintas de RF em homologação + 1 card de bug explícito `[FRONT] RF008`)
- **Eventos de reprovação:** 23 movimentos → *BUGS* (vários itens reprovados em 2–3 rodadas de reteste)
- **Em aberto ao fim do período:** **0** — quadro 100% zerado em 12/06 🏁
- **Retestes da semana anterior:** os 13 bugs `[FRONT]`/`[BACK]` abertos em 03/06 (BUG-136 a BUG-148) foram **todos validados e fechados**; BUG-140 foi arquivado
- **Reportados majoritariamente por:** Lucas Gabriel (QA, homologação de backend via API/Swagger) e Guilherme Brogio (reprovação do lote RF008 de agenda)
- **Padrão dominante:** lacunas de **contrato de API** — endpoints `PATCH` ausentes (agendamentos, filiais, responsáveis), status não exposto em `GET`, logs de auditoria incompletos

## 3. Classificação (severidade e status)

| Severidade | Descrição |
|---|---|
| Crítica | Impede uso do fluxo principal ou falha grave de negócio |
| Alta | Afeta fortemente funcionalidade importante do MVP (cards marcados *URGENTE*) |
| Média | Afeta comportamento relevante, mas há contorno |
| Baixa | Impacto pequeno, visual ou pontual |

**Status:** Aberto · Em análise · Em correção · Corrigido · Validado · Rejeitado

## 4. Bugs identificados na semana

Numeração dá sequência ao BUG-161 (última ID da semana 31-06).

| ID | Item | Módulo | Severidade | Reportado em | Status final | Coluna atual |
|---|---|---|---|---|---|---|
| BUG-162 | `[FRONT]` RF008 - BUGs impossibilitando testes (card de bloqueio criado em *BUGS*) | FRONTEND | Alta | 07/06 | Validado | CONCLUIDO (10/06) |
| BUG-163 | RF008 - Agendamentos simultâneos: status não visível em `GET`, sem endpoint de atualização; não cria agendamento para o dia atual | BACKEND | Alta | 07/06 (re-reprovado 11/06) | Validado | CONCLUIDO (12/06) |
| BUG-164 | RF008.1 - Agenda: visualização de simultaneidade por horário ausente na UI | FRONTEND | Alta | 07/06 | Validado | CONCLUIDO (10/06) |
| BUG-165 | RF008.2 - Agenda: criação com bloqueio indevido | FRONTEND | Alta | 07/06 | Validado | CONCLUIDO (10/06) |
| BUG-166 | RF008.3 - Agenda: tratamento de conflito real (409) não refletido na UI | FRONTEND | Alta | 07/06 | Validado | CONCLUIDO (10/06) |
| BUG-167 | RF010 - Cancelamento/edição: endpoints de edição e consulta detalhada ausentes no Swagger | BACKEND | Alta | 07/06 (re-reprovado 08 e 11/06) | Validado | CONCLUIDO (12/06) |
| BUG-168 | RF024 - Responsáveis: sem endpoint para alterar vínculo cliente↔responsável | BACKEND | Média | 07/06 (re-reprovado 11/06) | Validado | CONCLUIDO (12/06) |
| BUG-169 | RF012 - Histórico: sem filtro de ordenação; log registra apenas sucessos | BACKEND | Média | 07/06 (re-reprovado 10/06) | Validado | CONCLUIDO (12/06) |
| BUG-170 | RF017 - Filiais: falta endpoint `PATCH` para inativar filial | BACKEND | Alta | 07/06 (re-reprovado 11/06) | Validado | CONCLUIDO (12/06) |
| BUG-171 | RF019 - Filial no agendamento: impossível validar filial inativa (depende do BUG-170) | BACKEND | Alta | 07/06 (re-reprovado 11/06) | Validado | CONCLUIDO (12/06) |
| BUG-172 | RF011 - Observações logísticas: criação não gera log de auditoria | BACKEND | Média | 08/06 | Validado | CONCLUIDO (11/06) |
| BUG-173 | RF020 - Conflito de veículo: sem `PATCH /api/v1/agendamentos/{id}`, checklist de edição inválido; depois `PATCH` retornava erro | BACKEND | Alta | 08/06 (re-reprovado 11/06) | Validado | CONCLUIDO (12/06) |
| BUG-174 | RF013 - Dashboard: cálculos não validáveis (sem massa/log para conferência) | BACKEND | Média | 11/06 | Validado | CONCLUIDO (12/06) |

## 5. Reteste dos bugs da semana anterior (BUG-136 a BUG-161)

Reteste executado em **07/06** por Guilherme Brogio, com evidência de "PASSOU ✅" comentada card a card:

| ID anterior | Card | Resultado do reteste |
|---|---|---|
| BUG-136 a BUG-139, BUG-141 a BUG-144, BUG-147, BUG-148 | Lote de 10 cards `[FRONT]` da área de clientes/veículos | **PASSOU** — concluídos em 07/06 |
| BUG-140 | `[BACK]` 409 inválido no cadastro de clientes novos | **Arquivado** em *BLOQUEADO* (03/06, sem atividade posterior) — problema absorvido pelas correções do CRUD de veículos/payload |
| BUG-145 | `[FRONT]` Aba de veículos inativa com card "concluído" | **PASSOU** — desbloqueado em 07/06 e concluído em 10/06 |
| BUG-146 | `[BACK]` CRUD Veículos | **PASSOU** — concluído em 07/06 |
| BUG-149 a BUG-161 | Reprovações de RF da semana anterior | Todos os RFs reentraram no fluxo e fecharam até 12/06 (ver §4 para os que reprovaram de novo) |

## 6. Detalhamento (bugs de alta severidade da semana)

### BUG-163 — RF008: status de agendamento invisível e bloqueio do dia atual

- **Severidade:** Alta  ·  **Status:** Validado
- **Identificado em:** 07/06 por Lucas Gabriel (QA); re-reprovado em 11/06
- **Evidências:** "Agendamento não tem status visível em GET, nem endpoint para atualização de status"; "Não é possível criar agendamento para dia atual (presente), apenas o seguinte"; "Os agendamentos criados não persistem tempo suficiente para os testes"
- **Correção:** matheus moreira (3 ciclos: 07–08/06 e 11/06); validado e concluído em 12/06.
- **Impacto:** bloqueava a validação ponta a ponta do fluxo de agendamentos simultâneos (CA006/RN011).

### BUG-164 a BUG-166 — Lote RF008.x: UI da agenda sem simultaneidade

- **Severidade:** Alta  ·  **Status:** Validado
- **Identificado em:** 07/06 por Guilherme Brogio, com matriz de critérios de aceite comentada card a card (todos **FALHOU**)
- **Observação:** backend OK, mas a UI não exibia simultaneidade por horário nem tratava conflito real/409 — originou o card de bloqueio BUG-162.
- **Correção:** frente front (Thiago/Lucas Arruda); validados em 10/06.

### BUG-167 / BUG-173 — Falta de `PATCH /api/v1/agendamentos/{id}` (RF010/RF020)

- **Severidade:** Alta  ·  **Status:** Validado
- **Identificado em:** 07–08/06 por Lucas Gabriel
- **Evidências:** "Como **não existe PATCH** `/api/v1/agendamentos/{id}`, vários itens do checklist **não podem ser validados**: editar horário para faixa sem conflito / com conflito…"; na rodada de 11/06 o `PATCH` recém-criado retornava erro.
- **Correção:** matheus moreira (backend) e Vinicius Tomazi (fix de RF020 enviado via GitHub em 12/06); validados em 12/06.
- **Impacto:** regras de bloqueio de edição (RN004) e de conflito de veículo (RN011/CA006) sem possibilidade de teste via API.

### BUG-170 / BUG-171 — Inativação de filial sem endpoint (RF017/RF019)

- **Severidade:** Alta  ·  **Status:** Validado
- **Identificado em:** 07/06 por Lucas Gabriel; re-reprovados em 11/06
- **Evidências:** "Falta endpoint de patch para inativar [filial]"; "Não é possível validar agendamento com filial inativa porque não há endpoint para atualização de status da filial"
- **Impacto:** CA007 (agendamento exige filial válida/ativa) não verificável; bugs encadeados — BUG-171 dependia da correção do BUG-170.
- **Correção:** backend; validados e concluídos em 12/06.

### BUG-168 — RF024: vínculo cliente↔responsável sem endpoint de atualização

- **Severidade:** Média  ·  **Status:** Validado
- **Identificado em:** 07/06 por Lucas Gabriel, com encaminhamento explícito: "Validar com PO se é necessário desenvolver ou tirar do escopo do projeto"
- **Resolução:** mantido no escopo; matheus moreira implementou (2 ciclos), validado em 12/06.

## 7. Riscos prováveis a monitorar (referência do template)

| ID sugerido | Risco monitorado | Relação documental | Situação ao fim da semana |
|---|---|---|---|
| BUG-001 | Agendamento salvo sem filial | CA007 | Coberto — RF019 validado em 12/06 |
| BUG-002 | Capacidade de filial aceita valor fora da faixa | CA008 | Coberto — RF018 validado em 07/06 |
| BUG-003 | Mesmo veículo agendado no mesmo horário em filiais diferentes | CA006 / RN011 | Coberto — RF020 validado em 12/06 |
| BUG-004 | Filiado não aparece no agendamento após cadastro | CA010 | Coberto — RF023/RF024 validados em 10–12/06 |
| BUG-005 | Agendamento finalizado permite edição | RN004 | Coberto — RF010 validado em 12/06 |

> Os cinco riscos de referência do template foram exercitados em homologação nesta semana e estão cobertos pelos RFs correspondentes, todos validados. Recomenda-se mantê-los na suíte de regressão.

## 8. Processo recomendado

1. Registrar o bug com o máximo de contexto. 2. Associar ao requisito/regra/CA. 3. Priorizar por severidade. 4. Atualizar status conforme a correção. 5. Registrar reteste e evidências.

## 9. Referências

- `plano-de-testes-mvp.md` · `drp.md` · `5-gdr.md` · `relatorio-de-bugs-template.md`

> **Nota de numeração:** IDs BUG-162+ dão sequência ao último ID da semana 31-06 (BUG-161). Reprovações de RF não têm card próprio (são o mesmo card do requisito movido para *BUGS*); a exceção é o BUG-162, card criado em *BUGS* para consolidar os bloqueios de teste do RF008.
>
> **Nota de encerramento:** com o fechamento de 12/06 às 18:01 (DB001 → *CONCLUIDO*), o quadro não possui nenhum bug ou requisito em aberto — primeira semana do projeto com **zero pendências**.
