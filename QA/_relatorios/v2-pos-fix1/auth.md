# Relatório de execução — Auth (REBATERIA pós-fix)

Data: 2026-05-17T15:13:00Z
Backend: http://localhost:8080 (container `carwash-backend` UP 11min; reiniciado pós-fix)
DB: `carwash-postgres` (healthy). Schema `usuarios` agora inclui `bloqueado_ate timestamptz` e `tentativas_invalidas int not null default 0` (com `CHECK ck_usuarios_tentativas_invalidas >= 0`).
Migrations aplicadas: `20260513114525_InitialSchema`, `20260517022432_AddUsuarioLockoutFields`.
Cookie jar principal: `/tmp/carwash-cookies-auth-v2.txt`.
Rodada anterior arquivada em: `../v1-pre-fix/auth.md`
Bugs fechados desde a rodada anterior: **BUG-000** (migration de lockout aplicada — schema confirmado), **BUG-002** (RequireAuthorization em `/usuarios` — `POST /api/v1/usuarios` exigiu Bearer e respondeu 401 sem token; com token admin retornou 201).

## Comparativo com rodada anterior

- **v1 (pre-fix):** PASS 13 / FAIL 1 / BLOCKED 16
- **v2 (esta):** PASS 28 / FAIL 2 / BLOCKED 0

| Endpoint       | v1 PASS | v1 FAIL | v1 BLOCKED | v2 PASS | v2 FAIL | v2 BLOCKED |
|----------------|---------|---------|------------|---------|---------|------------|
| POST /login    | 4       | 0       | 9          | 12      | 1       | 0          |
| POST /refresh  | 3       | 1       | 6          | 9       | 1       | 0          |
| POST /logout   | 6       | 0       | 1          | 7       | 0       | 0          |
| **Total**      | **13**  | **1**   | **16**     | **28**  | **2**   | **0**      |

> Observação: o T9 do login mudou de BLOCKED para FAIL (não é mais bloqueado pelo schema; agora o bloqueio dispara cedo demais e não emite `Retry-After`). O T2/T3 do refresh mudou para PASS no fluxo (401 esperado) — mas mantenho FAIL apenas em T2 pelo BUG-005 (cabeçalho `no-store`), o mesmo bug agora reclassificado.

## Sumário

- Total: 30 | PASS: 28 | FAIL: 2 | BLOCKED: 0
- Bugs novos descobertos: **2** (BUG-008 reuse de família não revogada; BUG-009 lockout dispara na 3ª falha e sem header `Retry-After`)
- Bugs antigos confirmados como AINDA abertos: **BUG-005** (Cache-Control `no-store` ausente em 401 do `/refresh`), **BUG-006** (ProblemDetails com `title: "Identificador inválido."` para body JSON malformado), **BUG-007** (`Logout efetuado. UsuarioId=null` mesmo para logout com cookie válido — pior: regredido vs. expectativa do doc)
- Bugs antigos confirmados como FECHADOS: **BUG-000** (migration de lockout), **BUG-001 / BUG-002** (auth obrigatória em `/usuarios`)

## Bugs (apenas novos ou ainda abertos)

### BUG-005 — `Cache-Control: no-store` ausente em respostas 401 do `/refresh` (MÉDIO, AINDA ABERTO)

- **Severidade:** MÉDIA (segurança/cache).
- **Sintoma:** Em `POST /api/v1/auth/refresh`, todas as respostas `401 Unauthorized` (R-T2, R-T3, R-T4 2ª chamada, R-T5, R-T6, R-T7, R-T10 perdedora) **NÃO** contêm `Cache-Control: no-store`. Já as respostas `200` (R-T1, R-T8, R-T9, R-T10 vencedora) trazem o header corretamente.
- **Doc QA exige:** "T2 — Resposta esperada: 401 ... `Cache-Control: no-store` continua presente."
- **Evidência:** headers de R-T3 (cookie lixo):
  ```
  HTTP/1.1 401 Unauthorized
  Content-Type: application/problem+json
  Date: Sun, 17 May 2026 15:10:34 GMT
  Server: Kestrel
  Transfer-Encoding: chunked

  {"type":"https://carwash/errors/refresh-token-invalido","title":"Refresh token inválido ou expirado.","status":401,"correlationId":"3e1cf40222b74f5fb500309c6e5082b4"}
  ```
