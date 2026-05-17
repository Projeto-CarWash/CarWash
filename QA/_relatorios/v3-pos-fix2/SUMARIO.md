# Relatório Consolidado de QA — Backend CarWash (v3 — pós segunda iteração de fix)

Data: 2026-05-17T17:30Z
Backend: `http://localhost:8080`
Migrations aplicadas: `20260513114525_InitialSchema`, `20260517022432_AddUsuarioLockoutFields`, `20260517061810_RefatoraClienteEndereco`.
Rodadas anteriores: `QA/_relatorios/v1-pre-fix/`, `QA/_relatorios/v2-pos-fix1/`.

---

## TL;DR

- **PASS subiu de 64% (v2) → 94% (147/157) na v3.**
- **Usuários atingiu 100% PASS** (41/41).
- **Clientes destravados pela migration `RefatoraClienteEndereco`**: Read 22/28, Write 46/47.
- **Bug crítico novo (BUG-010)**: race em `/refresh` paralelo emite 2× 200 em 50% das tentativas — regressão do fix de BUG-008 (revogação de família), provavelmente faltou lock pessimista. Quebra single-use enforcement.
- **9 gaps confirmados em runtime** (eram BLOCKED na v2) e **3 bugs novos descobertos** em Clientes Read.
- **2 bloqueadores LGPD** seguem em aberto: PII em claro nas respostas de Clientes e ausência de filtro de tenant (RN multiunidade).

---

## Evolução v1 → v2 → v3

| Área | v1 PASS | v2 PASS | v3 PASS | Δ total |
|---|---:|---:|---:|---:|
| Auth | 13/30 | 28/30 | **29/30** | +16 |
| Usuários | 19/41 | 36/41 | **41/41 (100%)** | +22 |
| Clientes Read | 6/28 | 6/28 | **22/28** | +16 |
| Clientes Write | 5/47 | 21/47 | **46/47 (98%)** | +41 |
| Health | 9/11 | 9/11 | 9/11 | 0 |
| **Total** | **52 (33%)** | **100 (64%)** | **147 (94%)** | **+95** |

---

## Bugs fechados confirmados nesta rebateria ✅

| Bug | Confirmação v3 |
|---|---|
| BUG-005 | `Cache-Control: no-store` presente em todos os 401 do `/refresh` |
| BUG-008 | 3ª chamada com cookie rotacionado → 401 (`SessoesAfetadas=8` no log) |
| BUG-Auth-Lockout | 401×3 + 403 com header HTTP `Retry-After: 900` |
| BUG-U003 | POST sem `perfil` → 400 + `errors.perfil` |
| BUG-U004 | PATCH `{}` → 400 + `errors.ativo` |
| BUG-U007 | `errors.perfil` agora aparece no body `{}` |
| BUG-U009 | Auto-desativação admin → 409 `auto-desativacao-bloqueada` |
| BUG-009 | Schema `clientes` com `data_nascimento`, `endereco_*`, `celular NOT NULL` |
| BUG-010 (Clientes) | GET cliente inexistente → 404 + ProblemDetails (não mais 500) |
| BUG-CR002 | `?ativo=xyz` → 400 binding limpo |

---

## Bugs CRÍTICOS ainda abertos

### 🚨 BUG-010 (Auth) — Race em refresh paralelo: 2× 200 com mesmo token

- **Severidade**: CRÍTICA · single-use enforcement quebrado · regressão do fix de BUG-008.
- **Sintoma**: duas chamadas paralelas `/refresh` com o mesmo cookie. Em 10 runs:
  - 5/10 → **200/200** (BUG: dois tokens válidos derivados da mesma sessão)
  - 3/10 → **401/401** (estado degenerado também inválido)
  - 2/10 → **200/401** (esperado)
- **Evidência (log)**: duas linhas `"Sessão renovada. SessaoAnterior=be7e1a42-..., SessaoNova=<distinta>"` no mesmo timestamp — duas transações leram a sessão antes da revogação ser commitada.
- **Fix**: `SELECT ... FOR UPDATE` (lock pessimista) na sessão ativa, OU `RowVersion` (optimistic concurrency) com retry → 401 em conflito. Cobrir com teste `Task.WhenAll(2 chamadas)` assert exatamente 1×200 + 1×401.
- **Impacto**: atacante com cookie roubado dispara 2 chamadas paralelas e obtém 2 access tokens. Quebra CA011.

