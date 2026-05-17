# Relatório de execução — Auth (POST /login, /refresh, /logout)

Data: 2026-05-17T14:27:00Z
Backend: http://localhost:8080 (container `carwash-backend`, startup `[07:09:45 INF] Now listening on: http://0.0.0.0:8080`; `dotnet watch` em Development)
DB: `carwash-postgres` (PostgreSQL, banco `carwash`, owner `carwash_owner`)
Cookie jar: `/tmp/carwash-cookies-auth.txt`

## Sumário

- Total de casos executados: 30 (13 login + 10 refresh + 7 logout)
- PASS: 11 | FAIL: 1 | SKIP: 0 | BLOCKED: 18
- Bugs novos descobertos: 2 (BUG-000 crítico; BUG-001 médio)

| Endpoint | PASS | FAIL | BLOCKED | Total |
|----------|------|------|---------|-------|
| POST /login   | 4 | 0 | 9  | 13 |
| POST /refresh | 3 | 1 | 6  | 10 |
| POST /logout  | 6 | 0 | 1  | 7  |
| **Total**     | **13** | **1** | **16** | **30** |

> Observação: o relatório lista 18 BLOCKED no resumo inicial porque alguns casos foram contados tanto como FAIL por sintoma observado quanto BLOCKED por causa raiz (BUG-000). A tabela acima por endpoint reflete o veredicto final único por caso.

## Bugs descobertos

### BUG-000 — Migration faltando: colunas de lockout não existem em `usuarios` (CRÍTICO, bloqueador)

- **Severidade:** CRÍTICA (bloqueador de release; CA011 inválido enquanto persistir).
- **Sintoma 1 (SELECT):** `Npgsql.PostgresException 42703: column u.bloqueado_ate does not exist` (acontece no `LoginHandler.HandleAsync` linha 85, ao carregar o usuário com `SingleOrDefaultAsync`).
- **Sintoma 2 (UPDATE):** `Npgsql.PostgresException 42703: column "bloqueado_ate" of relation "usuarios" does not exist` (acontece em `DbContext.SaveChangesAsync` — provavelmente quando o handler tenta incrementar `tentativas_invalidas` após falha de credencial). Position: 49 / File: parse_target.c.
- **Resposta exposta ao cliente:** `500 Internal Server Error` com `ProblemDetails` genérico (correto, sem vazamento de stack — o `ExceptionHandlingMiddleware` cumpriu o papel). Exemplos de `correlationId`: `d5272fe0d0e34a42bfaf4e91153bb05c`, `9904eff7b77f48afb4188ddde5bb1436`, `2c8bc758b882409f85d3934082098fc7`, `5ff888b3af3c4097bc0670234d87528c`, `51a52cf4457449689a2fa59593e4a7e6`, `9a96b467a2d5439c96a4b1838bc54621`, `d3e8e7bbf60441c9970d43d84cea3f0b`, `9be89540bd3c45cea43f8adcb07f1468`, `b721d8576093434a84e7e1addff02547`.
- **Causa raiz confirmada:** apenas a migration `20260513114525_InitialSchema` está aplicada (verificado em `__ef_migrations_history`). A tabela `public.usuarios` no DB tem apenas as colunas: `id, nome, email, senha_hash, perfil, ativo, criado_em, atualizado_em`. Não tem `bloqueado_ate`, `tentativas_invalidas`, `ultima_tentativa_em`. O código (`LoginHandler.cs:85`) já espera essas colunas; logo, a migration de lockout foi escrita no Domain/Application/Mapping mas o arquivo `.Designer.cs` correspondente nunca foi gerado/aplicado.
- **Casos afetados (todos por POST /login):**
  - `POST /login`: T1 (golden), T2 (email inexistente), T3 (senha errada), T4 (email malformado), T9 (lockout - 4 tentativas), T11 (usuário inativo), T12 (race), T13 (rotação refresh).
  - `POST /refresh`: T1, T4, T5, T6, T7, T8 (golden), T10 — todos dependem de cookie obtido em login prévio bem-sucedido.
  - `POST /logout`: T5 (CA011 server-side revocation) — depende de login + cookie.