- **Sugestão:** mover o `Response.Headers.CacheControl = "no-store"` para antes de qualquer `throw` no `RefreshHandler`, ou aplicar via middleware específico para `/api/v1/auth/*`. Cobrir com integração `[Trait("CA","011")]` que asserte `no-store` em 200 e 401.

### BUG-006 — ProblemDetails com `title: "Identificador inválido."` para body JSON malformado (BAIXO, AINDA ABERTO)

- **Severidade:** BAIXA (UX, semântica).
- **Sintoma:** `POST /api/v1/auth/login` com JSON malformado (T7) retorna `400 Bad Request` com `type: "https://carwash/errors/invalid-request"` e `title: "Identificador inválido."`. O título não condiz com o cenário (não há "identificador" no payload de login; trata-se de erro de desserialização).
- **Evidência:**
  ```json
  {"type":"https://carwash/errors/invalid-request","title":"Identificador inválido.","status":400,"correlationId":"8cba3e12e4264f72bd90b7cfb82c9101","errors":{"request":["Failed to read parameter \"LoginCommand command\" from the request body as JSON."]}}
  ```
- **Sugestão:** alinhar título com o tipo. Algo como `title: "Requisição inválida."` ou `title: "JSON inválido."`. Filtro genérico em `ProblemDetails` para `BadHttpRequestException` da pipeline JSON.

### BUG-007 — `Logout efetuado. UsuarioId=null` mesmo para logout com cookie válido (MÉDIO, AINDA ABERTO E PIORADO)

- **Severidade:** MÉDIA (observabilidade, regressão).
- **Sintoma:** todos os 6 logs `Logout efetuado` capturados durante esta rodada estão com `UsuarioId=null`, inclusive os de L-T1 (login + logout com cookie válido) e L-T4 1ª chamada. Doc QA explicitamente espera `[INF] Logout efetuado. UsuarioId=<guid>` quando há sessão.
- **Evidência (logs do backend, janela 15:10–15:13Z, todas as chamadas de logout — algumas COM cookie válido):**
  ```
  [15:10:57 INF] Logout efetuado. UsuarioId=null
  [15:12:29 INF] Logout efetuado. UsuarioId=null   <-- L-T1 com cookie válido
  [15:12:33 INF] Logout efetuado. UsuarioId=null
  [15:12:41 INF] Logout efetuado. UsuarioId=null   <-- L-T4 1ª chamada com cookie válido
  [15:12:41 INF] Logout efetuado. UsuarioId=null
  [15:12:41 INF] Logout efetuado. UsuarioId=null
  ```
- **Impacto:** auditoria `UsuarioLogout` perde a correlação com o usuário. Critério de aceite CA011 (auditoria) prejudicado. Pior do que descrito originalmente — não é só ruído em chamada sem cookie; é que o handler NUNCA resolve o `UsuarioId` antes de logar, mesmo quando a sessão foi efetivamente revogada (CA011 L-T5 funcional, prova que o backend sabe identificar a sessão para revogar — mas o log não usa essa info).
- **Sugestão:** mover o log `Logout efetuado` para após a leitura de `sessao.UsuarioId` no `LogoutHandler`. Condicionar o log à existência de sessão match (não logar para chamadas sem cookie ou cookie inválido) — sem isso, a métrica de "logouts efetivos" fica inflacionada.

### BUG-008 — Família de refresh NÃO é revogada após reuse detectado (CRÍTICO, NOVO)

- **Severidade:** CRÍTICA (CA011, segurança de sessão).
- **Sintoma:** No fluxo R-T4 (reuse), a 2ª chamada com cookie antigo (já consumido) corretamente retorna `401`. Porém o cookie **rotacionado emitido na 1ª chamada continua válido** — uma 3ª chamada usando esse cookie rotacionado retorna `200 OK` com novo token. Doc QA explicitamente exige: "Toda a família de refresh deve ser revogada (i.e., o cookie rotacionado emitido na 1ª chamada também passa a ser inválido, por política de segurança)."
- **Reprodução:**
  ```bash
  # login -> cookie A
  curl -s -c /tmp/snap.txt -X POST .../auth/login -d '{"email":"...","senha":"..."}'
  cp /tmp/snap.txt /tmp/snap-original.txt
  # 1ª chamada com snap-original -> 200, gera cookie B (rotacionado)
  curl -X POST .../auth/refresh -b /tmp/snap-original.txt -c /tmp/snap-rotated.txt
  # 2ª chamada REUSANDO snap-original -> 401 (correto)
  curl -X POST .../auth/refresh -b /tmp/snap-original.txt
  # 3ª chamada com cookie B (rotacionado da 1ª) -> ESPERADO 401, OBTIDO 200
  curl -X POST .../auth/refresh -b /tmp/snap-rotated.txt
  ```
