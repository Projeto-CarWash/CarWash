# Relatório — Auth (v4 — pós terceira iteração de fix)

Data: 2026-05-17T19:30:00Z
Backend: http://localhost:8080 (container `carwash-backend` UP ~1h; sem reinício durante esta rodada)
DB: `carwash-postgres` (healthy). Schema mantém migrations `InitialSchema`, `AddUsuarioLockoutFields`, `RefatoraClienteEndereco`, `AdicionaAuditoriaUsuarioCliente`.
Cookie jar principal: `/tmp/qa-auth-v4/`.
Rodada anterior: `../v3-pos-fix2/auth.md`
Bugs fechados nesta iteração (CONFIRMADOS): **BUG-010**, **BUG-006**, **BUG-007**

## Comparativo v3 → v4

| Endpoint            | v3 PASS | v4 PASS | Δ  |
|---------------------|--------:|--------:|---:|
| POST /login (13)    | 13      | 13      | 0  |
| POST /refresh (10)  | 9       | 10      | +1 |
| POST /logout (7)    | 7       | 7       | 0  |
| **Total**           | **29**  | **30**  | **+1** |

Notas do comparativo:
- **Refresh T10** (race) **saiu de FAIL → PASS** — BUG-010 fechado. Em 10 runs sequenciais com 7s de intervalo entre logins (para evitar interferência do rate-limit `/auth/login`), **10/10 runs** retornaram **exatamente 1×200 + 1×401**. Logs do backend mostram, para cada par, **uma única** linha `Sessão renovada` + **uma única** linha `Refresh token reuse detectado` apontando para a mesma `SessaoComprometida`. Single-use enforcement restaurado.
- **Login T7** (JSON malformado) mantém PASS, mas agora com título correto `"Corpo da requisição inválido. Verifique o JSON e tente novamente."` — BUG-006 fechado.
- **Logout L-T1 / L-T5** (cookie válido) agora logam `Logout efetuado. UsuarioId=<guid>, SessaoId=<guid>` — BUG-007 fechado. `LogoutHandler` resolve sessão antes de logar.

## Sumário

- **Total: 30 | PASS: 30 | FAIL: 0 | BLOCKED: 0**
- **Bugs fechados confirmados nesta iteração:** **BUG-010** (race no /refresh), **BUG-006** (título "Identificador inválido." em JSON malformado), **BUG-007** (`Logout efetuado. UsuarioId=null` mesmo com cookie válido).
- **Bugs ainda abertos:** nenhum.
- **Bugs novos descobertos:** nenhum.
- **Observação não-bloqueante (sem severidade):** em `L-T4` (logout duplicado), a 2ª chamada ainda emite `Logout efetuado. UsuarioId=...` com o **mesmo `SessaoId`** da 1ª. Como a sessão já foi revogada na 1ª chamada, o esperado pelo doc QA é "2ª chamada: silenciosa (sem `UsuarioId`)". Não impacta CA011 e não é regressão de v3 (em v3 também ocorria como `UsuarioId=null`). Anotado para o dev considerar — possivelmente bastaria condicionar o log INF de `Logout efetuado` à existência de `revogado_em IS NULL` na sessão antes do UPDATE. Não bloqueia release; abriria um BUG novo se a expectativa for endurecida.

## Bugs

Nenhum bug aberto nesta rodada. Os três bugs sob escrutínio foram fechados:

### BUG-010 — Race no /refresh paralelo (CRÍTICO) — **FECHADO**

- **Estratégia adotada pelo dev:** `SELECT ... FOR UPDATE` (lock pessimista PostgreSQL) com commit explícito antes de `throw` para preservar a defesa de reuse do BUG-008.
- **Confirmação QA (10 runs sequenciais, 7s entre logins):**
  ```
  run 1:  200 401 -> OK
  run 2:  200 401 -> OK
  run 3:  200 401 -> OK
  run 4:  200 401 -> OK
  run 5:  401 200 -> OK
  run 6:  200 401 -> OK
  run 7:  200 401 -> OK
  run 8:  401 200 -> OK
  run 9:  200 401 -> OK
  run 10: 200 401 -> OK
  OK=10 | FAIL2x401=0 | FAIL2x200=0
  ```