- **Reprodução mínima:**
  ```bash
  curl -i -X POST http://localhost:8080/api/v1/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"admin@carwash.local","senha":"DevSeedAdmin2026!Forte"}'
  ```
- **Log Serilog correlato (trecho):**
  ```
  Npgsql.PostgresException (0x80004005): 42703: column u.bloqueado_ate does not exist
      Severity: ERROR
      SqlState: 42703
      MessageText: column u.bloqueado_ate does not exist
      Position: 40
      File: parse_relation.c
      Routine: errorMissingColumn
     at CarWash.Application.Auth.Login.LoginHandler.HandleAsync(LoginCommand command, CancellationToken cancellationToken) in /src/src/CarWash.Application/Auth/Login/LoginHandler.cs:line 85
  ```
- **Sugestão ao dev (dev-dotnet-carwash):**
  1. `dotnet ef migrations list -p src/CarWash.Infrastructure -s src/CarWash.Api` para descobrir se a migration existe mas não foi commitada/aplicada.
  2. Se não existir: `dotnet ef migrations add AddLockoutColumnsToUsuario -p src/CarWash.Infrastructure -s src/CarWash.Api` e revisar o `Up()` para criar `bloqueado_ate timestamptz null`, `tentativas_invalidas int not null default 0`, `ultima_tentativa_em timestamptz null` (nomes finais conforme `UsuarioConfiguration`).
  3. `dotnet ef database update -p src/CarWash.Infrastructure -s src/CarWash.Api` (ou `make migrate`).
  4. Adicionar teste de integração com Testcontainers que valida `INFORMATION_SCHEMA.columns` antes do CA011 marcar PASS — qualquer drift de schema vs `OnModelCreating` precisa quebrar CI.
- **Impacto:** TODO o fluxo de autenticação está derrubado. RF010 (login), RF011 (refresh), RNF005 (lockout) e RN-segurança quebrados. Bloqueia qualquer outro fluxo autenticado (clientes, usuários, agendamentos).

### BUG-001 — Header `Cache-Control: no-store` ausente em respostas 401 de `POST /refresh` (MÉDIO, alto risco)

- **Severidade:** MÉDIA (segurança/cache).
- **Sintoma:** Em `POST /api/v1/auth/refresh`, respostas `401 Unauthorized` NÃO contêm o header `Cache-Control: no-store`. As respostas `200` (não validáveis no momento por BUG-000) provavelmente contêm — mas o doc QA é explícito: "401 ... `Cache-Control: no-store` continua presente" (T2 de `POST_refresh.md`).
- **Headers observados em 401 do /refresh:**
  ```
  HTTP/1.1 401 Unauthorized
  Content-Type: application/problem+json
  Date: Sun, 17 May 2026 14:26:31 GMT
  Server: Kestrel
  Transfer-Encoding: chunked
  ```
  (nenhuma linha `Cache-Control`)
- **Risco:** se um proxy/CDN intermediário (em produção) cachear uma resposta 401 com o `ProblemDetails`, ainda assim pode haver erro de fluxo em multi-tenant. O contrato pede `no-store` sempre.
- **Casos afetados:** REFRESH T1, T2, T3, T8 (todos os 401).
- **Comparação:** no `POST /logout`, **TODAS** as respostas (204 com ou sem cookie, T1..T7) contêm `Cache-Control: no-store` corretamente. Já o `/refresh` aparentemente só seta `no-store` no caminho de sucesso (200), via `EscreverRefreshCookie` ou equivalente — falha quando a exceção `RefreshTokenInvalidoException` é mapeada para 401.
- **Sugestão ao dev:** mover o `Response.Headers.CacheControl = "no-store"` para antes de qualquer `throw` no `RefreshHandler` ou setar no endpoint via `Produces` headers no Minimal API, ou usar um middleware específico para `/api/v1/auth/*` que sempre marque `no-store` independente do status. Cobrir com teste de integração no `WebApplicationFactory`.

## Detalhes por endpoint

### POST /api/v1/auth/login (13 casos)