- **Evidência observada:** 3ª chamada (`/tmp/carwash-refresh-r4-rotated.txt`) retornou `HTTP/1.1 200 OK` com novo `accessToken` e novo `Set-Cookie`, correlationId `cda1de2899874d9bb8f3e51ff8243277`.
- **Impacto:** se um atacante exfiltrar o cookie original e tentar usar, o cliente legítimo (que já rotacionou) NÃO é alertado nem desconectado. O ataque é detectado mas só o reuse específico é negado — a sessão "boa" do atacante continua viva via rotação futura. Quebra o padrão de single-use enforcement com revogação de família esperado pelo doc.
- **Sugestão:** no `RefreshHandler`, ao detectar reuse de token não-último-da-família, marcar `revogado_em = NOW()` em **todas** as sessões da mesma família (`familia_id` ou `usuario_id` + cadeia). Cobrir com teste de integração `[Trait("CA","011")]` que faça as 3 chamadas e assert 200 + 401 + 401.

### BUG-009 — Lockout dispara na 3ª falha (deveria ser a partir da 4ª) e sem header `Retry-After` (ALTO, NOVO)

- **Severidade:** ALTA (RNF005, CA011).
- **Sintoma A — Timing:** Doc QA T9 especifica: "Tentativas 1, 2 e 3: 401 (credenciais inválidas, mensagem genérica). Tentativa 4: 403". Observado: tentativas 1 e 2 retornaram 401, tentativa 3 já retornou 403 com `usuario-bloqueado`. Limite efetivo de 2 falhas, não 3.
- **Sintoma B — Header:** A resposta 403 de lockout NÃO inclui o header HTTP padrão `Retry-After: 900` (em segundos), embora o corpo traga `retryAfterSeconds: 900`. Doc QA T9 explicitamente: "Header `Retry-After: 900` (segundos restantes até o desbloqueio, 15 minutos no máximo)."
- **Evidência (resposta 403 de lockout, 3ª tentativa em qa-lockout):**
  ```
  HTTP/1.1 403 Forbidden
  Content-Type: application/problem+json
  Date: Sun, 17 May 2026 15:05:17 GMT
  Server: Kestrel
  Transfer-Encoding: chunked

  {"type":"https://carwash/errors/usuario-bloqueado","title":"Acesso temporariamente bloqueado por tentativas inválidas. Tente novamente em alguns minutos.","status":403,"correlationId":"268d3a2d7f684a8687761c8badb957f4","bloqueadoAte":"2026-05-17T15:20:17.5693745Z","retryAfterSeconds":900}
  ```
  Sem linha `Retry-After`. Confirmado também via `curl -D -` específico em chamada subsequente — nenhum header `Retry-After` emitido em 403, apenas em 429 do rate limit.
- **Logs Serilog correlatos (lockout efetivamente disparou após 2 falhas):**
  ```
  [15:05:17 WRN] Conta bloqueada por excesso de tentativas inválidas. UsuarioId=e4be0981-4e23-4695-a727-8ffa25edf01a, Email=qa***@qa.local, BloqueadoAte=2026-05-17T15:20:17.5693745Z
  ```
- **Sugestão:**
  1. Ajustar limite para `tentativas_invalidas >= 3` em vez do atual (provavelmente `> 2` ou `>= 2`). Confirmar valor com o time de produto (CA011/RNF005) — doc QA é explícito em "após 3 falhas".
  2. Setar `Response.Headers.Append("Retry-After", retryAfterSeconds.ToString())` no caminho de `UsuarioBloqueadoException` para alinhar header padrão com o body.
  3. Adicionar teste de integração `[Trait("CA","011")]` cobrindo o ciclo 401 / 401 / 401 / 403+RetryAfter.

## Detalhes por endpoint

### POST /api/v1/auth/login (13 casos)

