# Relatório Consolidado de QA — Backend CarWash (v4 — pós terceira iteração de fix)

Data: 2026-05-17T18:30Z
Backend: `http://localhost:8080`
Migrations aplicadas: 4 (`InitialSchema`, `AddUsuarioLockoutFields`, `RefatoraClienteEndereco`, `AdicionaAuditoriaUsuarioCliente`).
Rodadas anteriores: `QA/_relatorios/v1-pre-fix/`, `v2-pos-fix1/`, `v3-pos-fix2/`.

---

## TL;DR

- **98% PASS (154/157)** — apenas **1 FAIL** real (BUG-TENANT-CLI · decisão arquitetural) e **2 SKIP** (T4/T5 Health · exigem derrubar Postgres com agentes paralelos).
- **Auth: 30/30 (100%)** · **Usuários: 41/41 (100%)** · **Clientes Write: 47/47 (100%)** · **Clientes Read: 27/28** · **Health: 9/11**.
- **Todos os 13 bugs corrigidos na iteração anterior foram CONFIRMADOS fechados** — sem regressão.
- **2 bugs novos descobertos pelo agente Read** durante seed de dados: BUG-POST-DATANASC-NULL (500 em POST sem `dataNascimento`) e BUG-PUT-ATIVO-IGNORADO (PUT descarta `ativo` silenciosamente).
- **3 itens permanecem em aberto por decisão de produto/arquitetura**: BUG-LGPD-CLI (máscara de PII), BUG-TENANT-CLI (multi-tenant), BUG-CONTRATO-404-ROUTE (padronizar 404).
- **1 gap depende de feature**: GAP-CW-CLI-STA-AGD (UC003/UC004 agendamentos).

---

## Evolução de qualidade ao longo das rebaterias

| Área | v1 | v2 | v3 | v4 | v1→v4 |
|---|---:|---:|---:|---:|---:|
| Auth | 13/30 | 28/30 | 29/30 | **30/30** | +17 |
| Usuários | 19/41 | 36/41 | 41/41 | **41/41** | +22 |
| Clientes Read | 6/28 | 6/28 | 22/28 | **27/28** | +21 |
| Clientes Write | 5/47 | 21/47 | 46/47 | **47/47** | +42 |
| Health | 9/11 | 9/11 | 9/11 | 9/11 | 0 |
| **Total** | **52 (33%)** | **100 (64%)** | **147 (94%)** | **154 (98%)** | **+102** |

---

## Bugs FECHADOS confirmados na v4 ✅ (sem regressão)

### Da terceira iteração de fix (v3 → v4)
| ID | Confirmação v4 |
|---|---|
| BUG-010 (Auth) | 10/10 runs paralelos com 1×200 + 1×401 (lock pessimista `FOR UPDATE`) |
| BUG-006 / BUG-U006 | `title: "Corpo da requisição inválido..."` em body errors; `title: "Identificador inválido."` apenas em path; sem vazamento de parâmetro C# |
| BUG-007 | `Logout efetuado. UsuarioId=<guid>, SessaoId=<guid>`; sem cookie → silencioso |
| GAP-CW-CLI-AUDIT (create) | `criado_por_usuario_id` populado com `sub` do JWT |
| GAP-CW-CLI-AUDIT (update) | `atualizado_por_usuario_id` atualizado; `criado_por_usuario_id` imutável |
| GAP-CW-CLI-STA-EMP | PATCH `{}` → 400 + `errors.ativo` |
| GAP-CW-CLI-EMAIL-1 | POST 2 clientes mesmo email → 1×201 + 1×409 `cliente-email-duplicado` |
| GAP-CW-CLI-PUT-EML | PUT alterando email para um já cadastrado → 409 |
| GAP-CW-CLI-PUT-CPF | PUT com `cpf`/`cnpj` no body → warning Serilog estruturado (Opção B) |
| BUG-CACHE-PII | GET clientes/{id} → `Cache-Control: no-store` |
| BUG-FILTRO-BUSCA-IGNORADO | `?busca=' OR 1=1 --` → Total=0 |
| BUG-BUSCA-DADO-INSUSPEITO | `?busca=xyzabc123notexist` → Total=0 |
| GAP-UNACCENT-ASSIM | `?busca=joão` casa `Joao Silva` e variantes (simétrico) |
| GAP-PAG-0 | `?pagina=0` → 400 + `errors.pagina` |
| GAP-CLAMP | `?tamanhoPagina=10000` → 400 + `errors.tamanhoPagina` |

### Acumulado das iterações anteriores (também confirmados sem regressão na v4)
BUG-001, BUG-002, BUG-005, BUG-008, BUG-009, BUG-010 (Clientes), BUG-Auth-Lockout, BUG-U001, BUG-U002, BUG-U003, BUG-U004, BUG-U007, BUG-U009, BUG-CR002.

---

## Bugs NOVOS descobertos na v4

### BUG-POST-DATANASC-NULL — POST sem `dataNascimento` → 500 com stack visível (MÉDIO)
- **Sintoma**: `curl POST /api/v1/clientes` omitindo `dataNascimento` → 500 com stack `System.InvalidOperationException: Nullable object must have a value at ClienteService.cs:81 (request.DataNascimento!.Value)`.
- **Causa**: o validator não exige presença explícita do campo; o service usa `!.Value` sem null-check.
- **Esperado**: 400/422 com `errors.dataNascimento`.
- **Fix**: adicionar `RuleFor(x => x.DataNascimento).NotNull()` no `CreateClienteRequestValidator` (e `UpdateClienteRequestValidator`).

