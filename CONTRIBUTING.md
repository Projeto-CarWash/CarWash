# Guia de Contribuição — CarWash

Este documento define **como o time trabalha no monorepo**: branches, commits, PRs e merges.
**Vale para todos** — backend, frontend, docs, infra. Sem exceção.

> Em caso de dúvida não coberta aqui, alinhe com o `arquiteto-carwash` (decisões técnicas) ou `po-pm-carwash` (escopo).

---

## 1. Estratégia de Branch — Trunk-Based simplificado

```
main  ──●──────●──────●──────●──────●──→  (sempre deployável)
        │      │      │      │      │
        └─feat │      │      │      └─fix
              └─feat  │      └─chore
                     └─test
```

### Regras

1. **`main` é sagrada.** Sempre deployável, sempre verde no CI. Nada de push direto.
2. **Branches são curtas** — alvo: ≤ 3 dias de vida. Mais que isso, divide a tarefa.
3. **Uma branch = uma task do PO/PM** (idealmente, um RF ou parte dele).
4. **Branches velhas são deletadas** após o merge (squash). Sem `release/`, sem `develop/`, sem branches paralelas de longa duração.
5. **Hotfix em produção** segue o mesmo fluxo (`fix/...`), mas com label `hotfix` e revisão expressa do arquiteto + QA.

### Nomenclatura

```
<tipo>/<id-task>-<slug-curto-em-kebab>
```

- **tipo:** `feat`, `fix`, `chore`, `docs`, `refactor`, `test`, `perf`, `ci`, `build`
- **id-task:** ID do PO/PM (ex: `TASK-042`) **ou** RF do DRP (ex: `RF020`). Use o que houver.
- **slug:** 2 a 5 palavras descritivas em kebab-case.

✅ **Bons exemplos:**
- `feat/RF020-conflito-global-agendamento`
- `fix/TASK-118-422-cliente-cpf-formato`
- `chore/RF018-celulas-1-100-validator`
- `docs/atualiza-readme-docker`
- `ci/workflow-cache-otimizacao`

❌ **Maus exemplos:**
- `dev`, `meu-branch`, `temp`, `wip`
- `feature-agenda` (sem tipo correto, sem id, slug genérico)
- `RF020` (sem tipo nem slug)
- `feat/agenda-mil-mudancas` (escopo grande demais)

---

## 2. Conventional Commits — formato obrigatório

Toda mensagem de commit segue:

```
<tipo>(<escopo>): <descrição curta no imperativo>

[corpo opcional explicando o porquê — não o quê]

[footer opcional com Refs, BREAKING CHANGE, Co-Authored-By]
```

### Tipos permitidos

| Tipo       | Quando usar                                                         |
| ---------- | ------------------------------------------------------------------- |
| `feat`     | Nova funcionalidade ligada a um RF                                  |
| `fix`      | Correção de bug                                                     |
| `docs`     | Mudança em `docs/`, README, CONTRIBUTING, comentários de doc        |
| `style`    | Formatação, sem mudança de lógica (Prettier, dotnet format)         |
| `refactor` | Reescrita sem mudar comportamento externo                           |
| `test`     | Adicionar/ajustar testes (xUnit, Vitest, Playwright)                |
| `chore`    | Tarefas de manutenção sem efeito em runtime (deps, configs)         |
| `build`    | Mudanças em build/empacotamento (Dockerfile, csproj, package.json)  |
| `ci`       | Pipeline (workflows, hooks, lint configs)                           |
| `perf`     | Melhoria de performance mensurável                                  |
| `revert`   | Reverter um commit anterior                                         |

### Escopos do monolito

Use **um e apenas um** escopo por commit. Se a mudança cruza fronteiras, **divida em commits separados**.

| Escopo      | Cobre                                                               |
| ----------- | ------------------------------------------------------------------- |
| `back`      | `backend/` — código C#/.NET (Domain, Application, Infrastructure, Api) |
| `front`     | `frontend/` — código React/TS                                       |
| `db`        | Migrations EF Core, scripts SQL, índices, constraints               |
| `infra`     | `docker/`, compose files, Dockerfiles, Makefile                     |
| `ci`        | `.github/workflows/`, hooks husky                                   |
| `docs`      | `docs/`, README, CONTRIBUTING, comentários de doc                   |
| `agents`    | `.claude/agents/` — definições de subagentes                        |
| `deps`      | Atualização de dependências (NuGet, npm)                            |
| `auth`      | Mudanças cruzando back+front em autenticação (raro, justifique)     |

