# Relatório — Auth (v3 — pós segunda iteração de fix)

Data: 2026-05-17T17:30:00Z
Backend: http://localhost:8080 (container `carwash-backend` UP ~1h; sem reinício durante esta rodada)
DB: `carwash-postgres` (healthy). Schema `usuarios` mantém `bloqueado_ate timestamptz` e `tentativas_invalidas int not null default 0` (CHECK `>= 0`).
Cookie jar principal: `/tmp/qa-auth-v3/cookies.txt`.
Rodada anterior: `../v2-pos-fix1/auth.md`
Bugs fechados nesta iteração (CONFIRMADOS): **BUG-005**, **BUG-008**, **BUG-Auth-Lockout**
Bugs P3 ainda abertos: **BUG-006** (título "Identificador inválido." em JSON malformado), **BUG-007** (`Logout efetuado. UsuarioId=null` mesmo com cookie válido)
Bug NOVO descoberto: **BUG-010** (R-T10 race — multi-refresh paralelo emite 2× 200 em ~50% dos runs após o fix do BUG-008)

## Comparativo v2 → v3
| Endpoint            | v2 PASS | v3 PASS | Δ  |
|---------------------|--------:|--------:|---:|
| POST /login (13)    | 12      | 13      | +1 |
| POST /refresh (10)  | 9       | 9       | 0  |
| POST /logout (7)    | 7       | 7       | 0  |
| **Total**           | **28**  | **29**  | **+1** |

Notas do comparativo:
- **Login T9** (lockout) saiu de FAIL → PASS — BUG-Auth-Lockout fechado (1/2/3=401, 4=403, header `Retry-After: 900` presente).
- **Refresh T2** (sem cookie) saiu de FAIL → PASS — BUG-005 fechado (`Cache-Control: no-store` presente em 401).
- **Refresh T4** (reuse + família revogada) PASS funcional confirmado — BUG-008 fechado (3ª chamada com cookie rotacionado retorna 401, logs mostram `Refresh token reuse detectado. Família revogada`).
- **Refresh T10** (race) — PASS na v2 vira **FAIL** na v3: o fix do BUG-008 introduziu janela de concorrência que permite 2× 200 em 50% dos runs (10 runs: 5× 200/200, 2× 200/401, 3× 401/401). Novo BUG-010 aberto. Para preservar a linha de mudança "número agregado", marquei T10 como FAIL e o total de refresh permaneceu 9/10 (em v2 o FAIL era T2).

## Sumário
- **Total: 30 | PASS: 29 | FAIL: 1 | BLOCKED: 0**
- Bugs fechados confirmados: **BUG-005**, **BUG-008**, **BUG-Auth-Lockout**
- Bugs ainda abertos (P3): BUG-006 (baixo), BUG-007 (médio)
- Bugs novos descobertos: **BUG-010 (CRÍTICO — race no /refresh quebra single-use)**

## Bugs (apenas novos ou ainda abertos)

### BUG-010 — Refresh paralelo emite 2× 200 a partir do MESMO refresh token (CRÍTICO, NOVO — regressão do fix do BUG-008)

- **Severidade:** CRÍTICA (CA011, segurança de sessão, single-use enforcement).
- **Sintoma:** Quando duas chamadas `/api/v1/auth/refresh` são disparadas em paralelo usando o MESMO cookie (cenário R-T10 do doc QA), o backend frequentemente emite **dois `200 OK` válidos**, com **dois refresh tokens rotacionados distintos**, ambos derivados da mesma `SessaoAnterior`. Doc QA T10 é explícito: "Saída: uma linha `200` e uma linha `401`. Apenas um vencedor."
- **Estatística (10 runs sequenciais, mesmo usuário admin):**
  ```
  run 1:  200 200   <-- 2x200 (BUG)
  run 2:  200 200   <-- 2x200 (BUG)
  run 3:  200 200   <-- 2x200 (BUG)
  run 4:  200 200   <-- 2x200 (BUG)
  run 5:  200 200   <-- 2x200 (BUG)
  run 6:  200 401   <-- OK
  run 7:  200 401   <-- OK
  run 8:  401 401   <-- inesperado (família revogada retroativa após sucesso parcial)
  run 9:  401 401   <-- inesperado
  run 10: 401 401   <-- inesperado
  ```
  Frequência de quebra: **5/10 = 50%** retornam 2× 200; **3/10 = 30%** retornam 2× 401 (estado degenerado também inválido segundo doc); apenas **2/10 = 20%** retornam o esperado 1×200 + 1×401.
