# Configuração do Repositório no GitHub — CarWash

Este documento é um **passo a passo** para você configurar tudo que **não dá para versionar em arquivo**: settings do repo, branch protection, CodeRabbit, labels, secrets etc.

> Faça uma vez. Marque o checkbox conforme avança. Atualize este doc se mudar algo.

---

## ✅ Pré-requisitos

- [x] Repositório criado no GitHub e código enviado (`git push -u origin main`).
- [x] Você tem permissão de **Admin** no repo.
- [x] CLI do GitHub instalada: `gh --version` (opcional, mas acelera).

---

## 1. Configurações gerais do repositório

**Caminho:** `Settings` → `General`

### 1.1 Pull Requests
- [x] **Allow merge commits** — ❌ DESMARCAR
- [x] **Allow squash merging** — ✅ MARCAR
  - [x] **Default to pull request title** (em "Default commit message") — ✅
- [x] **Allow rebase merging** — ✅ MARCAR (uso restrito ao arquiteto, ver `CONTRIBUTING.md` §3)
- [ ] **Always suggest updating pull request branches** — ✅ MARCAR
- [ ] **Allow auto-merge** — ✅ MARCAR
- [ ] **Automatically delete head branches** — ✅ MARCAR (alinhado a `CONTRIBUTING.md` §1)

### 1.2 Features
- [ ] **Issues** — ✅ MARCAR
- [ ] **Projects** — ✅ MARCAR (vamos usar para tracking do PO/PM)
- [ ] **Discussions** — opcional, marque se quiser canal assíncrono.
- [ ] **Wiki** — ❌ DESMARCAR (use `docs/` no repo).

### 1.3 Pushes
- [ ] **Limit pushes that create files larger than** `100 MB` (se a UI permitir, deixe em 100 MB).

---

## 2. Branch protection — `main`, `homolog`, `development`

**Caminho:** `Settings` → `Branches` → `Add branch ruleset` (ou **Branch protection rules** → `Add rule`)

> **Modelo de promoção alinhado ao DAT §8.1 (3 ambientes):**
>
> ```
>   feature/* ──PR──▶ development ──PR──▶ homolog ──PR──▶ main
>      (dev local)    (deploy dev)        (deploy hom)    (deploy prod)
> ```
>
> Cada branch protegida exige PR + CI verde + aprovação. Gates ficam mais
> rígidos conforme o código se aproxima de produção. **Crie um ruleset por
> branch protegida**, seguindo as três configurações abaixo (2.1–2.3 para
> `main`, 2.4 para `homolog`, 2.5 para `development`).

### 2.1 Identificação
- [ ] **Branch name pattern:** `main`
- [ ] **Enforcement status:** `Active`

### 2.2 Regras obrigatórias

#### Bypass list
- [ ] **Vazio.** Nem o owner faz push direto.

#### Restrict pushes / Require a pull request before merging
- [ ] **Required approvals:** `1`
- [ ] **Dismiss stale pull request approvals when new commits are pushed** — ✅
- [ ] **Require review from Code Owners** — ✅ (usa o `.github/CODEOWNERS`)
- [ ] **Require approval of the most recent reviewable push** — ✅
- [ ] **Allowed merge methods:** apenas `Squash` (deixe Rebase ativo só se quiser permitir uso restrito).

#### Require status checks to pass before merging
- [ ] **Require branches to be up to date before merging** — ✅
- [ ] **Required status checks** (adicione todos os jobs do `ci.yml`):
  - `Commits & PR title (Conventional Commits)`
  - `Frontend (lint • typecheck • test • build)`
  - `Backend (format • build • test)`
  - `Docker compose validate`
  - `Spell check`
  - `CodeRabbit` (aparece depois que o app for instalado — passo 4)

#### Require conversation resolution before merging
- [ ] ✅

#### Require linear history
- [ ] ✅ (espelha a regra de squash merge do `CONTRIBUTING.md`)

#### Block force pushes
- [ ] ✅

#### Restrict deletions
- [ ] ✅

#### Require signed commits
- [ ] Opcional — marque se o time tiver GPG/SSH signing configurado.