### Regras de escrita

- **Imperativo:** "adiciona", "corrige", "remove" — não "adicionado", "corrigindo".
- **Sem ponto final** na linha de assunto.
- **≤ 72 caracteres** na linha de assunto.
- **Body com `por quê`**, não `o quê` (o diff já mostra o quê).
- **Refs obrigatórias** quando há vínculo a requisito: `Refs: RF020, RN011, CA006`.
- **BREAKING CHANGE:** se quebra contrato (API, schema, env var), declare no footer.

### Exemplos

```
feat(back): adiciona constraint UNIQUE global em Agendamento(VeiculoId, DataHora)

Implementa RN011 no banco como última linha de defesa contra race condition.
Captura DbUpdateException e retorna 409 Conflict.

Refs: RF020, RN011, CA006
```

```
fix(front): trata erro 409 do POST /agendamentos com mensagem clara

Antes a UI mostrava genérico "erro ao salvar"; agora cita conflito de horário
em outra filial conforme RN011.

Refs: RF020, RN011, CA006
```

```
chore(infra): aumenta limite de memória do backend em produção para 1G

Métrica do staging mostrou picos de 480Mi durante listagem de agenda.
Margem dobra para evitar OOM em dia de pico.

Refs: RNF002
```

```
build(deps): atualiza Npgsql.EntityFrameworkCore.PostgreSQL 8.0.10 -> 8.0.11

Patch security release. Sem mudança de API.
```

---

## 3. Pull Requests

### Antes de abrir

1. ✅ Branch atualizada com `main` (rebase, não merge).
2. ✅ CI local passa (`make smoke` quando aplicável; `npm run check` no front; `dotnet build` + `dotnet test` no back).
3. ✅ Cobertura de testes adicionada — todo CA novo tem teste (CA011).
4. ✅ Migration EF Core revisada (se houver) — `dotnet ef migrations script` lido manualmente.

### Título do PR

**É a mensagem de squash merge no `main`.** Use o mesmo formato Conventional Commits:

```
feat(back): adiciona constraint UNIQUE global em Agendamento (RF020)
```

O título do PR deve seguir Conventional Commits — revise manualmente antes de pedir review.

### Descrição

Use o template em `.github/PULL_REQUEST_TEMPLATE.md`. Os campos não-opcionais são:

- **Por quê** (motivação de negócio: P1–P7, RF, CA).
- **O que muda** (resumo técnico).
- **Como testei** (manual + automatizado).
- **Rastreabilidade** (RF, RN, CA, módulo do DAT).
- **Riscos / impactos** (RV, RAT mitigados ou criados).
- **Checklist de DoD** marcado.

### Tamanho

- **Alvo:** ≤ 400 linhas mudadas, ≤ 10 arquivos.
- **Acima de 800 linhas:** justifique no PR ou divida.
- Migrations + código + teste no mesmo PR é OK e desejável (atomicidade).

### Reviewers

- **Code review obrigatório** — mínimo 1 aprovação.
- **CODEOWNERS** atribui automaticamente: arquiteto para mudanças cruzando módulos; QA para PRs com label `needs-qa`.
- **PO/PM** valida quando o PR fecha um RF Must.

### Gates de merge (bloqueantes)

1. ✅ CI verde (lint, typecheck, build, test, docker validate, spell).
2. ✅ Pelo menos 1 aprovação.
3. ✅ Sem conflitos com `main`.
4. ✅ Conversas resolvidas.
5. ✅ Para PRs com label `needs-qa`: aprovação do `qa-carwash`.
6. ✅ Para PRs tocando `db/`, `infra/` ou `Directory.*.props`: aprovação do `arquiteto-carwash`.

### Estratégia de merge

- **Squash merge** é o padrão. Histórico do `main` fica linear e legível.
- **Rebase merge** apenas para PRs do arquiteto que mantêm commits intencionalmente atômicos.
- **Merge commit** é proibido.

### Após o merge

- Branch é deletada automaticamente.
- Se quebrar `main` (smoke test pós-merge), prioridade #1: reverter (`revert`) e investigar em branch nova.

---

## 4. Regras especiais para o monolito

### Não misture front e back num único PR

Mesmo que sejam para o mesmo RF. Razões:
- Reviewers diferentes (CODEOWNERS).
- Risco de regressão isolado.
- Squash merge fica menos claro.

**Exceção:** mudança de contrato compartilhado (ex: novo formato de erro 409 da API) — abra **um PR de back primeiro** com o contrato + 1 endpoint, e **um PR de front depois** consumindo. Use label `coordinated-change`.

