# Relatório — Clientes Write (POST, PUT, PATCH /{id}/status)

Data: 2026-05-17T14:24:00Z
Backend: http://localhost:8080
Pré-condição: **NÃO obteve token** — login `/api/v1/auth/login` retorna **500** (bug pré-conhecido confirmado).

## Sumário

- **Total de casos:** 47 (POST=20, PUT=13, PATCH=14)
- **PASS:** 5 (apenas os que dispensam autenticação: 401/404 antes do `[Authorize]`)
- **FAIL:** 0 verificados (todos os comportamentos com bug estão atrás do auth → BLOCKED)
- **BLOCKED:** 42
- **Bugs novos abertos:** 1 (BUG-CW-AUTH-001 — bloqueador)
- **Gaps pré-conhecidos:** não puderam ser confirmados (dependem de token)

> Os arquivos de QA `POST_clientes.md`, `PUT_cliente.md`, `PATCH_cliente_status.md` foram lidos na íntegra. Os 47 casos foram avaliados; a esmagadora maioria depende de `Authorization: Bearer <token>` em endpoint `[Authorize]`, e o login está quebrado. O efeito cascata é descrito em **BUG-CW-AUTH-001**.

---

## Bugs

### BUG-CW-AUTH-001 — `POST /api/v1/auth/login` retorna 500: coluna `usuarios.bloqueado_ate` não existe

- **Severidade:** **BLOQUEADOR** (impede 100% da homologação de QA write — clientes, veículos, agendamentos, qualquer write protegida por `[Authorize]`).
- **Endpoint:** `POST http://localhost:8080/api/v1/auth/login`.
- **Sintoma:** request retorna `500 Internal Server Error` com `ProblemDetails` genérico (`title: "Não foi possível concluir a operação no momento. Tente novamente."`, `correlationId`). Nenhum login possível.
- **Causa raiz:** o `LoginHandler` (`backend/src/CarWash.Application/Auth/Login/LoginHandler.cs:85`) executa um `SingleOrDefault` que **projeta** as colunas `u.bloqueado_ate` e `u.tentativas_invalidas` (campos do mecanismo de lockout — RN016/segurança), mas a tabela `public.usuarios` no banco do container ainda está na migration inicial (`20260513114525_InitialSchema`) e **NÃO possui essas colunas**. Houve evolução do modelo/entidade `Usuario` no código (commits `0b3a202` "rate-limit /auth/login + Retry-After no lockout" e `ed95f4d` "RF014 CRUD completo") sem a migration de adição de colunas correspondente, ou a migration existe localmente mas não foi aplicada ao container.
- **Erro Postgres:** `42703: column u.bloqueado_ate does not exist` (vide trecho de log em "Log" abaixo).
- **Casos afetados:**
  - **POST /api/v1/clientes:** T1, T2, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14 (a/b/c), T15, T16, T17, T18, T19, T20 — 18 BLOCKED. T3 e T4 passam.
  - **PUT /api/v1/clientes/{id}:** T1, T2, T3, T6, T7, T8, T9, T10, T11, T12, T13 — 11 BLOCKED. T4 e T5 passam.
  - **PATCH /api/v1/clientes/{id}/status:** T1, T2, T3, T4, T7, T8, T9, T10, T11, T12, T13, T14 — 12 BLOCKED. T5 e T6 passam.
- **Reprodução (curl):**

  ```bash
  curl -i -X POST http://localhost:8080/api/v1/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"admin@carwash.local","senha":"DevSeedAdmin2026!Forte"}'
  ```

  Resposta:

  ```
  HTTP/1.1 500 Internal Server Error
  Content-Type: application/problem+json
  X-Correlation-Id: 0807572c79ab4d47a9239b440dd6e6d6

  {"type":"https://carwash/errors/internal-error",
   "title":"Não foi possível concluir a operação no momento. Tente novamente.",
   "status":500,
   "correlationId":"0807572c79ab4d47a9239b440dd6e6d6"}
  ```

- **Log Serilog (trecho):**

  ```
  [14:23:38 ERR] Failed executing DbCommand (1ms) [Parameters=[@__emailNormalizado_0='?']]
  SELECT u.id, u.ativo, u.atualizado_em, u.bloqueado_ate, u.criado_em,
         u.email, u.nome, u.perfil, u.senha_hash, u.tentativas_invalidas
  FROM public.usuarios AS u
  WHERE u.email = @__emailNormalizado_0
  LIMIT 1
  [14:23:38 ERR] Npgsql.PostgresException 42703: column u.bloqueado_ate does not exist
    at CarWash.Application.Auth.Login.LoginHandler.HandleAsync(...) LoginHandler.cs:85
    at CarWash.Api.Endpoints.Auth.AuthEndpoints.LoginAsync(...) AuthEndpoints.cs:66
  [14:23:38 ERR] Falha não tratada. CorrelationId=0807572c79ab4d47a9239b440dd6e6d6
  ```