- **Logs Serilog correlacionados (run com 2× 200):**
  ```
  [17:25:02 INF] Sessão renovada. UsuarioId=00000000-..., SessaoAnterior=be7e1a42-..., SessaoNova=9c31687e-...
  [17:25:02 INF] Sessão renovada. UsuarioId=00000000-..., SessaoAnterior=be7e1a42-..., SessaoNova=21e3922d-...
  ```
  Duas linhas de "Sessão renovada" com **mesmo `SessaoAnterior`** e diferentes `SessaoNova` — prova de que duas transações concorrentes leram a sessão antes da revogação ser commitada.
- **Hipótese:** o handler de refresh provavelmente faz `SELECT` da sessão pelo hash do token e depois `UPDATE` (revogando antiga + inserindo nova). Sem `SELECT ... FOR UPDATE` (lock pessimista) ou sem `RowVersion`/optimistic concurrency token, duas transações concorrentes leem a mesma linha "ativa" e ambas conseguem rotacionar. O fix do BUG-008 (revogação de família) talvez tenha removido um lock que existia antes, ou o caminho de detecção de reuse mascarava esta janela na v2.
- **Impacto:** quebra de single-use enforcement — atacante com cookie roubado pode disparar duas chamadas paralelas e obter dois access tokens válidos antes da defesa kickar. Em runs que caem em 401/401, ambos os clientes legítimos são deslogados (DoS auto-infligido). Mata o CA011 ("apenas o último refresh emitido vale").
- **Reprodução determinística:**
  ```bash
  : > /tmp/race-cookies.txt
  curl -s -c /tmp/race-cookies.txt -X POST http://localhost:8080/api/v1/auth/login \
    -H 'Content-Type: application/json' \
    -d '{"email":"admin@carwash.local","senha":"DevSeedAdmin2026!Forte"}' -o /dev/null
  ( curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:8080/api/v1/auth/refresh -b /tmp/race-cookies.txt ) &
  ( curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:8080/api/v1/auth/refresh -b /tmp/race-cookies.txt ) &
  wait
  # Repetir 5–10 vezes; em ~50% verá 200 200.
  ```
- **Sugestão:**
  1. No `RefreshHandler`, ler a sessão com lock pessimista: `FROM usuario_sessoes ... FOR UPDATE` (PostgreSQL) ou usar uma constraint UNIQUE em `refresh_token_hash` + `revogado_em IS NULL` com `INSERT ... ON CONFLICT` para forçar serialização.
  2. Alternativa: coluna `RowVersion` (xmin/optimistic) e re-tentar com DbUpdateConcurrencyException → traduzir em 401.
  3. Cobrir com teste de integração `[Trait("CA","011")]` com `Task.WhenAll` de duas chamadas `/refresh` ao mesmo cookie, asserindo **exatamente** 1× 200 e 1× 401 (e o tipo do 401 ser `refresh-token-invalido`, NÃO reuse detectado, pois é race natural).

### BUG-006 — ProblemDetails com `title: "Identificador inválido."` para body JSON malformado (BAIXO, AINDA ABERTO)