---

## Bugs ALTOS ainda abertos

### BUG-LGPD-CLI — PII em claro nas respostas (LGPD)
- Listagem expõe `cpf`, `cnpj`, `celular`, `email` em texto claro.
- GET/{id} expõe acima + `dataNascimento`, `endereco.{cep, logradouro, numero, bairro, cidade, uf}`, `telefone`.
- **Fix**: DTO de listagem sem CPF/CNPJ (ou mascarado); DTO de detalhe condicionado a claim `clientes:ver-pii`; logar acesso a PII. **Decisão de produto necessária**.

### BUG-TENANT-CLI — Ausência de filtro de tenant
- Funcionario `qa-userb-readv3@qa.local` leu cliente criado pelo admin com body completo + PII.
- Schema de `clientes` **não tem** `tenant_id`/`filial_id`.
- **Fix**: decisão arquitetural — coluna `filial_id` em `clientes` + filtro automático no DbContext baseado em claim do JWT, OU policy de autorização. **Decisão de arquitetura necessária**.

### BUG-CACHE-PII — Ausência de `Cache-Control: no-store` na resposta de PII
- Nenhum header `Cache-Control`, `Pragma` ou `Expires` em GET `/clientes/{id}`.
- **Fix**: adicionar `Cache-Control: no-store` (ou `private`) nos endpoints com PII.

### BUG-FILTRO-BUSCA-IGNORADO — Termos suspeitos zeram o filtro silenciosamente
- `?busca=' OR 1=1 --` retorna `Total=31` (universo completo).
- Também `?busca=OR 1=1` (Total=31) e `?busca=   ` (whitespace).
- Não é SQL injection real (sem 500), mas o consumidor pensa que filtrou e recebe tudo.
- **Fix**: rejeitar com 400 quando termo contém só whitespace; tratar termo "exótico" como literal de busca (retornar Total=0 quando não casa). Sem descarte silencioso.

### BUG-BUSCA-DADO-INSUSPEITO — Busca casa em campo não-óbvio
- `?busca=xyzabc123notexist` casa `Cliente B` e `Eduarda Lima`.
- Nome/CPF/email/celular desses clientes não contêm a string. Indica que o `LIKE` toca campo não-visível (id parcial? observações? endereço completo?).
- **Fix**: code review do WHERE no `ClienteService.ListarAsync` para confirmar quais colunas estão na busca e ajustar.

### GAP-CW-CLI-AUDIT-CREATE — Coluna `criado_por_usuario_id` não existe em `clientes`
- Auditoria de criação só no log Serilog (`UsuarioId: 00000000-...`), não persistida.
- CA011 / governança LGPD pendentes.
- **Fix**: migration adicionando `criado_por_usuario_id uuid NULL` + `atualizado_por_usuario_id uuid NULL` + persistir no service.

### GAP-CW-CLI-AUDIT (UPDATE) — Sem `atualizado_por_usuario_id`
- PUT e PATCH atualizam `atualizado_em` mas não persistem quem alterou.
- **Fix**: mesma migration acima + propagação do `usuarioId` no service.

### GAP-CW-CLI-EMAIL-1 + GAP-CW-CLI-PUT-EML — Email duplicado aceito
- POST com email já cadastrado em outro CPF → 201.
- PUT alterando email para um já cadastrado → 200.
- **Fix**: adicionar `ExisteEmailAsync` no service + índice único parcial em `email WHERE email IS NOT NULL` + 409. **Decisão de produto: email é único?**

### GAP-CW-CLI-PUT-CPF — Body com `cpf`/`cnpj` descartado silenciosamente
- PUT com `cpf`/`cnpj` no JSON → 200 + documento original mantido.
- **Fix**: rejeitar com 400 quando campos imutáveis aparecem no body (mais explícito que descartar).

### GAP-CW-CLI-STA-EMP — Body `{}` em PATCH status desativa silenciosamente
- `bool Ativo` não-anulável; body `{}` deserializa para `false` e o handler desativa.
- **Fix**: trocar para `bool?` + validator `NotNull` (mesmo padrão do BUG-U004).