### 2.3 Salvar
- [ ] Clique **Create** / **Save changes**.

---

### 2.4 Branch protection — `homolog` (homologação)

**Posição no fluxo:** recebe PR vindo **apenas de `development`**. Após merge, deploy automático em homologação para validação do proprietário (premissa A1).

**Caminho:** `Settings` → `Branches` → `Add branch ruleset`.

#### 2.4.1 Identificação

- [ ] **Branch name pattern:** `homolog`
- [ ] **Enforcement status:** `Active`

#### 2.4.2 Regras obrigatórias

##### Bypass list — homolog

- [ ] **Vazio.** Nem owner faz push direto.

##### Restrict pushes / Require a pull request before merging — homolog

- [ ] **Required approvals:** `1`
- [ ] **Dismiss stale pull request approvals when new commits are pushed** — ✅
- [ ] **Require review from Code Owners** — ✅ (QA é mandatório via CODEOWNERS para PRs com testes)
- [ ] **Require approval of the most recent reviewable push** — ✅
- [ ] **Allowed merge methods:** apenas `Squash`.

##### Require status checks to pass before merging — homolog

- [ ] **Require branches to be up to date before merging** — ✅
- [ ] **Required status checks** (mesmos do `main` — gate de qualidade igual):
  - `Commits & PR title (Conventional Commits)`
  - `Frontend (lint • typecheck • test • build)`
  - `Backend (format • build • test)`
  - `Docker compose validate`
  - `Spell check`
  - `CodeRabbit`

##### Require conversation resolution before merging — homolog

- [ ] ✅

##### Require linear history — homolog

- [ ] ✅

##### Block force pushes — homolog

- [ ] ✅

##### Restrict deletions — homolog

- [ ] ✅

##### Restrict creations (recomendado) — homolog

- [ ] **Restrict creations** — ✅ (somente `development` pode abrir PR para `homolog`).
  > GitHub não impõe nativamente "PR só de branch X"; reforçamos via job no CI (`branch-source` valida que `head ref == development`).

##### Require signed commits — homolog

- [ ] Opcional.

#### 2.4.3 Salvar

- [ ] Clique **Create**.

---

### 2.5 Branch protection — `development` (integração)

**Posição no fluxo:** recebe PR vindo de **branches feature/fix/etc.**. Após merge, deploy automático em ambiente de desenvolvimento. Gates são mais leves para não atrasar iteração diária do time.

**Caminho:** `Settings` → `Branches` → `Add branch ruleset`.

#### 2.5.1 Identificação

- [ ] **Branch name pattern:** `development`
- [ ] **Enforcement status:** `Active`

#### 2.5.2 Regras obrigatórias

##### Bypass list — development

- [ ] **Vazio.** Sem push direto, mesmo em integração.

##### Restrict pushes / Require a pull request before merging — development

- [ ] **Required approvals:** `1`
- [ ] **Dismiss stale pull request approvals when new commits are pushed** — ✅
- [ ] **Require review from Code Owners** — ❌ DESMARCAR (acelera integração; CODEOWNERS volta a valer em `homolog` e `main`)
- [ ] **Require approval of the most recent reviewable push** — ✅
- [ ] **Allowed merge methods:** apenas `Squash`.

##### Require status checks to pass before merging — development

- [ ] **Require branches to be up to date before merging** — ✅
- [ ] **Required status checks** (mesmos do `main` — qualidade não negocia):
  - `Commits & PR title (Conventional Commits)`
  - `Frontend (lint • typecheck • test • build)`
  - `Backend (format • build • test)`
  - `Docker compose validate`
  - `Spell check`
  - `CodeRabbit`

##### Require conversation resolution before merging — development

- [ ] ✅

##### Require linear history — development

- [ ] ✅

##### Block force pushes — development

- [ ] ✅

##### Restrict deletions — development

- [ ] ✅

##### Require signed commits — development

- [ ] Opcional.

#### 2.5.3 Salvar

- [ ] Clique **Create**.

---

### 2.6 Resumo das diferenças entre as três branches