- **Evidência DB (schema atual da tabela `usuarios`):**

  ```
  Column        | Type
  --------------+--------------------------
  id            | uuid
  nome          | character varying(120)
  email         | character varying(150)
  senha_hash    | text
  perfil        | character varying(20)
  ativo         | boolean
  criado_em     | timestamp with time zone
  atualizado_em | timestamp with time zone
  ```

  Migrations aplicadas no container: somente `20260513114525_InitialSchema`. Não há migration de lockout aplicada.

- **Sugestão de correção (em ordem de preferência):**
  1. Adicionar migration EF `AddLockoutFields` que cria `bloqueado_ate timestamp with time zone NULL` e `tentativas_invalidas integer NOT NULL DEFAULT 0` em `public.usuarios`, e aplicar (`dotnet ef database update`) ou subir container com a migration.
  2. Garantir que o `Program.cs` rode `db.Database.Migrate()` no startup quando `ASPNETCORE_ENVIRONMENT` for `Development`/`Testing` para evitar drift.
  3. Cobrir com teste de integração `LoginHandlerTests.Login_QuandoSchemaSemColunasLockout_RetornaErroClaro` ou — melhor — `MigrationConsistencyTests.Schema_BateComEntidadeUsuario` que falha o CI quando o `DbContext` espera colunas inexistentes.
  4. Em paralelo, registrar mapeamento no `ExceptionHandlingMiddleware` para `Npgsql.PostgresException` com `SqlState=42703` → 500 com hint distinto (`"database schema drift"`) para falhas catastróficas como esta serem detectáveis em smoke.
  5. CA011 quebrado: nenhum CA de fluxo write pode ser homologado sem login. Bloquear release.

---

## POST /api/v1/clientes (20 casos)

| ID  | Descrição                                          | Esperado            | Obtido           | Resultado | Bug                |
|-----|----------------------------------------------------|---------------------|------------------|-----------|--------------------|
| T1  | Golden PF (CPF válido)                             | 201 + id + traceId  | n/a (sem token)  | BLOCKED   | BUG-CW-AUTH-001    |
| T2  | Golden PJ (CNPJ válido)                            | 201                 | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T3  | Sem header Authorization                           | 401 + WWW-Authenticate: Bearer | **HTTP/1.1 401 Unauthorized, WWW-Authenticate: Bearer, Content-Length: 0, X-Correlation-Id presente** | **PASS** | — |
| T4  | Bearer expirado                                    | 401                 | n/a (não tenho token sequer válido para gerar expirado real) | BLOCKED   | BUG-CW-AUTH-001    |
| T5  | Bearer válido de outro usuário                     | 201                 | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T6  | CPF com DV errados                                 | 400 errors.cpf      | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T7  | CNPJ inválido (zeros)                              | 400 errors.cnpj     | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T8  | CPF com máscara                                    | 400                 | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T9  | CPF duplicado                                      | 409                 | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T10 | Email malformado                                   | 400 errors.email    | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T11 | Email duplicado em outro cliente (gap T11)         | 201 (gap)           | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T12 | Celular 10 dígitos                                 | 400                 | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T13 | Telefone fixo com máscara                          | 400                 | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T14a | Nome vazio                                        | 400                 | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T14b | Nome 2 chars                                      | 400                 | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T14c | Nome 101 chars                                    | 400                 | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T15 | Body vazio `{}`                                    | 400 lista de campos | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T16 | JSON malformado                                    | 400                 | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T17 | Whitespace + UF minúsculo                          | trim/normalização   | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T18 | Acentos no nome                                    | 201, UTF-8 íntegro  | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T19 | Race: 2 POSTs simultâneos mesmo CPF                | 1×201 + 1×409       | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T20 | Auditoria `criado_por_usuario_id` = sub do JWT     | match SQL           | n/a              | BLOCKED   | BUG-CW-AUTH-001    |

> Observação T3: a request sem token nem entra no pipeline de validação — pula direto para `401` com `WWW-Authenticate: Bearer` (body vazio, `Content-Length: 0`). Comportamento correto. Não há vazamento de detalhes nem `ProblemDetails`. Encaixa-se no contrato do DRP (autenticação obrigatória) e RNF de segurança.

> Observação T20 não é confirmável sem T1 — fica BLOCKED.

> Observação POST tem 22 linhas no agregado por causa dos sub-casos T14 (a/b/c). Para reporte de "20 casos" conforme tabela do QA, T14 conta como 1 caso BLOCKED.

---

## PUT /api/v1/clientes/{id} (13 casos)