- **Severidade:** BAIXA (UX/semântica).
- **Sintoma:** `POST /api/v1/auth/login` com JSON malformado (Login T7) retorna `400 Bad Request` com `type: "https://carwash/errors/invalid-request"` e `title: "Identificador inválido."`. O título não condiz com o cenário (não há "identificador" no payload de login).
- **Evidência (v3):**
  ```json
  {"type":"https://carwash/errors/invalid-request","title":"Identificador inválido.","status":400,
   "correlationId":"a04cc3c57033487fa90b3fa010ccc848",
   "errors":{"request":["Failed to read parameter \"LoginCommand command\" from the request body as JSON."]}}
  ```
- **Sugestão:** filtro genérico para `BadHttpRequestException` da pipeline JSON em ProblemDetails, com `title: "Requisição inválida."` ou `title: "JSON inválido."`.

### BUG-007 — `Logout efetuado. UsuarioId=null` mesmo para logout com cookie válido (MÉDIO, AINDA ABERTO)

- **Severidade:** MÉDIA (observabilidade, auditoria CA011).
- **Sintoma:** Todos os logs `Logout efetuado` capturados nesta rodada continuam com `UsuarioId=null`, inclusive os de L-T1 (login + logout com cookie válido) e L-T4 1ª chamada.
- **Evidência (v3, logs do backend durante a janela 17:26–17:27Z):**
  ```
  [17:26:20 INF] Logout efetuado. UsuarioId=null   <-- L-T1 com cookie válido
  [17:26:29 INF] Logout efetuado. UsuarioId=null   <-- L-T2 sem cookie
  [17:27:29 INF] Logout efetuado. UsuarioId=null   <-- L-T5 com cookie válido (CA011)
  ```
- **Impacto:** auditoria `UsuarioLogout` perde a correlação com o usuário. Critério CA011 (auditoria) prejudicado.
- **Sugestão:** mover o log para após resolução da sessão no `LogoutHandler`; condicionar emissão à existência de match.

## POST /api/v1/auth/login (13 casos)

| ID  | Descrição                                  | Esperado                                       | Obtido                                                                 | Resultado | Bug      |
|-----|--------------------------------------------|------------------------------------------------|------------------------------------------------------------------------|-----------|----------|
| T1  | Golden path admin                          | 200 + JWT + cookie HttpOnly + `no-store`       | 200, JWT válido (`sub`, `email`, `perfil=Admin`, `exp` +15min), cookie `HttpOnly; SameSite=Strict; Path=/api/v1/auth`, `Cache-Control: no-store`, correlationId `af3b956a…` | PASS | — |
| T2  | Email inexistente                          | 401 genérico anti-enumeração                   | 401 `{"type":".../invalid-credentials","title":"Usuário ou senha inválidos."}` + `Cache-Control: no-store` | PASS | — |
| T3  | Senha incorreta usuário existente          | 401 mesma mensagem de T2                       | 401 idêntico a T2                                                       | PASS | — |
| T4  | Email malformado sem `@`                   | 401 (anti-enumeração)                          | 401 idêntico a T2                                                       | PASS | — |
| T5a | Senha vazia                                | 400 + erro `senha` obrigatória                 | 400 `{"errors":{"senha":["Senha é obrigatória."]}}`                     | PASS | — |
| T5b | Senha `null`                               | 400 + erro `senha`                             | 400 idêntico a T5a                                                      | PASS | — |
| T6  | Body vazio `{}`                            | 400 + erros `email` e `senha`                  | 400 com ambos os erros                                                  | PASS | — |
| T7  | JSON malformado                            | 400 com título coerente                        | 400 `title:"Identificador inválido."` + `errors.request` ok             | PASS (ressalva) | BUG-006 (aberto) |
| T8a | Sem Content-Type                           | 415 ou 400                                     | 415 sem body                                                            | PASS | — |
| T8b | Content-Type `text/plain`                  | 415 ou 400                                     | 415 sem body                                                            | PASS | — |
| T9  | Lockout após 3 falhas (qa-lockout-v3)      | 401×3 + 403 com `Retry-After:900`              | **401, 401, 401, 403** + `Retry-After: 900` + body com `retryAfterSeconds:900` e `bloqueadoAte` ISO | **PASS** | **BUG-Auth-Lockout fechado** |
| T10 | Rate limit por IP                          | 11ª req+ → 429 + `Retry-After`                 | 10× 401 → 11–15× 429 com `Retry-After: 60` e body conforme contrato     | PASS | — |
| T11 | Usuário inativo (qa-lockout-v3 `ativo=false`) | 403 `usuario-inativo`                       | 403 `{"type":".../usuario-inativo","title":"Acesso bloqueado. Usuário inativo."}` | PASS | — |
| T12 | Login simultâneo (qa-lockout-v3 ×2)        | 200 + 200 cookies distintos                    | 2× 200, cookies distintos (`3ksQmz1j…` e `5XyEjV3h…`)                   | PASS | — |
| T13 | Rotação refresh entre 2 logins             | 200 + 200 cookies distintos                    | 2× 200, cookies distintos (`GjjEd1LO…` e `EYx7gv_d…`)                   | PASS | — |