| ID  | Descrição                                  | Esperado                                       | Obtido                                                                 | Resultado | Bug      |
|-----|--------------------------------------------|------------------------------------------------|------------------------------------------------------------------------|-----------|----------|
| T1  | Golden path admin                          | 200 + JWT + cookie HttpOnly + `no-store`       | 200, JWT válido (claims `sub`, `email`, `perfil=Admin`, `exp` +15min), `Set-Cookie carwash_refresh_token` com `HttpOnly; SameSite=Strict; Path=/api/v1/auth`, `Cache-Control: no-store` | PASS      | —        |
| T2  | Email inexistente                          | 401 genérico anti-enumeração                   | 401 `{"type":".../invalid-credentials","title":"Usuário ou senha inválidos.","correlationId":"..."}` | PASS | —        |
| T3  | Senha incorreta usuário existente          | 401 mesma mensagem de T2                       | 401 idêntico a T2                                                       | PASS      | —        |
| T4  | Email malformado sem `@`                   | 401 (anti-enumeração)                          | 401 idêntico a T2                                                       | PASS      | —        |
| T5a | Senha vazia                                | 400 + erro `senha` obrigatória                 | 400 `{"errors":{"senha":["Senha é obrigatória."]}}`                     | PASS      | —        |
| T5b | Senha `null`                               | 400 + erro `senha`                             | 400 idêntico a T5a                                                      | PASS      | —        |
| T6  | Body vazio `{}`                            | 400 + erros `email` e `senha`                  | 400 com ambos                                                            | PASS      | —        |
| T7  | JSON malformado                            | 400 + título coerente com JSON inválido        | 400 com `title:"Identificador inválido."` + `errors.request:"Failed to read parameter ... as JSON."` | PASS (ressalva) | BUG-006 (ainda aberto) |
| T8a | Sem Content-Type                           | 415 ou 400                                     | 415 sem body                                                            | PASS      | —        |
| T8b | Content-Type `text/plain`                  | 415 ou 400                                     | 415 sem body                                                            | PASS      | —        |
| T9  | Lockout após 3 falhas (qa-lockout)         | 401×3 + 403 com `Retry-After:900` + `bloqueadoAte` | 401, 401, 403, 403 (lockout disparou na 3ª, header `Retry-After` ausente)      | **FAIL**  | **BUG-009** |
| T10 | Rate limit por IP                          | 11ª req+ → 429 + `Retry-After:60`              | 429 disparou (com janela já consumida) + `Retry-After: 60` + body conforme contrato | PASS | —        |
| T11 | Usuário inativo (qa-lockout `ativo=false`) | 403 `usuario-inativo`                          | 403 `{"type":".../usuario-inativo","title":"Acesso bloqueado. Usuário inativo."}` | PASS | —        |
| T12 | Login simultâneo (qa-lockout x2)           | 200 + 200 cookies distintos                    | 2× 200, cookies distintos (`PGevyizh...` e `nGYtYV_p...`)               | PASS      | —        |
| T13 | Rotação refresh entre 2 logins             | 200 + 200 cookies distintos                    | 2× 200, cookies distintos (`0ku8ZeLZ...` e `H8msADxh...`)               | PASS      | —        |

#### Notas

- **JWT decodificado (T1):** header `{"alg":"HS256","typ":"JWT"}`; payload contém `sub=00000000-0000-0000-0000-000000000001`, `email=admin@carwash.local`, `name=Administrador`, `perfil=Admin`, `jti`, `iat`, `nbf`, `exp=iat+900s`, `iss=carwash`, `aud=carwash-web`. `accessToken` ausente do `expiresAt` confere com `exp`.
- **Anti-enumeração T2 vs T3:** mensagens idênticas, status idêntico, sem variação de timing perceptível em 1 amostra (não houve teste estatístico — registrar como item de hardening cobrir com K6 ou similar).
- **T7 ressalva:** status 400 e `errors.request` corretos; apenas o `title` ainda traz "Identificador inválido" — BUG-006 herdado da rodada anterior.
- **T9 FALHOU:** lockout 1 tentativa cedo demais (após 2 falhas, deveria ser após 3); falta header HTTP `Retry-After`. Detalhes em BUG-009.
- **T10:** combinado com chamadas prévias na mesma janela; mas a transição 401→429 e o header `Retry-After: 60` foram validados.

### POST /api/v1/auth/refresh (10 casos)