### Migrations EF Core

- **Sempre** uma migration por mudança lógica.
- **Nunca** edite uma migration já merged em `main` — gere uma nova migration corretiva.
- O nome da migration vira parte da auditoria: `AddVeiculoPlacaUnique`, `AddAgendamentoConflitoGlobalConstraint`.
- PR com migration **exige** revisão do arquiteto.

### Mudanças em `Directory.Packages.props` ou `package.json` (deps)

- Use tipo `build(deps)`.
- Patch/minor: bot/dev pode mergear.
- Major: exige revisão do arquiteto + nota de migração no PR.

### Mudanças em `docker/`, `docker-compose*.yml` ou `Makefile`

- Tipo `chore(infra)` ou `ci(infra)`.
- Exige `make smoke ENV=dev` rodado localmente e print/output no PR.

### Mudanças em `.claude/agents/`

- Tipo `chore(agents)`.
- Não exige aprovação do arquiteto, mas exige justificativa no corpo do commit.

---

## 5. Workflow exemplo (passo a passo)

```bash
# 1. Atualiza main
git checkout main && git pull --ff-only

# 2. Cria branch a partir de uma task do PO/PM
git checkout -b feat/RF020-conflito-global-agendamento

# 3. Trabalha em commits pequenos e atômicos
git add backend/src/CarWash.Domain/Agendamentos/Agendamento.cs
git commit   # abre editor com template .gitmessage

git add backend/tests/CarWash.Domain.Tests/AgendamentoTests.cs
git commit -m "test(back): cobre RN011 com race condition de duas filiais"

# 4. Atualiza com main antes de abrir PR
git fetch origin && git rebase origin/main

# 5. Push + PR
git push -u origin feat/RF020-conflito-global-agendamento
gh pr create --fill --label needs-qa

# 6. Após aprovação e CI verde: squash merge via interface do GitHub.
# Branch é deletada automaticamente.
```

---

## 6. Política de revert

- Quebrou `main`? **Reverte primeiro, investiga depois.**
- `git revert <sha>` (não force-push).
- Abre nova branch `fix/<tipo>-<slug>` para a correção.
- O revert também respeita Conventional Commits: `revert: <subject>`.

---

## 7. Quem aprova o quê (CODEOWNERS resumido)

| Pasta / arquivo                          | Owner padrão                  |
| ---------------------------------------- | ----------------------------- |
| `backend/`                               | `dev-dotnet-carwash` + arquiteto |
| `frontend/`                              | `dev-react-carwash` + arquiteto  |
| `backend/Directory.*.props`              | arquiteto (obrigatório)       |
| `backend/**/Migrations/`                 | arquiteto (obrigatório)       |
| `docker/`, `docker-compose*`, `Makefile` | arquiteto (obrigatório)       |
| `.github/workflows/`                     | arquiteto                     |
| `docs/`                                  | PO/PM + Antonio Neto          |
| `.claude/agents/`                        | arquiteto + CEO               |
| `.editorconfig`                          | arquiteto                     |
| testes que cobrem CA006–CA011            | QA (obrigatório)              |

Definição completa: `.github/CODEOWNERS`.

---

## 8. O que NUNCA fazer

- ❌ Push direto em `main`.
- ❌ Force-push em branch já compartilhada.
- ❌ Commit com mensagem genérica (`wip`, `fix`, `update`, `ajustes`).
- ❌ Misturar refactor amplo + feature no mesmo PR.
- ❌ Editar migration já mergeada.
- ❌ Commitar `.env`, segredo, certificado, dump de banco.
- ❌ Pular hooks com `--no-verify`.
- ❌ Mergear PR com CI vermelho "porque o teste é flaky" — investigue com QA antes.

---

## 9. Ferramentas que ajudam

- **gitmessage template:** `git config commit.template .gitmessage` (uma vez).
- **CI:** lint, typecheck, build, test, docker compose config.
- **CODEOWNERS:** auto-atribui reviewers no PR.
- **PR template:** `.github/PULL_REQUEST_TEMPLATE.md`.

Setup inicial (uma vez por dev):

```bash
git config commit.template .gitmessage
cd frontend && npm install   # instala husky
```

---

> Dúvidas sobre processo: pergunte ao `arquiteto-carwash`. Dúvidas sobre escopo de uma task: `po-pm-carwash`. Dúvidas sobre cobertura de teste: `qa-carwash`.