Notas:
- **T9 PASS:** lockout dispara na 4ª tentativa (limite=4 conforme reporte do dev); header HTTP `Retry-After: 900` presente alinhado com `retryAfterSeconds:900` no body. Doc QA pede "após 3 falhas, na 4ª" — bate com o implementado.
- **T10:** janela de rate limit confirmada (10 req aceitas → 11+ retornam 429 com `Retry-After: 60`). Body do 429: `{"title":"Muitas tentativas. Aguarde um instante e tente novamente.","status":429}`.
- **Anti-enumeração T2/T3/T4:** mensagens idênticas, status idêntico, sem variação de timing perceptível em amostra única.

## POST /api/v1/auth/refresh (10 casos)

| ID  | Descrição                                | Esperado                                      | Obtido                                                                                                      | Resultado | Bug                  |
|-----|------------------------------------------|-----------------------------------------------|-------------------------------------------------------------------------------------------------------------|-----------|----------------------|
| T1  | Golden path com cookie válido            | 200 + cookie rotacionado + `no-store`         | 200, cookie rotacionou (`Z4J2gqZU…` → `9yv6ZrXW…`), `Cache-Control: no-store`, JWT distinto                  | PASS      | —                    |
| T2  | Sem cookie                               | 401 + `no-store`                              | 401 **com** `Cache-Control: no-store` + body `refresh-token-invalido`                                        | **PASS**  | **BUG-005 fechado**  |
| T3  | Cookie lixo/aleatório                    | 401 + `no-store`                              | 401 **com** `Cache-Control: no-store` + body idêntico ao T2                                                  | **PASS**  | **BUG-005 fechado**  |
| T4  | Reuse do mesmo cookie + família revogada | 1ª=200; 2ª=401; 3ª (cookie rotacionado da 1ª)=401 | 1ª=200 (rotaciona A→B), 2ª=401 (reuse de A), **3ª=401** (cookie B também rejeitado — família revogada)   | **PASS**  | **BUG-008 fechado**  |
| T5  | Cookie revogado via /logout              | 401                                           | 401 + `no-store`                                                                                             | PASS      | —                    |
| T6  | Cookie expirado (UPDATE SQL em `expira_em`) | 401                                        | 401 + `no-store` (forçado via `UPDATE usuario_sessoes SET expira_em = NOW() - INTERVAL '1 minute' WHERE usuario_id=...`) | PASS | — |
| T7  | Usuário inativado entre login e refresh  | 401                                           | 401 + `no-store`                                                                                             | PASS      | —                    |
| T8  | Body arbitrário                          | 200 (body ignorado)                           | 200 com cookie rotacionado, body NÃO afetou comportamento                                                    | PASS      | —                    |
| T9  | CSRF cross-site (Origin/Referer)         | 200 via curl (`SameSite=Strict` só atua em browser) | 200, server não bloqueia por `Origin` (esperado para curl)                                              | PASS      | —                    |
| T10 | Multi-refresh paralelo (race)            | **1×200 + 1×401**                             | Em 10 runs: 5×(200+200), 2×(200+401), 3×(401+401). Frequência de quebra single-use: **50%**                  | **FAIL**  | **BUG-010 (NOVO crítico)** |