| Regra                                    | `development`         | `homolog`            | `main`               |
| ---------------------------------------- | --------------------- | -------------------- | -------------------- |
| Origem dos PRs                           | feature/fix/chore/... | apenas `development` | apenas `homolog`     |
| Aprovações exigidas                      | 1                     | 1                    | 1                    |
| Code Owners obrigatório                  | ❌                    | ✅                   | ✅                   |
| Status checks (CI completo + CodeRabbit) | ✅                    | ✅                   | ✅                   |
| Conversation resolution                  | ✅                    | ✅                   | ✅                   |
| Linear history                           | ✅                    | ✅                   | ✅                   |
| Block force push / deletion              | ✅                    | ✅                   | ✅                   |
| Squash merge apenas                      | ✅                    | ✅                   | ✅                   |
| Restrict creation (origem)               | ❌                    | ✅                   | ✅                   |
| Deploy automático após merge             | dev                   | hom                  | prod                 |
| QA obrigatório no review                 | ❌                    | ✅ (via CODEOWNERS)  | ✅ (via CODEOWNERS)  |

> **Observação sobre `Restrict creation`:** GitHub Branch Protection não tem nativamente a regra "PR só pode vir da branch X". A forma de forçar é via job no `ci.yml` que falha quando o `head ref` não é o esperado. Esse job precisa ser adicionado quando `development` e `homolog` forem criadas — abra issue rastreando.

---

## 3. CODEOWNERS — substituir placeholders

O arquivo `.github/CODEOWNERS` está com handles fictícios (`@arquiteto`, `@dev-back`, etc.). Substitua pelos handles **reais** do GitHub do time.

### 3.1 Mapeamento sugerido

| Placeholder    | Handle real (preencha) |
| -------------- | ---------------------- |
| `@arquiteto`   | `@<seu-handle>`        |
| `@dev-back`    | `@<dev-back>`          |
| `@dev-front`   | `@<dev-front>`         |
| `@qa`          | `@<qa>`                |
| `@po`          | `@<po-pm>`             |
| `@ceo`         | `@<ceo-stakeholder>`   |

### 3.2 Aplicar
```bash
# substitua localmente os 6 placeholders e commit
sed -i 's,@arquiteto,@SEU_HANDLE,g' .github/CODEOWNERS
# repita para os outros, ou edite manualmente
git add .github/CODEOWNERS
git commit -m "chore(ci): substitui placeholders de CODEOWNERS por handles reais"
git push
```

- [ ] Validar no GitHub: `Settings` → `Branches` → editar a regra de `main` → **Require review from Code Owners** continua marcado.

### 3.3 Times (opcional, recomendado se houver org)
Se o repo está numa **organização**, prefira times a indivíduos:
- [ ] Criar times: `carwash-arquitetura`, `carwash-back`, `carwash-front`, `carwash-qa`, `carwash-produto`.
- [ ] Substituir handles individuais por `@org/carwash-arquitetura` etc.

---

## 4. CodeRabbit — code review com IA

CodeRabbit lê cada PR e comenta sugestões/segurança/style. Já temos `.coderabbit.yaml` versionado para regras consistentes.

### 4.1 Instalar o GitHub App
- [ ] Acesse <https://coderabbit.ai/> e clique em **Sign in with GitHub**.
- [ ] Autorize a aplicação.
- [ ] No dashboard do CodeRabbit, clique **Install on GitHub**.
- [ ] Selecione **Only select repositories** → marque `CarWash` → **Install**.
- [ ] Confirme as permissões solicitadas (read code, write PR comments, read metadata).

### 4.2 Plano
- [ ] **Pro Trial / Free for OSS:** se o repo for **público**, o plano free cobre.
- [ ] **Privado:** trial gratuito de 14 dias; depois precisa do plano Pro (~$15/mês por dev). Decisão escalada ao CEO antes de virar pago.

### 4.3 Configuração no repo
O arquivo `.coderabbit.yaml` na raiz já vem com:
- Idioma `pt-BR` (mantém review em português).
- Profile `assertive` (review mais rigoroso, alinhado ao bar técnico do `arquiteto-carwash`).
- Auto-review **ativo** em PRs (não em drafts).
- Path instructions específicas: backend, frontend, migrations, infra, agents.
- Tools habilitadas: ESLint, Markdownlint, Hadolint (Dockerfile), Gitleaks (segredos), Semgrep (segurança).