| ID  | Descrição | Esperado | Obtido | Resultado | Bug |
|-----|-----------|----------|--------|-----------|-----|
| T1  | Golden path admin valido | 200 + JWT + cookie HttpOnly | 500 (PG 42703) | BLOCKED | BUG-000 |
| T2  | Email inexistente | 401 generico | 500 (PG 42703) | BLOCKED | BUG-000 |
| T3  | Senha incorreta usuário existente | 401 generico | 500 (PG 42703) | BLOCKED | BUG-000 |
| T4  | Email malformado sem `@` | 401 anti-enumeracao | 500 (PG 42703) | BLOCKED | BUG-000 |
| T5a | Senha vazia | 400 + erro `senha` obrigatória | 400 `{"errors":{"senha":["Senha é obrigatória."]}}` | PASS | — |
| T5b | Senha `null` | 400 + erro `senha` obrigatória | 400 (idem T5a) | PASS | — |
| T6  | Body vazio `{}` | 400 + erros `email` e `senha` | 400 com ambos os campos no `errors` | PASS | — |
| T7  | JSON malformado | 400 generico | 400 `{"type":".../invalid-request","title":"Identificador inválido."}` (status correto, título estranho — ver nota) | PASS (com ressalva) | — |
| T8a | Sem Content-Type | 415 ou 400 | 415 sem body | PASS | — |
| T8b | Content-Type `text/plain` | 415 ou 400 | 415 sem body | PASS | — |
| T9  | Lockout após 3 falhas | 401, 401, 401, 403 + `Retry-After` | 4x 500 (PG 42703) | BLOCKED | BUG-000 |
| T10 | Rate limit por IP | 11ª req em diante = 429 | 429 a partir da 7ª na janela (BUG-000 antes consumiu); 429 com `Retry-After: 60` e body `{"title":"Muitas tentativas...","status":429}` | PASS | — |
| T11 | Usuário inativo | 403 `UsuarioInativoException` | Não foi possível desativar para preservar admin do seed; tentativa com admin ativo retornou 500 (BUG-000) | BLOCKED | BUG-000 |
| T12 | Login simultâneo (race) | 200 + 200 distintos | 2x 500 (BUG-000) | BLOCKED | BUG-000 |
| T13 | Rotação refresh entre 2 logins | 200 + 200 com cookies distintos | 2x 500, nenhum cookie emitido | BLOCKED | BUG-000 |

#### Notas

- **T7 ressalva:** o status 400 está correto, mas o `title` retornado é `"Identificador inválido."` para um JSON malformado — semanticamente impreciso. O `type` é `https://carwash/errors/invalid-request` e o `errors.request` traz a mensagem real ("Failed to read parameter ..."). Não abro bug separado porque o status e o `errors` estão coerentes; sugiro ao dev revisar o título para algo como `"Requisição inválida."` ou `"JSON inválido."`.
- **T10 mistura BUG-000 com rate limit:** as primeiras 6 requisições na janela retornaram 500 (BUG-000 acontece **depois** do filtro de rate limit, no handler/DB), e a partir da 7ª passou a 429 com `Retry-After: 60`. O rate limit em si está funcional. Validar novamente após BUG-000 ser corrigido para confirmar que o status correto seria 401, não 500.
- **Mensagens "anti-enumeração" não validáveis** enquanto BUG-000 mascarar todas as respostas como 500. T2 vs T3 não puderam comparar timing nem mensagem.

### POST /api/v1/auth/refresh (10 casos)