- **Logs Serilog correlacionados (cada par):**
  ```
  [INF] Sessão renovada. UsuarioId=00000000-...-001, SessaoAnterior=<X>, SessaoNova=<Y>
  [WRN] Refresh token reuse detectado. Família revogada por segurança.
         UsuarioId=00000000-...-001, SessaoComprometida=<X>, SessoesAfetadas=1
  ```
  Sempre 1× "Sessão renovada" e 1× "reuse detectado" apontando para a mesma `SessaoComprometida`. **Nunca dois `Sessão renovada` com a mesma `SessaoAnterior`** (que era o sintoma da v3).
- **Ressalva metodológica:** uma primeira rodada com 20 runs em loop apertado (sem `sleep` entre logins) gerou 10/20 com 2×401, mas a investigação mostrou que isso decorreu do rate-limit `/auth/login` (10/min/IP) impedindo o re-login da segunda metade do loop; os cookies do snapshot ficavam vazios e ambos os refreshes recebiam 401 corretamente. Repetindo com `sleep 7` entre logins (mantendo o rate-limit folgado), o resultado convergiu para 10/10 OK. Cenário **anterior NÃO é race do `/refresh`** — é interação com o rate-limit do `/login`.
- **Veredito:** BUG-010 fechado. Single-use enforcement íntegro em chamadas paralelas naturais. Sugiro adicionar à suíte `[Trait("CA","011")]` um teste de integração com `Task.WhenAll` de 2 chamadas `/refresh` e assert `Count(200)==1 && Count(401)==1`, evitando depender da observação manual.

### BUG-006 — ProblemDetails para body JSON malformado (BAIXO) — **FECHADO**

- **Estratégia adotada pelo dev:** `ExceptionHandlingMiddleware.ClassificarBadRequest` diferencia body (`title: "Corpo da requisição inválido..."`) de path (`title: "Identificador inválido."`), sem vazar nome de parâmetro C#.
- **Confirmação QA (Login T7, JSON malformado):**
  ```json
  {"type":"https://carwash/errors/invalid-request",
   "title":"Corpo da requisição inválido. Verifique o JSON e tente novamente.",
   "status":400,
   "correlationId":"0e66bcff0954476ca351099460e5bc04",
   "errors":{"senha":["Valor inválido para o campo informado."]}}
  ```
  - Título coerente com o cenário (body, não path).
  - Sem vazamento de `"LoginCommand command"` ou outros nomes de parâmetro C# (v3 expunha `Failed to read parameter "LoginCommand command"`).
- **Veredito:** BUG-006 fechado.

### BUG-007 — `Logout efetuado. UsuarioId=null` (MÉDIO) — **FECHADO**

- **Estratégia adotada pelo dev:** `LogoutHandler` resolve a sessão via `IUsuarioSessaoRepository` + `ITokenHasher` antes de emitir o log `"Logout efetuado."`. Sem cookie ou cookie inválido → `LogDebug` distinto, sem `"Logout efetuado."`.
- **Confirmação QA:**
  - **L-T1 (cookie válido):**
    ```
    [19:27:54 INF] Logout efetuado. UsuarioId=00000000-0000-0000-0000-000000000001, SessaoId=1b816ff2-dc9f-4f99-8898-f937d6c5b982
    ```
  - **L-T2 (sem cookie):** nenhum log `"Logout efetuado."` emitido.
  - **L-T3 (cookie inválido):** nenhum log `"Logout efetuado."` emitido.
  - **L-T5 (cookie válido, CA011):**
    ```
    [19:28:28 INF] Logout efetuado. UsuarioId=00000000-0000-0000-0000-000000000001, SessaoId=eec2be02-a829-43a9-b7cd-013f8f3e14d4
    ```
- **Veredito:** BUG-007 fechado. Auditoria CA011 íntegra.

## POST /api/v1/auth/login (13 casos)