- [ ] Verifique no dashboard do CodeRabbit que o repo aparece com status `Active`.

### 4.4 Integração com branch protection
- [ ] Após o **primeiro PR**, o status check do CodeRabbit aparece na lista. Volte ao passo 2.2 e adicione `CodeRabbit` aos required status checks.

### 4.5 Uso no PR
- Comentários do CodeRabbit aparecem inline.
- Reviewers humanos **não substituem** o CodeRabbit — ele complementa, não decide aprovação.
- Para conversar com o bot dentro do PR, comente: `@coderabbitai <pergunta>`.
- Para pular review num PR específico: comente `@coderabbitai pause`.
- Para forçar reanálise: `@coderabbitai full review`.

---

## 5. Labels — padronização

**Caminho:** `Issues` → `Labels` → `New label`

Crie esses labels (ou edite os existentes para bater com nomes):

### 5.1 Tipo de mudança (espelha Conventional Commits)
- [ ] `type: feat` — `#0E8A16`
- [ ] `type: fix` — `#D93F0B`
- [ ] `type: docs` — `#0075CA`
- [ ] `type: refactor` — `#A2EEEF`
- [ ] `type: test` — `#FBCA04`
- [ ] `type: chore` — `#CCCCCC`
- [ ] `type: build` — `#5319E7`
- [ ] `type: ci` — `#5319E7`
- [ ] `type: perf` — `#FFA500`

### 5.2 Escopo do monolito
- [ ] `scope: back` — `#1D76DB`
- [ ] `scope: front` — `#BFD4F2`
- [ ] `scope: db` — `#5319E7`
- [ ] `scope: infra` — `#000000`
- [ ] `scope: ci` — `#0E8A16`
- [ ] `scope: docs` — `#0075CA`
- [ ] `scope: agents` — `#7057FF`

### 5.3 Operacional / fluxo
- [ ] `needs-qa` — `#D4C5F9` — exige aprovação do `qa-carwash` antes do merge.
- [ ] `coordinated-change` — `#FF6F00` — PR que cruza front+back; precisa do par.
- [ ] `hotfix` — `#B60205` — correção urgente em produção.
- [ ] `breaking` — `#B60205` — quebra contrato (API, schema, env).
- [ ] `blocked` — `#000000` — esperando algo externo (validação do proprietário, decisão jurídica).
- [ ] `do-not-merge` — `#B60205` — temporário; nunca mergear com este label.

### 5.4 Rastreabilidade ao DRP
- [ ] `must-have` — `#0E8A16`
- [ ] `should-have` — `#FBCA04`
- [ ] `could-have` — `#CCCCCC`
- [ ] `wont-have` — `#000000`

### 5.5 Comando rápido (gh CLI)

```bash
# rode na raiz do repo, autenticado com gh
for L in \
  "type: feat|0E8A16" "type: fix|D93F0B" "type: docs|0075CA" "type: refactor|A2EEEF" \
  "type: test|FBCA04" "type: chore|CCCCCC" "type: build|5319E7" "type: ci|5319E7" "type: perf|FFA500" \
  "scope: back|1D76DB" "scope: front|BFD4F2" "scope: db|5319E7" "scope: infra|000000" \
  "scope: ci|0E8A16" "scope: docs|0075CA" "scope: agents|7057FF" \
  "needs-qa|D4C5F9" "coordinated-change|FF6F00" "hotfix|B60205" "breaking|B60205" \
  "blocked|000000" "do-not-merge|B60205" \
  "must-have|0E8A16" "should-have|FBCA04" "could-have|CCCCCC" "wont-have|000000"; do
  IFS='|' read -r name color <<< "$L"
  gh label create "$name" --color "$color" --force >/dev/null && echo "✓ $name"
done
```

---

## 6. Actions — secrets e permissões

**Caminho:** `Settings` → `Secrets and variables` → `Actions`