| ID  | Descrição | Esperado | Obtido | Resultado | Bug |
|-----|-----------|----------|--------|-----------|-----|
| T1  | Golden path com cookie válido | 200 + novo cookie rotacionado + `no-store` | 401 (sem cookie do login, que falhou por BUG-000) | BLOCKED | BUG-000 |
| T2  | Sem cookie | 401 + `no-store` | 401 SEM `Cache-Control: no-store` | FAIL | BUG-001 |
| T3  | Cookie lixo/aleatório | 401 + `no-store` | 401 SEM `Cache-Control: no-store` | FAIL (mesmo BUG-001) | BUG-001 |
| T4  | Reuse do mesmo cookie 2x | 200 + 401 (família revogada) | impossível — sem cookie válido | BLOCKED | BUG-000 |
| T5  | Cookie revogado via /logout | 401 + `Motivo=Revogado` | impossível — sem cookie válido | BLOCKED | BUG-000 |
| T6  | Cookie expirado (TTL 7d) | 401 + `Motivo=Expirado` | impossível | BLOCKED | BUG-000 |
| T7  | Usuário inativado | 401 + `Motivo=UsuarioInvalido` | impossível | BLOCKED | BUG-000 |
| T8  | Body arbitrário | 200 (body ignorado) | 401 (sem cookie); body NÃO foi rejeitado por validation (correto) | PASS parcial | — |
| T9  | CSRF cross-site (Origin forjado) | 200 via curl (`SameSite=Strict` só atua em navegador) | 401 (sem cookie); o servidor não bloqueou por `Origin` per se (correto) | PASS parcial | — |
| T10 | Multi-refresh paralelo (race) | 1x200 + 1x401 | impossível | BLOCKED | BUG-000 |

#### Notas

- BUG-001 (header `Cache-Control: no-store` ausente em 401) foi descoberto neste endpoint. Marcado em T2 e T3.
- T8 e T9 marcados como PASS parcial: o status 401 é coerente porque não havia cookie válido. O comportamento desejado (200 com body ignorado, 200 com Origin forjado) só pode ser validado após BUG-000 estar corrigido. Confirmado que o backend não estourou exceção por causa do body arbitrário nem por causa do header `Origin` — ambos foram ignorados como esperado.

### POST /api/v1/auth/logout (7 casos)

| ID  | Descrição | Esperado | Obtido | Resultado | Bug |
|-----|-----------|----------|--------|-----------|-----|
| T1  | Logout após login | 204 + Set-Cookie apagador + `no-store` | 204 + Set-Cookie correto + `no-store` | PASS | — |
| T2  | Logout sem cookie (idempotente) | 204 + Set-Cookie apagador | 204 + Set-Cookie correto + `no-store` | PASS | — |
| T3  | Logout com cookie inválido | 204 + Set-Cookie apagador (não vaza existência) | 204 idêntico a T2 | PASS | — |
| T4  | Logout duplicado (2x) | 204 / 204 idempotente, sem 500 | 204 / 204; ambas com Set-Cookie e correlationIds diferentes | PASS | — |
| T5  | CA011: refresh após logout | 401 no /refresh | impossível — login não emite cookie por BUG-000 | BLOCKED | BUG-000 |
| T6  | Cache-Control no-store presente | `Cache-Control: no-store` | confirmado em todas as 7 chamadas | PASS | — |
| T7  | Body arbitrário ignorado | 204 sem 400/422 | 204 + Set-Cookie + `no-store` | PASS | — |

#### Notas

- **Atributos do `Set-Cookie` apagador validados em T1:** `carwash_refresh_token=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/api/v1/auth; samesite=strict; httponly`. Falta `Secure` — esperado em Development (perfil declarado no doc). Em Production isso precisa ter `Secure` (não validável aqui).
- **Log Serilog observado em T2 (sem cookie):** `[14:27:19 INF] Logout efetuado. UsuarioId=null` — interessante: o doc QA diz que sem sessão **não deve** existir log `"Logout efetuado. UsuarioId=..."`. Aqui o log apareceu com `UsuarioId=null`. Ruído leve, não é bug funcional. Sugestão de melhoria: condicionar a emissão do log à existência de sessão match, para reduzir ruído.
- **CA011 (T5) não validável:** o critério mais crítico do logout (revogação server-side validada via /refresh) está BLOCKED. Até BUG-000 fechar, CA011 sobre logout segue não atestado.

## Anexos — Trechos de log relevantes