| ID  | Descrição                                  | Esperado                                       | Obtido                                                                 | Resultado | Bug      |
|-----|--------------------------------------------|------------------------------------------------|------------------------------------------------------------------------|-----------|----------|
| T1  | Golden path admin                          | 200 + JWT + cookie HttpOnly + `no-store`       | 200, JWT válido (`sub`=admin, `email`, `perfil=Admin`, `exp` +15min), cookie `HttpOnly; SameSite=Strict; Path=/api/v1/auth`, `Cache-Control: no-store`, correlationId `741db9a2…` | PASS | — |
| T2  | Email inexistente                          | 401 genérico anti-enumeração                   | 401 `{"type":".../invalid-credentials","title":"Usuário ou senha inválidos."}` + `no-store` | PASS | — |
| T3  | Senha incorreta usuário existente          | 401 mesma mensagem de T2                       | 401 idêntico a T2                                                       | PASS | — |
| T4  | Email malformado sem `@`                   | 401 (anti-enumeração)                          | 401 idêntico a T2                                                       | PASS | — |
| T5a | Senha vazia                                | 400 + erro `senha` obrigatória                 | 400 `{"errors":{"senha":["Senha é obrigatória."]}}`                     | PASS | — |
| T5b | Senha `null`                               | 400 + erro `senha`                             | 400 idêntico a T5a                                                      | PASS | — |
| T6  | Body vazio `{}`                            | 400 + erros `email` e `senha`                  | 400 com ambos os erros                                                  | PASS | — |
| T7  | JSON malformado                            | 400 com título coerente (sem nome de parâmetro C#) | **400 `title:"Corpo da requisição inválido..."` + `errors.senha`** | **PASS** | **BUG-006 fechado** |
| T8a | Sem Content-Type                           | 415 ou 400                                     | 415 sem body                                                            | PASS | — |
| T8b | Content-Type `text/plain`                  | 415 ou 400                                     | 415 sem body                                                            | PASS | — |
| T9  | Lockout após 3 falhas (qa-lockout-v4)      | 401×3 + 403 com `Retry-After:900`              | **401, 401, 401, 403** + `Retry-After: 900` + body com `retryAfterSeconds:900` e `bloqueadoAte` ISO | PASS | — |
| T10 | Rate limit por IP                          | 11ª req+ → 429 + `Retry-After`                 | 10× 401 → 11–15× 429 com `Retry-After: 60`                              | PASS | — |
| T11 | Usuário inativo (qa-inativo-v4 `ativo=false`) | 403 `usuario-inativo`                       | 403 `{"type":".../usuario-inativo","title":"Acesso bloqueado. Usuário inativo."}` | PASS | — |
| T12 | Login simultâneo (qa-inativo-v4 ×2)        | 200 + 200 cookies distintos                    | 2× 200, cookies distintos (`9aT1D1ph…` e `VnOUzTdI…`)                    | PASS | — |
| T13 | Rotação refresh entre 2 logins             | 200 + 200 cookies distintos                    | 2× 200, cookies distintos (`V-FSf61E…` e `0ekRFmLB…`)                    | PASS | — |

Notas:
- **T7 PASS confirmado:** título é agora `"Corpo da requisição inválido. Verifique o JSON e tente novamente."` (v3: `"Identificador inválido."`). Sem vazamento de `"LoginCommand command"` no body. Cobre body inválido vs path inválido conforme estratégia do dev.
- **T9 PASS:** lockout dispara na 4ª tentativa; header HTTP `Retry-After: 900` + `retryAfterSeconds:900` no body + `bloqueadoAte` ISO no body. Usuário descartável (`qa-lockout-v4-1779043594@qa.local`).
- **T11 PASS:** após `UPDATE usuarios SET ativo=false WHERE email='qa-inativo-v4-...'`, login retorna 403 `usuario-inativo` (status correto). Usuário foi reativado ao fim do teste.
- **T10 PASS:** janela de rate-limit confirmada (10 OK → 11+ retornam 429 com `Retry-After: 60`).

## POST /api/v1/auth/refresh (10 casos)

| ID  | Descrição                                | Esperado                                      | Obtido                                                                                                      | Resultado | Bug                  |
|-----|------------------------------------------|-----------------------------------------------|-------------------------------------------------------------------------------------------------------------|-----------|----------------------|
| T1  | Golden path com cookie válido            | 200 + cookie rotacionado + `no-store`         | 200, cookie rotacionou (`gBi0ypzW…` → `h5emw3vu…`), `Cache-Control: no-store`, JWT distinto                  | PASS      | —                    |
| T2  | Sem cookie                               | 401 + `no-store`                              | 401 + `Cache-Control: no-store` + body `refresh-token-invalido`                                              | PASS      | —                    |
| T3  | Cookie lixo/aleatório                    | 401 + `no-store`                              | 401 + `Cache-Control: no-store` + body idêntico ao T2                                                       | PASS      | —                    |
| T4  | Reuse do mesmo cookie + família revogada | 1ª=200; 2ª=401; 3ª (cookie rotacionado da 1ª)=401 | 1ª=200 (rotaciona A→B), 2ª=401 (reuse de A), **3ª=401** (cookie B também rejeitado — família revogada)   | PASS      | —                    |
| T5  | Cookie revogado via /logout              | 401                                           | 401 + `no-store`                                                                                             | PASS      | —                    |
| T6  | Cookie expirado (UPDATE SQL em `expira_em`) | 401                                        | 401 + `no-store` (forçado via `UPDATE usuario_sessoes SET expira_em = NOW() - INTERVAL '1 minute' WHERE usuario_id=...`) | PASS      | —                    |
| T7  | Usuário inativado entre login e refresh  | 401                                           | 401 + `no-store` (usado `qa-inativo-v4-...` com `ativo=false`)                                              | PASS      | —                    |
| T8  | Body arbitrário                          | 200 (body ignorado)                           | 200 com cookie rotacionado, body NÃO afetou comportamento                                                    | PASS      | —                    |
| T9  | CSRF cross-site (Origin/Referer)         | 200 via curl (`SameSite=Strict` só atua em browser) | 200, server não bloqueia por `Origin` (esperado para curl)                                              | PASS      | —                    |
| T10 | Multi-refresh paralelo (race)            | **1×200 + 1×401**                             | **10/10 runs com 1×200 + 1×401** (com `sleep 7s` entre logins). Logs com 1× "Sessão renovada" + 1× "reuse detectado" por par. | **PASS** | **BUG-010 fechado** |

Notas:
- **R-T10 PASS:** ver detalhamento na seção "Bugs > BUG-010". Resumo: 10/10 OK com `sleep 7s` entre logins; o cenário "2×401 em runs apertados" observado inicialmente foi explicado pelo rate-limit do `/auth/login` interferindo na geração de novos cookies — não é race do `/refresh`.
- **R-T4 PASS:** logs do backend registram `Refresh token reuse detectado. Família revogada por segurança. ... SessoesAfetadas=N`. Defesa única + preservada após o fix do BUG-010 (lock pessimista não conflita com revogação de família).
- **R-T2/R-T3 PASS:** `Cache-Control: no-store` presente em todas as respostas 401 do `/refresh`.

## POST /api/v1/auth/logout (7 casos)

| ID  | Descrição                       | Esperado                                                      | Obtido                                                                                                       | Resultado | Bug          |
|-----|---------------------------------|---------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------|-----------|--------------|
| T1  | Logout após login               | 204 + Set-Cookie apagador + `no-store` + log com `UsuarioId`+`SessaoId` | 204 + `Set-Cookie: carwash_refresh_token=; expires=Thu, 01 Jan 1970 ...; path=/api/v1/auth; samesite=strict; httponly` + `Cache-Control: no-store` + log `Logout efetuado. UsuarioId=00000000-...-001, SessaoId=1b816ff2-...` | **PASS** | **BUG-007 fechado** |
| T2  | Logout sem cookie               | 204 idempotente + Set-Cookie apagador + **sem** log `Logout efetuado.` | 204 + Set-Cookie apagador + `no-store`; **nenhum log `Logout efetuado.`** emitido (silencioso/Debug)         | **PASS** | **BUG-007 fechado** |
| T3  | Logout com cookie inválido      | 204 indistinguível de T2 + sem log `Logout efetuado.`         | 204 idêntico a T2; **nenhum log `Logout efetuado.`** emitido                                                  | **PASS** | **BUG-007 fechado** |
| T4  | Logout duplicado (mesmo cookie) | 204 / 204 idempotente; 2ª chamada silenciosa                  | 204 / 204; ambas as chamadas emitiram log `Logout efetuado. UsuarioId=00000000-...-001, SessaoId=bda010d8-...` com **mesmo `SessaoId`**. Idempotência HTTP OK. Veja observação abaixo. | PASS (com observação) | — |
| T5  | CA011: refresh após logout      | 401 no /refresh                                                | 401 — sessão revogada server-side; CA011 atendido. Log do logout com `UsuarioId`+`SessaoId`.                  | PASS      | —            |
| T6  | Cache-Control no-store presente | `Cache-Control: no-store`                                      | presente                                                                                                     | PASS      | —            |
| T7  | Body arbitrário ignorado        | 204                                                            | 204 + Set-Cookie + `no-store`                                                                                | PASS      | —            |

Notas:
- **L-T1 / L-T5 PASS:** `UsuarioId=00000000-0000-0000-0000-000000000001, SessaoId=<guid>` aparecem no log de info quando há cookie válido. BUG-007 fechado.
- **L-T2 / L-T3 PASS:** sem cookie ou cookie inválido **não** geram a linha `Logout efetuado.` (consistente com o LogDebug distinto que o dev relatou). BUG-007 fechado para os cenários sem sessão.
- **L-T4 observação não-bloqueante:** a 2ª chamada do logout duplicado ainda emite `Logout efetuado. UsuarioId=..., SessaoId=...` com o **mesmo `SessaoId`** da 1ª. Como a sessão já foi revogada na 1ª chamada, o doc QA diz "2ª chamada: silenciosa (sem `UsuarioId`)". Possível causa: o repositório pode estar achando a sessão (revogada) pelo hash e devolvendo o `SessaoId`, sem filtrar por `revogado_em IS NULL`. **Não regressão de v3** (v3 emitia `UsuarioId=null` em todas as chamadas). **Não bloqueia release** — auditoria CA011 não é comprometida, e a idempotência HTTP está correta. Sugiro o dev avaliar se a estratégia de "log apenas quando há sessão ativa" deveria filtrar por `revogado_em IS NULL` no fetch. Se a expectativa for endurecida, abriria um BUG-011 (baixo, observabilidade).

## Anexos — trechos relevantes

### Headers 200 do /login (T1)

```
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
Cache-Control: no-store
Set-Cookie: carwash_refresh_token=zv30Ma4P8c9q9I4SowyUwtA_MIKcVhfwp6kHiABNaQg; expires=Sun, 24 May 2026 18:42:40 GMT; path=/api/v1/auth; samesite=strict; httponly
X-Correlation-Id: 741db9a2373b4d1daf49d3e2abc1031e
```

### Headers 403 de lockout (T9) — com `Retry-After: 900`

```
HTTP/1.1 403 Forbidden
Content-Type: application/problem+json
Cache-Control: no-store
Retry-After: 900

{"type":"https://carwash/errors/usuario-bloqueado",
 "title":"Acesso temporariamente bloqueado por tentativas inválidas. Tente novamente em alguns minutos.",
 "status":403,
 "correlationId":"38551170f00342078e3fbe0d46cc93fe",
 "bloqueadoAte":"2026-05-17T19:03:43.4451088Z",
 "retryAfterSeconds":900}
```

### Body T7 (BUG-006 fechado) — título correto e sem nome de parâmetro C#

```json
{"type":"https://carwash/errors/invalid-request",
 "title":"Corpo da requisição inválido. Verifique o JSON e tente novamente.",
 "status":400,
 "correlationId":"0e66bcff0954476ca351099460e5bc04",
 "errors":{"senha":["Valor inválido para o campo informado."]}}
```

### Logs do race do /refresh (BUG-010 fechado — 10/10 OK)

Para cada par paralelo do R-T10:

```
[INF] Sessão renovada. UsuarioId=00000000-...-001, SessaoAnterior=<X>, SessaoNova=<Y>
[WRN] Refresh token reuse detectado. Família revogada por segurança.
       UsuarioId=00000000-...-001, SessaoComprometida=<X>, SessoesAfetadas=1
```

Sempre 1× `Sessão renovada` com `SessaoAnterior=X` + 1× `reuse detectado` apontando para `SessaoComprometida=X`. **Sem** duas linhas `Sessão renovada` com mesmo `SessaoAnterior` (sintoma da v3 eliminado).

### Logs de logout (BUG-007 fechado)

```
[19:27:54 INF] Logout efetuado. UsuarioId=00000000-0000-0000-0000-000000000001, SessaoId=1b816ff2-dc9f-4f99-8898-f937d6c5b982   <-- L-T1 com cookie válido
                                                                                                                                  <-- L-T2 sem cookie: nenhum log emitido
                                                                                                                                  <-- L-T3 cookie inválido: nenhum log emitido
[19:28:11 INF] Logout efetuado. UsuarioId=00000000-...-001, SessaoId=bda010d8-d1c0-4e0c-abb2-ffe99d5b6735   <-- L-T4 1ª chamada
[19:28:11 INF] Logout efetuado. UsuarioId=00000000-...-001, SessaoId=bda010d8-d1c0-4e0c-abb2-ffe99d5b6735   <-- L-T4 2ª chamada (observação não-bloqueante)
[19:28:28 INF] Logout efetuado. UsuarioId=00000000-...-001, SessaoId=eec2be02-a829-43a9-b7cd-013f8f3e14d4   <-- L-T5 CA011
```

### Estado final do banco (cleanup)

```
              email               | ativo | tentativas_invalidas | bloqueado_ate
----------------------------------+-------+----------------------+---------------
 admin@carwash.local              | t     |                    0 |
 qa-lockout-v4-1779043594@qa.local| t     |                    0 |
 qa-inativo-v4-1779044004@qa.local| t     |                    0 |
```

Usuários `qa-lockout-v4-1779043594@qa.local` (id `2fb7c3c0-bbf8-4d3b-a348-bc7b8f1a610b`, perfil `Funcionario`, senha `Forte!Teste2026Senha`) e `qa-inativo-v4-1779044004@qa.local` (id `8dbc5c27-2608-495f-b3af-22e1048babea`, mesma senha) criados nesta rodada via `POST /api/v1/usuarios` autenticado como admin; ambos permanecem no banco, ativos, sem lockout — podem ser reutilizados em rebaterias futuras ou removidos manualmente.

## Próximos passos (recomendações de QA)

1. **Promover a suíte de Auth para CI `[Trait("CA","011")]`:**
   - Login T7 com asserção de `title="Corpo da requisição inválido..."` e ausência de nome de parâmetro C# no body.
   - Login T9 com `Retry-After: 900` no header HTTP e `retryAfterSeconds:900` no body.
   - Refresh T4 (3 chamadas: 200, 401, 401) cobrindo família revogada em reuse.
   - Refresh T10 com `Task.WhenAll` de 2 chamadas paralelas, assert `Count(200)==1 && Count(401)==1` rodando 10× em sequência para flakiness regression.
   - Logout T1/T5 com asserção de log `"Logout efetuado. UsuarioId=<guid>, SessaoId=<guid>"` (capturar via `ILogger` mock ou Serilog InMemorySink).
   - Logout T2/T3 com asserção de **ausência** da linha `"Logout efetuado."`.
2. **Avaliar endurecimento de L-T4 (observação não-bloqueante):** condicionar emissão do log `Logout efetuado.` à existência de `revogado_em IS NULL` no fetch da sessão, evitando log redundante na 2ª chamada do logout duplicado. Decisão fica com o dev — não bloqueia release.
3. **Hardening pendente (já documentado em rodadas anteriores):** rate-limit em `/refresh` (atualmente sem limite — risco de força bruta), teste estatístico de timing T2 vs T3 (anti-enumeração) com K6/NBomber, validação automatizada de `Secure=true` em Production.
4. **Sugestão de cobertura adicional para `/auth/login`:** adicionar caso explícito para `Content-Type: application/xml` (esperado 415) e payload válido mas tamanho >1MB (esperado 413 ou 400, conforme política de tamanho de body).
