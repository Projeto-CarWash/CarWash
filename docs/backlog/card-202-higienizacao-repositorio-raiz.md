# Backlog — Card 202 — Higienização do repositório raiz e `.gitignore`

> Status: refinamento do analista. Sem lacunas bloqueantes — execução direta.
> Rastreabilidade base: dívida operacional (DX/CI). Não está vinculado a RF
> de negócio do DRP; vincula-se a **RNF002 (disponibilidade do pipeline)** e
> à **premissa A3** do DVS — "ambiente de desenvolvimento estável".
> Origem: a raiz do monorepo carrega `package.json` + `package-lock.json` +
> `node_modules/` duplicando dependências do frontend, além de um patch vazio
> (`rf021.patch`) e um screenshot (`mintty.2026-05-24_19-36-23.png`). O `.gitignore`
> da raiz não ignora `node_modules/`, `bin/`, `obj/`, `dist/`, então o working
> tree fica cronicamente sujo (milhares de "D node_modules/..." em `git status`).

## Resumo executivo

Remove artefatos do diretório raiz que não pertencem ao código-fonte do
produto e ajusta o `.gitignore` para parar de versionar diretórios de build
e dependências. Card de baixo esforço, alto impacto em developer experience
e em ergonomia do `git status` — pré-requisito para qualquer fluxo de PR
limpo. Não toca código de aplicação.

## User stories

### US-202.1 — Working tree limpo
**Como** desenvolvedor do CarWash, **quero** que `git status` em uma branch
recém-checkada mostre 0 arquivos não rastreados/modificados pertencentes a
build ou dependências **para que** mudanças reais fiquem visíveis no diff
sem ruído. (DX; premissa A3 do DVS.)

### US-202.2 — Sem dependência fantasma na raiz
**Como** mantenedor do monorepo, **quero** que `package.json`/`node_modules`
existam **apenas** dentro de `frontend/` **para que** não exista ambiguidade
sobre onde rodar `pnpm install`, e ferramentas como Renovate/Dependabot não
sejam confundidas por dois manifestos duplicados. (RAT05 — acoplamento.)

### US-202.3 — Onboarding previsível
**Como** novo dev entrando no projeto, **quero** seguir um README único que
aponte para `frontend/` (web) e `backend/` (api) **para que** eu não rode
comandos no diretório errado por inferência implícita do `package.json` da
raiz. (RNF001 — usabilidade do projeto, lido por humanos.)

## Lacunas para decisão

- **L1 (não bloqueante)** — O `package.json` da raiz tem algum script
  utilitário do monorepo (lint cross-repo, husky, lint-staged)? **Verificar
  antes de deletar.** Se sim, mover para `tools/` ou converter em um root
  package legítimo (workspaces); se não, deletar. Default analista após
  inspeção rápida: **sem scripts úteis** — pode deletar.
- **L2 (não bloqueante)** — Husky/lint-staged configurados na raiz? Se sim,
  precisam ser preservados em outro lugar (ex: `frontend/package.json` com
  hooks via `prepare`).

## Critérios de aceite (Given/When/Then)

1. **CA-202.1 — Arquivos removidos:** Os seguintes caminhos não existem mais:
   - `/home/gbrogio/university/carwash/package.json`
   - `/home/gbrogio/university/carwash/package-lock.json`
   - `/home/gbrogio/university/carwash/node_modules/`
   - `/home/gbrogio/university/carwash/rf021.patch`
   - `/home/gbrogio/university/carwash/mintty.2026-05-24_19-36-23.png`
2. **CA-202.2 — `.gitignore` da raiz atualizado:** o arquivo raiz `.gitignore`
   ignora ao menos:
   - `node_modules/`
   - `dist/`
   - `build/`
   - `bin/`
   - `obj/`
   - `*.user`, `*.suo`
   - `.idea/`, `.vs/`
   - `*.log`
   - `*.patch`
   - `mintty.*.png`
3. **CA-202.3 — Working tree limpo após checkout:** Dado um checkout fresh
   da branch principal, quando rodar `git status`, então o output mostra
   "nothing to commit, working tree clean" (modulo arquivos legitimamente
   editados pelo dev).