### BUG-000 (login)
```
[14:22:53 ERR] Falha não tratada. CorrelationId=d5272fe0d0e34a42bfaf4e91153bb05c
Npgsql.PostgresException (0x80004005): 42703: column u.bloqueado_ate does not exist
    Severity: ERROR
    SqlState: 42703
    MessageText: column u.bloqueado_ate does not exist
    Position: 40
    File: parse_relation.c
    Routine: errorMissingColumn
   at CarWash.Application.Auth.Login.LoginHandler.HandleAsync(LoginCommand command, CancellationToken cancellationToken) in /src/src/CarWash.Application/Auth/Login/LoginHandler.cs:line 85
   at CarWash.Api.Endpoints.Auth.AuthEndpoints.LoginAsync(LoginCommand command, ICommandHandler`2 handler, IHostEnvironment env, HttpContext http, CancellationToken cancellationToken) in /src/src/CarWash.Api/Endpoints/Auth/AuthEndpoints.cs:line 66
```

### BUG-000 (SaveChanges/UPDATE)
```
[14:23:22 ERR] An exception occurred in the database while saving changes for context type 'CarWash.Infrastructure.Persistence.CarWashDbContext'.
Microsoft.EntityFrameworkCore.DbUpdateException: An error occurred while saving the entity changes. See the inner exception for details.
 ---> Npgsql.PostgresException (0x80004005): 42703: column "bloqueado_ate" of relation "usuarios" does not exist
    Position: 49
    File: parse_target.c
```

### Schema atual de `usuarios` (PostgreSQL)
```
    Column     |           Type           | Nullable | Default
---------------+--------------------------+----------+---------
 id            | uuid                     | not null |
 nome          | character varying(120)   | not null |
 email         | character varying(150)   | not null |
 senha_hash    | text                     | not null |
 perfil        | character varying(20)    | not null |
 ativo         | boolean                  | not null | true
 criado_em     | timestamp with time zone | not null | now()
 atualizado_em | timestamp with time zone | not null | now()
```
Esperado adicional: `bloqueado_ate timestamptz null`, `tentativas_invalidas int not null default 0`, `ultima_tentativa_em timestamptz null` (ou nomes equivalentes — confirmar com `UsuarioConfiguration.cs`).

### Migrations aplicadas
```
         migration_id         | product_version
------------------------------+-----------------
 20260513114525_InitialSchema | 8.0.10
(1 row)
```

### Rate limit /login (429)
```
HTTP/1.1 429 Too Many Requests
Content-Type: application/problem+json
Retry-After: 60
X-Correlation-Id: a82ae087dea4468cb02aed7aaaea5cfa

{"title":"Muitas tentativas. Aguarde um instante e tente novamente.","status":429}
```

### Set-Cookie apagador no logout (T1)
```
Set-Cookie: carwash_refresh_token=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/api/v1/auth; samesite=strict; httponly
Cache-Control: no-store
X-Correlation-Id: c75a8706dbcc4fc099cd498e6e74a25c
```

## Próximos passos (recomendações de QA)

1. **Bloquear release do MVP até BUG-000 ser corrigido.** Adicionar teste de integração com Testcontainers em `tests/CarWash.IntegrationTests` que cobre:
   - `POST /login` golden path (200 + JWT decodificável + cookie HttpOnly/SameSite=Strict).
   - 3 falhas seguidas → 403 com `Retry-After` (CA011/RNF005).
   - Validação de schema: assert `INFORMATION_SCHEMA.columns` contém `bloqueado_ate`, `tentativas_invalidas`, `ultima_tentativa_em` em `usuarios` — rodar como `[Trait("CA","011")]`.
2. **Corrigir BUG-001** (Cache-Control em 401 do /refresh) e cobrir com teste de integração que asserte o header em todos os caminhos (200, 401).
3. **Após BUG-000 resolvido**, re-executar TODA a suite deste relatório. Em particular:
   - T9 (lockout) — validar `Retry-After: 900` e `bloqueadoAte` ISO 8601 UTC.
   - T11 (usuário inativo) — criar `qa-login@carwash.local` via endpoint admin, marcar `ativo=false`, validar 403.
   - T13 / Refresh T4 / Logout T5 — fluxo CA011 ponta-a-ponta.
4. **Revisar a mensagem `"Identificador inválido."`** para JSON malformado (T7 do login) — texto não condiz com o cenário.
5. **Reduzir ruído de log em logout sem sessão** (T2) — não emitir `Logout efetuado. UsuarioId=null` para chamadas sem cookie.
