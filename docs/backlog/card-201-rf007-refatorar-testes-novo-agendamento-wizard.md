# Backlog — Card 201 / RF007 — Refatorar testes de `NovoAgendamentoPage` para fluxo wizard

> Status: refinamento do analista. Sem lacunas bloqueantes — execução direta após priorização.
> Rastreabilidade base: P1 (agendamento desorganizado) → RF007 (Must) + RF015 (Must) → UC004 → Módulo Agenda (DAT §4.1) / Frontend.
> Origem: dívida técnica de testes — `frontend/src/pages/Agendamentos/NovoAgendamentoPage.test.tsx` (13 testes) ainda assume um form plano (`getByLabelText('Cliente')`, `getByLabelText('Filial')`), mas o componente foi reescrito como wizard multi-step (Cliente → Veículo → Data) durante a entrega da confirmação em 2 etapas (ADR 0004 / card 133). Build não quebra, mas pipeline fica vermelho em vitest.

## Resumo executivo

Realinhar a suíte de testes de integração da tela de novo agendamento ao
componente atualmente em produção (`frontend/src/components/agendamentos/NovoAgendamentoPage.tsx`),
que segue o fluxo de wizard com etapas Cliente · Veículo · Data e revisão em 2 etapas
(RF015 / ADR 0004). Mantém a cobertura comportamental (validação Zod, 409
RN011, 409 divergência de resumo, 410 sessão expirada, 400 por campo) mas reescreve
as interações para refletir os controles reais (steps, botões "Avançar"/"Revisar
agendamento"/"Confirmar agendamento"). Não adiciona nova lógica de negócio — apenas
restabelece o sinal verde do pipeline.

## User stories

### US-201.1 — Pipeline verde para o fluxo de novo agendamento
**Como** desenvolvedor do CarWash, **quero** que a suíte de testes de
`NovoAgendamentoPage` esteja alinhada ao componente em produção **para que** o
pipeline volte a sinalizar regressões reais de forma confiável. (Dívida técnica;
RF007/RF015; RNF009 — observabilidade no nível de teste.)

### US-201.2 — Cobertura preservada do fluxo wizard
**Como** Product Owner, **quero** garantir que a refatoração não reduza a
cobertura comportamental do fluxo de agendamento **para que** os critérios de
aceite do card 133 (confirmação em 2 etapas) continuem protegidos por testes
automatizados. (RF015; CA011.)

## Lacunas para decisão

- **L1 (não bloqueante)** — Em quantos passos exatamente o wizard navega hoje?
  O nome canônico de cada step ("Cliente", "Veículo", "Data") e o texto exato
  dos botões de avanço/voltar devem ser confirmados lendo o componente. Não é
  decisão de produto — é leitura de código antes de escrever os testes.
- **L2 (não bloqueante)** — Manter a abordagem MSW + `renderComProviders` já
  usada no arquivo atual? Default: sim — alinha com o restante da suíte e
  evita reinventar fixtures.

## Critérios de aceite (Given/When/Then)

1. **CA-201.1 — Pipeline:** Dado o estado pós-refatoração, quando executar
   `pnpm --filter frontend test` (ou comando equivalente da casa), então
   100% dos testes de `NovoAgendamentoPage.test.tsx` passam.
2. **CA-201.2 — Cobertura de validação Zod:** Mantido teste que verifica
   erros de validação na etapa de edição quando o formulário é submetido vazio.
3. **CA-201.3 — Cobertura da transição edição → revisão:** Mantido teste que
   preenche dados válidos, clica em "Revisar agendamento" e aguarda aparecer
   o botão "Confirmar agendamento" (etapa de revisão).
4. **CA-201.4 — Cobertura de confirmação bem-sucedida:** Mantido teste que
   completa o fluxo até o `201 Created` e redirecionamento/feedback.
5. **CA-201.5 — Cobertura de 409 RN011 (conflito de veículo):** Mantido
   teste de erro de conflito global.
6. **CA-201.6 — Cobertura de 409 divergência de resumo:** Mantido cenário do
   ADR 0004 (resumo da pré-confirmação não bate com o resumo recebido na
   confirmação).
7. **CA-201.7 — Cobertura de 410 sessão expirada:** Mantido o cenário em
   que a `IdempotencyKey` expira entre os passos.
8. **CA-201.8 — Cobertura de 400 por campo:** Mantido teste que checa
   propagação de erros de campo do backend para o formulário.
9. **CA-201.9 — Zero regressão funcional:** Nenhuma mudança no componente
   `NovoAgendamentoPage.tsx` é introduzida por este card. Diff restrito a
   arquivos `*.test.tsx`, `mswServer` handlers se necessário, e helpers
   de teste sob `frontend/src/test/`.
10. **CA-201.10 — Helpers extraídos:** Funções utilitárias do tipo
    `aguardarListas`, `preencherFormularioValido`, `irParaRevisao` são
    refatoradas para refletir o fluxo wizard (com `irParaEtapa(n)` ou
    equivalente) e mantidas no mesmo arquivo de teste para coesão.

## Tarefas — trilha Frontend (React)

| ID | Tarefa | Esforço | Dep. |
|----|--------|---------|------|
| FE-01 | Ler `frontend/src/components/agendamentos/NovoAgendamentoPage.tsx` e mapear: nomes/labels dos steps, roles dos botões de navegação, ordem dos campos por step | PP | — |
| FE-02 | Reescrever helpers `aguardarListas`, `preencherFormularioValido`, `irParaRevisao` para wizard (cada um avança o step necessário) | P | FE-01 |
| FE-03 | Atualizar os 13 testes existentes (assertions e interações) para casar com o wizard, preservando o cenário coberto (mesmo CA → mesmo `it`) | M | FE-02 |
| FE-04 | Conferir MSW handlers em `frontend/src/test/handlers.ts` — adicionar/ajustar resposta de `/api/v1/clientes/{id}/veiculos` se o step Veículo dispara nova query | PP | FE-01 |
| FE-05 | Rodar `pnpm --filter frontend test --coverage` e confirmar que a cobertura do componente não cai (limite informativo, não bloqueia) | PP | FE-03 |
| FE-06 | Code review focado: garantir que nenhum teste foi "amaciado" (skip, `expect(true).toBe(true)`, asserts removidos) | PP | FE-03 |

## Definition of Ready

- L1 resolvida (leitura do componente feita; nomes dos steps mapeados).
- Conjunto de MSW handlers atualmente usado pelo arquivo identificado.
- Diff esperado é apenas em arquivos de teste (sem tocar produção).

## Definition of Done

- 13 testes verdes em vitest.
- 8 cenários (CA-201.2 a CA-201.8) explicitamente cobertos — checklist
  marcado no PR.
- Nenhum `it.skip`, `xit`, `// TODO` ou `expect.assertions(0)` introduzido.
- Diff restrito a `*.test.tsx` e arquivos sob `frontend/src/test/`.
- Code review aprovado por um par do frontend.
- Pipeline CI marcado como verde — confirmado por screenshot/anexo no PR.

## Prioridade e estimativa

- **Prioridade:** Should — não bloqueia entrega de RF007 (já implementado e
  homologado), mas mantém o pipeline confiável. Subir para Must se o time
  acordar que pipeline vermelho == bloqueio de merge.
- **Esforço total:** P (1 dev frontend, ~meio dia).
- **Dependências externas:** nenhuma.
- **Bloqueia:** indiretamente — dificulta detectar regressões em RF007/RF015
  enquanto a suite estiver vermelha.

## Rastreabilidade resumida

| Rastreável | ID |
|------------|----|
| Problema (DVP-E §4.1) | P1 |
| Requisito (DRP §3) | RF007 (Must), RF015 (Must) |
| Regra de negócio (DRP §4) | RN011 (testada), RN010 (testada) |
| Critério de aceite global (DRP §10) | CA001, CA011 |
| Risco mitigado (DAT §11) | — (dívida de teste, não risco arquitetural) |
| Módulo (DAT §4.1) | Agenda — Frontend |
| ADR base | ADR 0004 (confirmação em 2 etapas) |