### 6.1 Secrets (Repository secrets)
Por enquanto o CI só precisa destes (para o job `docker` validar compose):
- (Nada obrigatório no momento — os jobs usam valores fake `ci-only`).

Quando ativar deploy real, adicionar conforme necessidade:
- [ ] `JWT_SECRET_PROD` — segredo de assinatura JWT em produção (NÃO reutilize o de CI).
- [ ] `POSTGRES_PASSWORD_PROD` — senha do banco em produção.

### 6.2 Permissions for GITHUB_TOKEN
**Caminho:** `Settings` → `Actions` → `General` → `Workflow permissions`
- [ ] **Read and write permissions** — ✅ (necessário para o CodeRabbit comentar).
- [ ] **Allow GitHub Actions to create and approve pull requests** — opcional; deixe ❌ por padrão.

### 6.3 Required workflows (organização)
Se o repo está em uma org com plano Team/Enterprise:
- [ ] Marcar `.github/workflows/ci.yml` como **required workflow** para garantir que ninguém desativa por engano.

---

## 7. Default branch

**Caminho:** `Settings` → `General` → `Default branch`
- [ ] Confirmar que **`main`** é o default. (Se vier `master`, renomeie.)

---

## 8. Templates de issue (opcional, recomendado)

**Caminho:** criar `.github/ISSUE_TEMPLATE/` localmente.

Sugestão de templates (não criados ainda — me avise se quiser que eu gere):
- [ ] `bug_report.yml` — campos: descrição, passos, esperado vs observado, ambiente, RF/RN afetado.
- [ ] `feature_request.yml` — campos: problema (P1–P7), proposta, RF candidato, prioridade MoSCoW.
- [ ] `requirement_gap.yml` — campos: doc fonte, ambiguidade, impacto, decisão pendente (PO/PM).

---

## 9. GitHub Projects — board do PO/PM (opcional, recomendado)

**Caminho:** `Projects` → `New project` → **Board**.

### 9.1 Estrutura sugerida
- **Colunas:** `Backlog` → `Refinamento` → `Pronto p/ Sprint` → `Em desenvolvimento` → `Code Review` → `QA` → `Done`.
- **Campos custom:**
  - `RF` (texto) — RF do DRP.
  - `Prioridade` (single select) — Must / Should / Could / Won't.
  - `Esforço` (single select) — PP / P / M / G / GG.
  - `Sprint` (iteration) — sprints quinzenais.

### 9.2 Automação
- [ ] PR aberto → move card para `Code Review`.
- [ ] PR mergeado → move para `Done`.
- [ ] Issue criada com label `must-have` → entra em `Backlog` automaticamente.

---

## 10. Verificação final

Depois de tudo configurado, abra um PR de teste:

```bash
git checkout -b chore/agents-test-config
echo "# teste" > /tmp/test.txt
git add /tmp/test.txt   # falhará — só para validar
# ou faça uma mudança real pequena, ex: typo em CONTRIBUTING.md
git commit -m "docs(docs): corrige typo em CONTRIBUTING"
git push -u origin chore/agents-test-config
gh pr create --fill
```

Confirme:
- [ ] CI roda os 5 jobs (`commit-conventions`, `frontend`, `backend` skipped sem .sln, `docker`, `spell`).
- [ ] CodeRabbit comenta o PR.
- [ ] CODEOWNERS atribui o reviewer correto automaticamente.
- [ ] Não consegue mergear sem aprovação + status checks.
- [ ] Após merge, branch é deletada automaticamente.
- [ ] Histórico do `main` segue linear (sem merge commits).

Se passou em todos os checks: **configuração concluída.** Salve este documento atualizado no repo.

---

## 11. Manutenção

- Revise branch protection trimestralmente — novos jobs do `ci.yml` precisam ser adicionados aos required checks.
- Atualize CODEOWNERS quando alguém entra/sai do time.
- `.coderabbit.yaml` evolui junto com `CONTRIBUTING.md` — se mudar regra de commit/branch, ajuste o yaml também.

> Dúvidas: `arquiteto-carwash` (config técnica) ou `ceo-carwash` (decisões com custo, ex: plano pago do CodeRabbit).