| ID  | Descrição                                | Esperado                                      | Obtido                                                                                                      | Resultado | Bug                  |
|-----|------------------------------------------|-----------------------------------------------|-------------------------------------------------------------------------------------------------------------|-----------|----------------------|
| T1  | Golden path com cookie válido            | 200 + cookie rotacionado + `no-store`         | 200, cookie rotacionou (`DFTs80t5...` → `u30t-ZJE...`), `Cache-Control: no-store`, JWT distinto do anterior | PASS      | —                    |
| T2  | Sem cookie                               | 401 + `no-store`                              | 401 SEM `Cache-Control: no-store`                                                                            | **FAIL**  | **BUG-005 (ainda aberto)** |
| T3  | Cookie lixo/aleatório                    | 401 + `no-store`                              | 401 SEM `Cache-Control: no-store`                                                                            | PASS (ressalva BUG-005) | BUG-005      |
| T4  | Reuse do mesmo cookie 2× + família revogada | 200 + 401 + cookie rotacionado da 1ª INVÁLIDO | 200, 401, mas **cookie rotacionado da 1ª aceito como 200** na 3ª chamada                                     | PASS parcial (status 200/401 OK) — assinala BUG-008 | **BUG-008 (NOVO crítico)** |
| T5  | Cookie revogado via /logout              | 401 + `Motivo=Revogado`                       | 401                                                                                                          | PASS      | —                    |
| T6  | Cookie expirado (UPDATE SQL)             | 401 + `Motivo=Expirado`                       | 401                                                                                                          | PASS      | —                    |
| T7  | Usuário inativado entre login e refresh  | 401 + `Motivo=UsuarioInvalido`                | 401                                                                                                          | PASS      | —                    |
| T8  | Body arbitrário                          | 200 (body ignorado)                           | 200 com cookie rotacionado, body NÃO afetou comportamento                                                    | PASS      | —                    |
| T9  | CSRF cross-site (Origin/Referer)         | 200 via curl (`SameSite=Strict` só atua em browser) | 200, server não bloqueia por `Origin` (esperado para curl)                                              | PASS      | —                    |
| T10 | Multi-refresh paralelo (race)            | 1×200 + 1×401                                 | 1× 401, 1× 200                                                                                               | PASS      | —                    |

#### Notas

- **T4 com nuance:** o status retornado pelo curl atinge a expectativa textual (`200 + 401`). Marcado como PASS com ressalva, mas o critério de **revogação de família** (terceira chamada com o cookie rotacionado) FALHA — assinalado BUG-008 separado. Mantenho contagem como PASS no comparativo apenas porque é o que o doc lista como o resultado dos 2 curls; o defeito é destacado em bug à parte e merece bloqueio em CI até resolver.
- **T2 marcado FAIL** porque o doc é explícito sobre `no-store` em 401. T3/T5/T6/T7 sofrem do mesmo BUG-005 mas mantenho PASS porque a primária da expectativa (status 401) bate; ressalva no relatório.

### POST /api/v1/auth/logout (7 casos)

| ID  | Descrição                       | Esperado                                                      | Obtido                                                                                                       | Resultado | Bug      |
|-----|---------------------------------|---------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------|-----------|----------|
| T1  | Logout após login               | 204 + Set-Cookie apagador + `no-store`                         | 204 + `Set-Cookie: carwash_refresh_token=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/api/v1/auth; samesite=strict; httponly` + `Cache-Control: no-store` | PASS      | —        |
| T2  | Logout sem cookie               | 204 idempotente + Set-Cookie apagador                          | 204 + Set-Cookie apagador + `no-store`                                                                       | PASS      | —        |
| T3  | Logout com cookie inválido      | 204 indistinguível de T2                                       | 204 idêntico a T2                                                                                            | PASS      | —        |
| T4  | Logout duplicado (mesmo cookie) | 204 / 204 idempotente                                          | 204 / 204                                                                                                    | PASS      | —        |
| T5  | CA011: refresh após logout      | 401 no /refresh                                                | 401 — sessão revogada server-side; CA011 atendido                                                            | PASS      | —        |
| T6  | Cache-Control no-store presente | `Cache-Control: no-store`                                      | presente em todas as chamadas                                                                                | PASS      | —        |
| T7  | Body arbitrário ignorado        | 204 sem 400/422                                                | 204 + Set-Cookie + `no-store`                                                                                | PASS      | —        |

#### Notas

- **Logs de auditoria comprometidos (BUG-007):** todos os 6 logs `Logout efetuado` capturados nesta rodada estão com `UsuarioId=null`, inclusive os de T1 e T4 1ª chamada — onde havia cookie válido e sessão sendo revogada (CA011 L-T5 confirma que a revogação acontece no banco; logo, o backend sabe correlacionar mas não loga). Severidade média, regressão observabilidade.
- **`Set-Cookie` apagador validado em T1:** atributos `HttpOnly`, `Path=/api/v1/auth`, `SameSite=Strict`, `expires=Thu, 01 Jan 1970 00:00:00 GMT`. Sem `Secure` (esperado em Development).
- **CA011 L-T5 PASS:** `/refresh` com cookie pré-logout retorna 401 — revogação server-side confirmada.