4. **CA-202.4 — Build do frontend não quebra:** Dado o estado pós-card,
   quando rodar `cd frontend && pnpm install && pnpm build`, então o build
   conclui sem erros.
5. **CA-202.5 — Build do backend não quebra:** Dado o estado pós-card,
   quando rodar `cd backend && dotnet build`, então o build conclui sem
   erros (`bin/`/`obj/` agora ignorados, mas não removidos).
6. **CA-202.6 — Pipeline CI verde:** O workflow CI (GitHub Actions ou
   equivalente) executa do início ao fim sem regressão.
7. **CA-202.7 — Husky/lint-staged preservados (se L2 = sim):** Os hooks
   pre-commit/pre-push continuam funcionando após a limpeza — testado por
   um commit dummy local.

## Tarefas

| ID | Tarefa | Esforço | Dep. |
|----|--------|---------|------|
| OPS-01 | Inspecionar `/package.json` raiz: listar `scripts`, `devDependencies`, `dependencies` e bater contra os do `frontend/package.json` para confirmar duplicidade total | PP | — |
| OPS-02 | Resolver L1/L2: se houver script único na raiz, decidir destino (mover para `frontend/` ou para `tools/`) antes de apagar | PP | OPS-01 |
| OPS-03 | Remover artefatos: `package.json`, `package-lock.json`, `node_modules/`, `rf021.patch`, `mintty.2026-05-24_19-36-23.png` (raiz) | PP | OPS-02 |
| OPS-04 | Atualizar `.gitignore` raiz com regras de `node_modules/`, `dist/`, `build/`, `bin/`, `obj/`, `.vs/`, `.idea/`, `*.user`, `*.log`, `*.patch`, `mintty.*.png` | PP | OPS-03 |
| OPS-05 | Atualizar/criar README raiz (se ainda houver instrução apontando para `pnpm` na raiz) | PP | OPS-04 |
| OPS-06 | Verificar workflows CI (`.github/workflows/*.yml`) — confirmar que nenhum job assume `pnpm install` na raiz | PP | OPS-04 |
| OPS-07 | Smoke test local: `cd frontend && pnpm install && pnpm build && pnpm test` + `cd backend && dotnet build && dotnet test` | PP | OPS-04 |
| OPS-08 | Abrir PR com diff mínimo, descrição apontando para este card e checklist dos 7 CAs | PP | OPS-07 |

## Definition of Ready

- L1 e L2 inspecionados (não bloqueia o card — bloqueia apenas decidir destino
  dos hooks, se existirem).
- Acordo do CEO/arquiteto de que `node_modules/` na raiz era "lixo" (já
  confirmado nas tratativas que originaram este card).

## Definition of Done

- 7 CAs marcados verdes no PR.
- `git status` em branch fresh = clean.
- CI verde no PR e na branch principal pós-merge.
- README/onboarding atualizado se houver referência fantasma à raiz.

## Prioridade e estimativa

- **Prioridade:** Should — não bloqueia entrega de RF Must, mas trava
  produtividade do time. Subir para Must se a próxima revisão do CEO/orientador
  detectar working tree sujo em screenshot.
- **Esforço total:** PP (1 dev, ~1–2 horas).
- **Dependências externas:** nenhuma.
- **Bloqueia:** PRs futuros — qualquer alteração em `frontend/` aparece junto
  com 5k linhas de `D node_modules/...` no diff.

## Rastreabilidade resumida

| Rastreável | ID |
|------------|----|
| Problema (DVP-E §4.1) | — (não é problema de negócio; é dívida operacional) |
| Requisito (DRP §3) | — (dívida técnica; tangencia RNF002, RNF001) |
| Premissa (DVS) | A3 — ambiente de desenvolvimento estável |
| Risco mitigado (DAT §11) | RAT05 (acoplamento — ambiguidade de manifesto) |
| Módulo (DAT §4.1) | — (raiz do monorepo, não é módulo de aplicação) |
| ADR base | — |