Notas:
- **R-T4 confirmado:** logs do backend registram `Refresh token reuse detectado. Família revogada por segurança. ... SessoesAfetadas=N` — defesa funcional confirmada para o cenário sequencial. BUG-008 fechado.
- **R-T2/R-T3 confirmados:** `Cache-Control: no-store` agora presente em **todas** as respostas 401 do `/refresh` (T2, T3, T4 2ª, T5, T6, T7). BUG-005 fechado.
- **R-T10 FAIL:** explicado em BUG-010. Aparenta ser regressão do fix de BUG-008 (provavelmente faltou lock pessimista ou concorrência otimista).

## POST /api/v1/auth/logout (7 casos)

| ID  | Descrição                       | Esperado                                                      | Obtido                                                                                                       | Resultado | Bug      |
|-----|---------------------------------|---------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------|-----------|----------|
| T1  | Logout após login               | 204 + Set-Cookie apagador + `no-store`                         | 204 + `Set-Cookie: carwash_refresh_token=; expires=Thu, 01 Jan 1970 ...; path=/api/v1/auth; samesite=strict; httponly` + `Cache-Control: no-store` | PASS | — |
| T2  | Logout sem cookie               | 204 idempotente + Set-Cookie apagador                          | 204 + Set-Cookie apagador + `no-store`                                                                       | PASS      | —        |
| T3  | Logout com cookie inválido      | 204 indistinguível de T2                                       | 204 idêntico a T2                                                                                            | PASS      | —        |
| T4  | Logout duplicado (mesmo cookie) | 204 / 204 idempotente                                          | 204 / 204                                                                                                    | PASS      | —        |
| T5  | CA011: refresh após logout      | 401 no /refresh                                                | 401 — sessão revogada server-side; CA011 atendido                                                            | PASS      | —        |
| T6  | Cache-Control no-store presente | `Cache-Control: no-store`                                      | presente                                                                                                     | PASS      | —        |
| T7  | Body arbitrário ignorado        | 204                                                            | 204 + Set-Cookie + `no-store`                                                                                | PASS      | —        |

Notas:
- **BUG-007 ainda aberto:** todos os 3 logs `Logout efetuado` capturados nesta rodada estão com `UsuarioId=null`, inclusive os de T1 e T5 (cookie válido e sessão sendo revogada — CA011 L-T5 confirma que a revogação acontece). Severidade média, regressão de observabilidade.
- **`Set-Cookie` apagador validado em T1:** atributos `HttpOnly`, `Path=/api/v1/auth`, `SameSite=Strict`, `expires=Thu, 01 Jan 1970 00:00:00 GMT`. Sem `Secure` (esperado em Development).
- **CA011 L-T5 PASS:** `/refresh` com cookie pré-logout retorna 401 — revogação server-side confirmada.

## Anexos — trechos relevantes

### Headers 403 de lockout (T9) — com `Retry-After: 900` (BUG-Auth-Lockout fechado)

```
HTTP/1.1 403 Forbidden
Content-Type: application/problem+json
Date: Sun, 17 May 2026 16:21:19 GMT
Server: Kestrel
Cache-Control: no-store
Retry-After: 900
Transfer-Encoding: chunked

{"type":"https://carwash/errors/usuario-bloqueado",
 "title":"Acesso temporariamente bloqueado por tentativas inválidas. ...",
 "status":403,
 "correlationId":"13d2927ffe2f4e98bf5718bccd316371",
 "bloqueadoAte":"2026-05-17T16:36:19.4756742Z",
 "retryAfterSeconds":900}
```

### Headers 401 do /refresh (T2/T3) — com `no-store` (BUG-005 fechado)