### BUG-PUT-ATIVO-IGNORADO — PUT descarta `ativo` silenciosamente (BAIXO)
- **Sintoma**: `PUT /clientes/{id}` com `{"ativo":false}` → 200 mas response volta `"ativo":true` e DB permanece `ativo=t`.
- **Causa**: DTO `UpdateClienteRequest` não tem `Ativo` (modificação de status só via PATCH `/status`). Body é descartado silenciosamente.
- **Fix sugerido**: mesma estratégia do BUG-GAP-CW-CLI-PUT-CPF — warning estruturado no log quando campo "extra" é detectado (consistência), ou rejeitar com 400.

---

## Itens ainda em aberto (fora de escopo técnico)

### Decisão de produto (PO)
- **BUG-LGPD-CLI** — Máscara de CPF/CNPJ/email/celular/endereço/dataNascimento nas respostas. Hoje todos em claro para qualquer usuário autenticado.
- **BUG-CONTRATO-404-ROUTE** — Padronizar 404 (route constraint sem body vs id inexistente com ProblemDetails).

### Decisão arquitetural
- **BUG-TENANT-CLI** — Estratégia de multi-tenant. Funcionario lê cliente criado pelo admin. Sem `tenant_id`/`filial_id` em `clientes`. Único FAIL ativo da v4 (T8 byid Read).

### Dependente de outra feature
- **GAP-CW-CLI-STA-AGD** — Bloquear desativação de cliente com agendamentos abertos (depende de UC003/UC004).

---

## Health (sem mudanças)

9/11 PASS · 2 SKIP (T4/T5 — DB down impactaria agentes paralelos). Latência: `/health` 1.5-7ms (v3: 1.0-1.7ms), `/health/ready` ≈ 2ms. 50× paralelo em `/health/ready` → 100% 200.

---

## Detalhe por área

### Auth (PASS=30, FAIL=0)
| Endpoint | v3 | v4 |
|---|---:|---:|
| POST /login (13) | 13 | 13 |
| POST /refresh (10) | 9 | **10** |
| POST /logout (7) | 7 | 7 |

R-T10 (race paralelo) saiu de FAIL → PASS após o fix do BUG-010. Lockout, family revogation, Cache-Control e ProblemDetails todos confirmados.

### Usuários (PASS=41, FAIL=0)
Os 6 casos que em v3 eram "PASS com defeito cosmético" (PASS*) agora são PASS limpos pós BUG-U006 fechado. Race condition POST T14 confirma 1 linha única no DB. Auto-desativação admin → 409.

### Clientes Read (PASS=27, FAIL=1)
| Endpoint | v3 | v4 |
|---|---:|---:|
| GET listar (17) | 14 | **17** |
| GET /{id} (11) | 8 | **10** |

Único FAIL: T8 byid (cross-user read · BUG-TENANT-CLI). 2 bugs colaterais descobertos durante seed: BUG-POST-DATANASC-NULL e BUG-PUT-ATIVO-IGNORADO (do escopo Write).

### Clientes Write (PASS=47, FAIL=0)
| Endpoint | v3 | v4 |
|---|---:|---:|
| POST (20) | 19 | **20** |
| PUT (13) | 13 | 13 |
| PATCH status (14) | 14 | 14 |

POST T11 (email duplicado) saiu de "PASS com gap" para PASS limpo (`cliente-email-duplicado` ativo). Auditoria CRUD: `criado_por_usuario_id` e `atualizado_por_usuario_id` populados/imutáveis conforme esperado.

### Health (PASS=9, SKIP=2)
Sem regressão. T4/T5 SKIP novamente (4 outros agentes paralelos).

---

## Próximos passos sugeridos

### Prioridade 1 — bugs novos descobertos na v4 (curto, ~30min)
1. **BUG-POST-DATANASC-NULL** — adicionar `NotNull` no validator de `DataNascimento`. Reproduzir, corrigir, testar.
2. **BUG-PUT-ATIVO-IGNORADO** — warning Serilog quando `ativo` aparece no PUT (consistência com `cpf`/`cnpj`).

### Prioridade 2 — decisões pendentes (semana seguinte)
3. **BUG-LGPD-CLI** — acionar `po-pm-carwash` para definir quais campos mascarar e a quem expor PII completa (claim/role).
4. **BUG-TENANT-CLI** — acionar `arquiteto-carwash` para definir estratégia (coluna `filial_id` em `clientes` + filtro automático no DbContext baseado em claim).
5. **BUG-CONTRATO-404-ROUTE** — produto decide se uniformiza 404 (preferência: ProblemDetails sempre).

### Prioridade 3 — defesa em profundidade
6. Adicionar suíte CI `[Trait("CA","011")]` em `WebApplicationFactory` + Testcontainers cobrindo:
   - Race `Task.WhenAll(2× /refresh)` (regressão de BUG-010).
   - Schema consistency: `GetPendingMigrationsAsync()` vazio.
   - Filtro busca: termos suspeitos → Total=0.
   - Email único em POST/PUT cliente.
   - PATCH cliente status `{}` → 400.
   - Auditoria `criado_por_usuario_id` = `sub` do JWT.
   - Tudo dos 13 bugs já fechados — congelar via testes para evitar regressão silenciosa.

### Prioridade 4 — backlog
7. GAP-CW-CLI-STA-AGD: aguarda feature de agendamento.
8. T4/T5 Health: executar em janela isolada.
9. Integration tests com Testcontainers (hoje exigem Docker socket dentro do container — quebra dev loop).

---

**Status final v4**: **release praticamente liberado para os módulos Auth/Usuários/Clientes** — pendem apenas as 2 decisões de produto (LGPD) e arquitetura (multi-tenant). Sem essas decisões, recomendo NÃO subir para homologação aberta com dados reais por causa do PII em claro.