---

## Bugs MÉDIOS/BAIXOS ainda abertos

### BUG-007 (Auth) — Log `Logout efetuado. UsuarioId=null` mesmo com cookie válido
- Auditoria CA011 prejudicada.
- **Fix**: mover log para depois da resolução da sessão; condicionar emissão à existência de match.

### BUG-006 (Auth) — ProblemDetails `title: "Identificador inválido"` para JSON malformado
- **Fix**: handler genérico para `BadHttpRequestException` JSON → `title: "Requisição inválida."` ou `"JSON inválido."`.

### BUG-U006 (Usuários) — Mesma raiz do BUG-006 (resolve junto)

### BUG-CONTRATO-404-ROUTE — Inconsistência de body em 404
- 404 por route constraint → sem body (`Content-Length: 0`).
- 404 por id inexistente → com ProblemDetails.
- **Fix**: padronizar (preferência: usar ProblemDetails sempre).

### GAP-PAG-0 — `pagina<=0` normaliza silenciosamente para 1
- **Fix**: 400 com `errors.pagina`.

### GAP-CLAMP — `tamanhoPagina` clampa internamente mas JSON reflete valor original
- **Fix**: refletir o valor clampado no JSON, ou rejeitar com 400.

### GAP-UNACCENT-ASSIM — Normalização assimétrica
- `busca=joao` casa "João Silva" (unaccent na coluna ✓).
- `busca=joão` NÃO casa "Joao Silva" (sem unaccent no termo ✗).
- **Fix**: aplicar `unaccent(@busca)` no termo antes do LIKE.

### GAP-CW-CLI-STA-AGD — PATCH status sem RN bloqueando cliente com agendamentos abertos
- Sem agendamentos no banco para reproduzir runtime. Service não checa por design.
- **Fix**: depende de feature de agendamento. Reabrir quando UC003/UC004 estiver pronto.

---

## Plano de ação

### Prioridade 1 (paralela, dispara agora)
- **Dev A**: BUG-010 (Auth · race em refresh paralelo). Lock pessimista ou optimistic concurrency em `RefreshTokenService`.
- **Dev B**: BUG-006, BUG-007 (Auth P3). `BadHttpRequestException` handler em ProblemDetails + log Logout com `UsuarioId` correto.
- **Dev C**: Suíte de Clientes — auditoria (coluna+migration+service), email único, body `{}` em PATCH status, cpf/cnpj rejeitado em PUT, cache-control PII, filtro busca, unaccent simétrico, paginação 400, clamp coerente, contrato 404 padronizado, code review da busca insuspeita.

### Prioridade 2 (sprint seguinte — decisão de produto/arquitetura)
- BUG-LGPD-CLI: máscara / claim para PII (PO).
- BUG-TENANT-CLI: estratégia multi-tenant (arquiteto).
- GAP-CW-CLI-STA-AGD: aguarda feature de agendamento (UC003/UC004).

### Prioridade 3 (testes de regressão)
- Suíte `[Trait("CA","011")]` com Testcontainers cobrindo:
  - `Task.WhenAll(2× /refresh)` → 1×200 + 1×401 (regressão BUG-010).
  - Schema consistency: `db.Database.GetPendingMigrationsAsync()` vazio.
  - Filtro busca: `?busca=' OR 1=1 --` → 0 itens.
  - Email duplicado em POST cliente → 409.
  - PATCH cliente status `{}` → 400.
  - Auditoria: `criado_por_usuario_id` = `sub` do JWT.

---

## Health (sem mudanças)

9/11 PASS (mesmas 2 SKIP T4/T5 — DB down impactaria agentes paralelos). Latências marginalmente melhores: `/health` 1.1ms (v2: 1.7-2.1ms), `/health/ready` 1.5ms (v2: 2.1ms). 50× paralelo em `/health/ready` → 100% 200.

---

**Status v3**: release ainda **bloqueado** por BUG-010 (Auth) e gaps LGPD/tenant. Decisões de produto necessárias para BUG-LGPD-CLI; arquitetura para BUG-TENANT-CLI.