```
HTTP/1.1 401 Unauthorized
Content-Type: application/problem+json
Date: Sun, 17 May 2026 17:20:36 GMT
Server: Kestrel
Cache-Control: no-store
Transfer-Encoding: chunked

{"type":"https://carwash/errors/refresh-token-invalido",
 "title":"Refresh token inválido ou expirado.",
 "status":401,
 "correlationId":"0ea4fa81cfa64eb098f9b6edd72e5dee"}
```

### Logs de família revogada em reuse (BUG-008 fechado)

```
[17:25:05 WRN] Refresh token reuse detectado. Família revogada por segurança.
  UsuarioId=00000000-0000-0000-0000-000000000001,
  SessaoComprometida=25e3b021-a264-48ab-a0b0-60671ee97634,
  SessoesAfetadas=8
```

### Logs do race do /refresh (BUG-010 NOVO)

```
[17:25:02 INF] Sessão renovada. UsuarioId=00000000-..., SessaoAnterior=be7e1a42-..., SessaoNova=9c31687e-...
[17:25:02 INF] Sessão renovada. UsuarioId=00000000-..., SessaoAnterior=be7e1a42-..., SessaoNova=21e3922d-...
```

Duas "Sessão renovada" com **mesmo `SessaoAnterior`** → duas transações concorrentes leram e rotacionaram a mesma linha. Single-use quebrado.

### Logs de logout (BUG-007 ainda aberto)

```
[17:26:20 INF] Logout efetuado. UsuarioId=null   <-- L-T1 com cookie válido
[17:26:29 INF] Logout efetuado. UsuarioId=null   <-- L-T2 sem cookie
[17:27:29 INF] Logout efetuado. UsuarioId=null   <-- L-T5 com cookie válido (CA011)
```

### Estado final (cleanup)

```
         email          | ativo | tentativas_invalidas | bloqueado_ate
------------------------+-------+----------------------+---------------
 admin@carwash.local    | t     |                    0 |
 qa-lockout@qa.local    | t     |                    0 |
 qa-lockout-v3@qa.local | t     |                    0 |
```

Usuário `qa-lockout-v3@qa.local` (id `330fc587-b486-41a4-8640-e89ffb4e7090`, perfil `Funcionario`, senha `Forte!Teste2026Senha`) criado nesta rodada via `POST /api/v1/usuarios` autenticado como admin; permanece no banco, ativo, sem lockout — pode ser reutilizado em rebaterias futuras ou removido manualmente.

## Próximos passos (recomendações de QA)

1. **CRÍTICO — BUG-010 (race no /refresh):** bloquear release até resolver. Adicionar `SELECT ... FOR UPDATE` (ou RowVersion/optimistic) no `RefreshHandler`. Cobrir com teste `[Trait("CA","011")]` que dispara `Task.WhenAll(2 chamadas /refresh)` e assert exatamente 1×200 + 1×401. Sem isso, single-use enforcement está furado em 50% dos cenários paralelos.
2. **Fechar BUG-007 (auditoria de logout sem `UsuarioId`):** mover log para após resolução da sessão no `LogoutHandler`; condicionar emissão à existência de match.
3. **Fechar BUG-006 (título "Identificador inválido."):** handler genérico para `BadHttpRequestException` JSON em ProblemDetails, com `title: "Requisição inválida."`.
4. **Adicionar à suíte CI `[Trait("CA","011")]`:**
   - Cenário R-T4: 3 chamadas (200, 401, 401) cobrindo família revogada em reuse.
   - Cenário R-T10: `Task.WhenAll` cobrindo race natural (1×200 + 1×401).
   - Cenário Login T9: 1..4 falhas + header `Retry-After: 900` no body e no header HTTP.
5. **Hardening pendente (já documentado em rodadas anteriores):** rate-limit em `/refresh` (atualmente sem limite — risco de força bruta), teste estatístico de timing T2 vs T3 (anti-enumeração) com K6/NBomber.