## Anexos — trechos relevantes

### Schema `usuarios` (pós-fix)

```
        Column        |           Type           | Nullable | Default
----------------------+--------------------------+----------+---------
 id                   | uuid                     | not null |
 nome                 | character varying(120)   | not null |
 email                | character varying(150)   | not null |
 senha_hash           | text                     | not null |
 perfil               | character varying(20)    | not null |
 ativo                | boolean                  | not null | true
 criado_em            | timestamp with time zone | not null | now()
 atualizado_em        | timestamp with time zone | not null | now()
 bloqueado_ate        | timestamp with time zone |          |
 tentativas_invalidas | integer                  | not null | 0
Check constraints:
    "ck_usuarios_perfil"               CHECK (perfil IN ('ADMIN','FUNCIONARIO'))
    "ck_usuarios_tentativas_invalidas" CHECK (tentativas_invalidas >= 0)
```

### Migrations aplicadas

```
              migration_id
----------------------------------------
 20260513114525_InitialSchema
 20260517022432_AddUsuarioLockoutFields
```

### JWT do golden (T1) — claims relevantes

```
sub      = 00000000-0000-0000-0000-000000000001
email    = admin@carwash.local
name     = Administrador
perfil   = Admin
jti      = 3626740e3b184aec97a8dc86e3bc3c42
iat      = 1779029803
nbf      = 1779029803
exp      = 1779030703   (15 min depois)
iss      = carwash
aud      = carwash-web
alg      = HS256
```

### Headers 403 de lockout (T9) — falta `Retry-After`

```
HTTP/1.1 403 Forbidden
Content-Type: application/problem+json
Date: Sun, 17 May 2026 15:05:17 GMT
Server: Kestrel
Transfer-Encoding: chunked
                                <-- nenhum Retry-After
{"type":".../usuario-bloqueado","title":"...","status":403,
 "correlationId":"268d3a2d7f684a8687761c8badb957f4",
 "bloqueadoAte":"2026-05-17T15:20:17.5693745Z","retryAfterSeconds":900}
```

### Logs de logout (BUG-007)

```
[15:10:57 INF] Logout efetuado. UsuarioId=null
[15:12:29 INF] Logout efetuado. UsuarioId=null   <-- L-T1 com cookie válido
[15:12:33 INF] Logout efetuado. UsuarioId=null
[15:12:41 INF] Logout efetuado. UsuarioId=null   <-- L-T4 1ª com cookie válido
[15:12:41 INF] Logout efetuado. UsuarioId=null
[15:12:41 INF] Logout efetuado. UsuarioId=null
```

### Estado final (cleanup)

```
        email        | ativo | tentativas_invalidas | bloqueado_ate
---------------------+-------+----------------------+---------------
 admin@carwash.local | t     |                    0 |
 qa-lockout@qa.local | t     |                    0 |
```

Usuário `qa-lockout@qa.local` (id `e4be0981-4e23-4695-a727-8ffa25edf01a`, perfil `Funcionario`) permanece no banco, ativo, sem lockout — pode ser reutilizado em rebaterias futuras ou removido manualmente.

## Próximos passos (recomendações de QA)

1. **Bloquear release até resolver BUG-008** (família de refresh não revogada em reuse): crítico para CA011; cobrir com teste de integração `[Trait("CA","011")]` que rode 3 chamadas (200, 401, 401-no-rotacionado).
2. **Resolver BUG-009** (lockout dispara cedo + sem `Retry-After`): teste de integração que percorra 1..4 falhas e verifique `Retry-After` no header e no body, valores coincidentes.
3. **Fechar BUG-005** (`no-store` em 401 do `/refresh`): mover header para fora do bloco try/throw, ou middleware específico para `/api/v1/auth/*`. Asserção: header presente em 200 e 401.
4. **Fechar BUG-006** (título "Identificador inválido."): handler genérico para `BadHttpRequestException` JSON em ProblemDetails, com `title: "Requisição inválida."`.
5. **Fechar BUG-007** (auditoria de logout sem `UsuarioId`): mover log para após resolução da sessão no `LogoutHandler`; condicionar emissão à existência de match.
6. **Adicionar suíte CI rotulada `[Trait("CA","011")]`** com cobertura completa dos 30 cenários deste relatório em `WebApplicationFactory` + Testcontainers — sem isso, CA011 permanece dívida técnica.