| ID  | Descrição                                          | Esperado            | Obtido           | Resultado | Bug                |
|-----|----------------------------------------------------|---------------------|------------------|-----------|--------------------|
| T1  | Golden path — body completo                        | 200 + ClienteResponse | n/a            | BLOCKED   | BUG-CW-AUTH-001    |
| T2  | Body parcial (sem `nome`)                          | 400                 | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T3  | Id Guid inexistente                                | 404                 | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T4  | Id não-Guid (`abc`)                                | 404 (route constraint) | **404 (X-Correlation-Id presente, body vazio)** | **PASS** | — |
| T5  | Sem `Authorization`                                | 401                 | **401 (WWW-Authenticate: Bearer)** | **PASS** | — |
| T6  | Body vazio `{}`                                    | 400 lista           | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T7  | Celular inválido                                   | 400                 | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T8  | Body com `cpf`/`cnpj` (gap UX)                     | 200 (descartado)    | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T9  | Email duplicado entre clientes (gap)               | 200 (gap)           | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T10 | Unicode no nome                                    | 200                 | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T11 | Race condition (dois PUTs paralelos)               | last-write-wins     | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T12 | Invariantes de auditoria                           | `criadoEm` imutável; `atualizadoEm` cresce | n/a | BLOCKED | BUG-CW-AUTH-001 |
| T13 | Performance < 500ms                                | HTTP=200, Total<0.5s | n/a            | BLOCKED   | BUG-CW-AUTH-001    |

> Observação T4: rota com id não-Guid retorna `404` ANTES do `[Authorize]`, confirmando que o route constraint `:guid` está corretamente declarado. T5: sem token, retorna `401` com `WWW-Authenticate: Bearer`. Ambos comportam-se conforme spec.

---

## PATCH /api/v1/clientes/{id}/status (14 casos)

| ID  | Descrição                                          | Esperado            | Obtido           | Resultado | Bug                |
|-----|----------------------------------------------------|---------------------|------------------|-----------|--------------------|
| T1  | Desativar cliente ativo                            | 200, `ativo:false`  | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T2  | Reativar cliente inativo                           | 200, `ativo:true`   | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T3  | Toggle repetido (idempotência)                     | 200/200             | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T4  | Id Guid inexistente                                | 404                 | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T5  | Id não-Guid (`abc`)                                | 404 (route constraint) | **404 (X-Correlation-Id presente)** | **PASS** | — |
| T6  | Sem `Authorization`                                | 401                 | **401 (WWW-Authenticate: Bearer)** | **PASS** | — |
| T7  | Body ausente (sem `--data`)                        | 400 (mapeamento de `ArgumentNullException`) | n/a | BLOCKED | BUG-CW-AUTH-001 |
| T8  | `ativo: null`                                      | 400                 | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T9  | `ativo: "sim"`                                     | 400                 | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T10 | Body `{}` (gap conhecido — desativa silenciosamente)| 400 (atual: 200)   | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T11 | Campo extra + mass assignment                      | 200, ignora extras  | n/a              | BLOCKED   | BUG-CW-AUTH-001    |
| T12 | Desativar cliente com agendamentos ativos          | 409 (esperado) / 200 (atual gap) | n/a   | BLOCKED   | BUG-CW-AUTH-001    |
| T13 | Log com `TraceId`/`UsuarioId`                      | linha estruturada presente | n/a       | BLOCKED   | BUG-CW-AUTH-001    |
| T14 | Race (PATCH paralelos opostos)                     | last-write-wins, sem 500 | n/a         | BLOCKED   | BUG-CW-AUTH-001    |

> Observação T5: PATCH com id não-Guid também devolve `404` por route constraint, antes do auth. T6: sem token devolve `401` + `WWW-Authenticate: Bearer`. Ambos OK.

---

## Limpeza

Não foi possível criar clientes de teste (login bloqueado), portanto não há registros `teste-%@qa.local` a limpar no banco. Nenhum `DELETE` executado.

---

## Recomendação ao gestor da sprint

1. Tratar **BUG-CW-AUTH-001** como **bloqueador imediato**. Sem login, nenhum CA write é homologável e o CA011 do MVP fica retido.
2. Aplicar a migration de lockout em `public.usuarios` (ou criar uma se nunca tiver sido gerada), redeploy do container `carwash-backend` apontando para o mesmo banco já populado pelo seed (preserva `admin@carwash.local`).
3. Reabrir esta suíte de QA `clientes-write` integralmente após o fix — os 42 casos BLOCKED viram PASS/FAIL conforme o caso, e os gaps pré-conhecidos (POST T11 email duplicado, PUT T8/T9 cpf-cnpj-email, PATCH T10 body vazio, PATCH T12 sem RN para agendamentos abertos) precisam ser confirmados e abertos como issues separadas.
4. Adicionar teste de integração com Testcontainers que dispara `dotnet ef database update` no banco em uma collection fixture e roda `LoginHandler` em request real — esta classe de bug nunca mais deveria chegar em homologação.
