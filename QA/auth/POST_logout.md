# POST /api/v1/auth/logout

## Resumo

- **Método:** POST
- **Path:** `/api/v1/auth/logout`
- **Propósito:** Revogar a sessão ativa do usuário, invalidando o refresh token no servidor e apagando o cookie `carwash_refresh_token` no cliente.
- **Autenticação:** Anônimo (não exige Authorization). Aceita requisição com ou sem cookie de sessão.
- **Idempotência:** Sim. Chamadas repetidas, sem cookie ou com cookie já revogado retornam o mesmo resultado.
- **Produces:** `204 No Content` (sucesso, sem body) | `500 Internal Server Error` (apenas em falha inesperada de infraestrutura).
- **Headers de resposta esperados:**
  - `Set-Cookie: carwash_refresh_token=; expires=Thu, 01 Jan 1970 00:00:00 GMT; HttpOnly; Path=/api/v1/auth; SameSite=Strict`
  - `Cache-Control: no-store`

## Pré-requisitos

- Backend em execução em `http://localhost:8080`.
- Para casos T1, T3, T4, T5: realizar login prévio gravando o cookie em arquivo:

```bash
curl -s -c /tmp/carwash-cookies.txt \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@carwash.local","senha":"Senha@123"}' \
  http://localhost:8080/api/v1/auth/login
```

- Para casos T2, T6, T7: nenhum cookie é necessário (ou usar um jar vazio).
- `jq` instalado para inspeção opcional de respostas.

## Tabela resumo

| Caso | Cenário | Cookie enviado | Status esperado | Set-Cookie apagador | Observação |
|------|---------|----------------|------------------|---------------------|------------|
| T1   | Logout golden path após login | Sim, válido     | 204 | Sim | Sessão revogada no DB |
| T2   | Logout sem cookie             | Não             | 204 | Sim | Idempotente, sem log de sessão |
| T3   | Logout com cookie inválido    | Sim, inválido   | 204 | Sim | Não vaza existência de sessão |
| T4   | Logout duplicado              | Sim, válido (2x)| 204 / 204 | Sim / Sim | Segunda chamada sem efeito server-side |
| T5   | Refresh após logout (CA011)   | Cookie antigo   | 401 no /refresh | N/A | Crítico: revogação server-side |
| T6   | Verificação de Cache-Control  | Não             | 204 | Sim | Header `Cache-Control: no-store` obrigatório |
| T7   | Body arbitrário no POST       | Não             | 204 | Sim | Body deve ser ignorado |

## Detalhamento por caso

### T1 — Golden path: login seguido de logout com cookie

```bash
# Login (gera cookie)
curl -s -c /tmp/carwash-cookies.txt \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@carwash.local","senha":"Senha@123"}' \
  http://localhost:8080/api/v1/auth/login

# Logout enviando o cookie
curl -i -s -b /tmp/carwash-cookies.txt -c /tmp/carwash-cookies.txt \
  -X POST \
  http://localhost:8080/api/v1/auth/logout
```

**Resposta esperada:**

```
HTTP/1.1 204 No Content
Cache-Control: no-store
Set-Cookie: carwash_refresh_token=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/api/v1/auth; secure; httponly; samesite=strict
```

- Sem body.
- Cookie `carwash_refresh_token` deve ser sobrescrito com data no passado.
- Atributos obrigatórios no `Set-Cookie`: `HttpOnly`, `Path=/api/v1/auth`, `SameSite=Strict`, `expires=Thu, 01 Jan 1970 00:00:00 GMT`.

**Logs Serilog esperados:**

```
[INF] Logout efetuado. UsuarioId=<guid>
[INF] Auditoria: UsuarioLogout UsuarioId=<guid>
```

### T2 — Logout sem cookie (idempotente)

```bash
curl -i -s -X POST http://localhost:8080/api/v1/auth/logout
```

**Resposta esperada:**

```
HTTP/1.1 204 No Content
Cache-Control: no-store
Set-Cookie: carwash_refresh_token=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/api/v1/auth; secure; httponly; samesite=strict
```

- Sem body.
- Mesmo sem sessão, o backend retorna 204 e ainda emite o `Set-Cookie` apagador (idempotência).

**Logs Serilog esperados:**

- Logout silencioso. Não deve existir entrada `"Logout efetuado. UsuarioId=..."` nem auditoria `UsuarioLogout`, já que não há sessão associada.

### T3 — Logout com cookie inválido ou já revogado

```bash
curl -i -s \
  -H "Cookie: carwash_refresh_token=token-invalido-ou-ja-revogado" \
  -X POST \
  http://localhost:8080/api/v1/auth/logout
```

**Resposta esperada:**

```
HTTP/1.1 204 No Content
Cache-Control: no-store
Set-Cookie: carwash_refresh_token=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/api/v1/auth; secure; httponly; samesite=strict
```

- A resposta deve ser indistinguível de T2 do ponto de vista do cliente. Não vazar se a sessão existia ou não (proteção contra enumeração).

**Logs Serilog esperados:**

- Pode haver log de tentativa de revogação sem match (debug/info), porém sem `UsuarioId` quando o token não corresponde a nenhuma sessão.

### T4 — Logout duplicado (duas chamadas seguidas)

```bash
# Login
curl -s -c /tmp/carwash-cookies.txt \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@carwash.local","senha":"Senha@123"}' \
  http://localhost:8080/api/v1/auth/login

# Primeira chamada
curl -i -s -b /tmp/carwash-cookies.txt -X POST http://localhost:8080/api/v1/auth/logout

# Segunda chamada (mesmo cookie original)
curl -i -s -b /tmp/carwash-cookies.txt -X POST http://localhost:8080/api/v1/auth/logout
```

**Resposta esperada:**

- Ambas as chamadas: `204 No Content`, com `Set-Cookie` apagador e `Cache-Control: no-store`.
- A segunda chamada não deve provocar 500 nem 401, mesmo já tendo havido revogação na primeira.

**Logs Serilog esperados:**

- 1ª chamada: `"Logout efetuado. UsuarioId=<guid>"` + `UsuarioLogout`.
- 2ª chamada: silenciosa (sem `UsuarioId`), pois a sessão já não existe ativa no DB.

### T5 — CA011: revogação server-side validada via /refresh

Esse é o teste crítico do CA011: garantir que o cookie de refresh anterior não funciona mais após logout.

```bash
# 1) Login (captura o cookie original)
curl -s -c /tmp/carwash-cookies.txt \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@carwash.local","senha":"Senha@123"}' \
  http://localhost:8080/api/v1/auth/login

# Backup do cookie original (antes do logout sobrescrever)
cp /tmp/carwash-cookies.txt /tmp/carwash-cookies-antes-logout.txt

# 2) Logout
curl -i -s -b /tmp/carwash-cookies.txt -c /tmp/carwash-cookies.txt \
  -X POST http://localhost:8080/api/v1/auth/logout

# 3) Tentar refresh com o cookie antigo (pre-logout)
curl -i -s -b /tmp/carwash-cookies-antes-logout.txt \
  -X POST http://localhost:8080/api/v1/auth/refresh
```

**Resposta esperada no /refresh:**

```
HTTP/1.1 401 Unauthorized
```

- Deve retornar `401 Unauthorized`. Se retornar 200 com novo token, a revogação server-side está quebrada (bug crítico CA011).

**Logs Serilog esperados:**

- No logout: `"Logout efetuado. UsuarioId=<guid>"`.
- No refresh subsequente: log de falha do tipo `"Refresh negado: sessao revogada"` ou `"Refresh token invalido"`, sem renovação.

### T6 — Header Cache-Control: no-store presente

```bash
curl -i -s -X POST http://localhost:8080/api/v1/auth/logout | grep -i 'cache-control'
```

**Resposta esperada:**

```
Cache-Control: no-store
```

- O header `Cache-Control: no-store` deve estar **sempre** presente, independentemente da existência de cookie.
- Status da chamada: 204.

### T7 — Body arbitrário no POST é ignorado

```bash
curl -i -s -X POST \
  -H "Content-Type: application/json" \
  -d '{"foo":"bar","sessao":"123"}' \
  http://localhost:8080/api/v1/auth/logout
```

**Resposta esperada:**

```
HTTP/1.1 204 No Content
Cache-Control: no-store
Set-Cookie: carwash_refresh_token=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/api/v1/auth; secure; httponly; samesite=strict
```

- Body de request deve ser ignorado pelo handler `LogoutAsync`.
- Sem 400/422 por payload desconhecido.

**Logs Serilog esperados:**

- Sem log de sessão (não há cookie). Sem warnings de desserialização.

## Bugs e crashes a observar

- **500 ao chamar /logout sem cookie:** comportamento esperado é 204 (idempotente). Qualquer 500 deve ser reportado.
- **Set-Cookie ausente na resposta:** o backend deve sempre emitir o cookie apagador, mesmo quando não há sessão.
- **Sessão não invalidada server-side (CA011 crítico):** se T5 retornar 200/novo token no /refresh com o cookie pré-logout, há vazamento de sessão — bug bloqueante.
- **204 retornando com body:** `204 No Content` por contrato HTTP não pode ter corpo. Reportar se houver bytes após os headers.
- **Cache-Control: no-store ausente:** risco de proxy intermediário cachear resposta com `Set-Cookie`. Reportar.
- **Set-Cookie sem `HttpOnly`:** quebra de segurança (cookie acessível via JS). Reportar.
- **Set-Cookie sem `Path=/api/v1/auth` ou sem `SameSite=Strict`:** o cookie apagador deve casar exatamente com os atributos do cookie original; caso contrário, o navegador não substitui.
- **Logout retornando 401/403:** o endpoint é `AllowAnonymous`. Qualquer exigência de Authorization é regressão.
- **Duplicidade de logs `UsuarioLogout` para a mesma sessão:** segunda chamada de T4 não deve gerar nova entrada de auditoria.
- **Throws não tratados quando cookie tem formato inesperado** (string vazia, caracteres não-ASCII, tamanho enorme): deve retornar 204, não 500.

## Como reportar para o dev

Ao identificar uma divergência, abrir issue/ticket com:

1. **Caso afetado:** identificador (T1..T7) e cenário.
2. **Comando exato executado:** bloco bash usado, incluindo cookie/headers.
3. **Resposta obtida:** status, headers completos (`curl -i`) e body se houver.
4. **Resposta esperada:** conforme este documento (status, `Set-Cookie`, `Cache-Control`).
5. **Logs Serilog do backend:** trecho do log no instante da requisição (timestamp + nível + mensagem).
6. **Reprodutibilidade:** se ocorre 1/1, 1/N ou apenas sob concorrência.
7. **Severidade sugerida:**
   - Crítico (bloqueia release): T5 falhando (sessão não revogada), Set-Cookie ausente, 500 em cenário idempotente.
   - Alto: header de segurança ausente (`HttpOnly`, `SameSite`, `Cache-Control`).
   - Médio: ausência de log de auditoria `UsuarioLogout`.
   - Baixo: ruído de log adicional sem impacto funcional.
8. **Critério de aceite impactado:** referenciar CA011 quando envolver revogação server-side ou auditoria.
9. **Anexar request/response brutos** (saída de `curl -i -s`) para facilitar diagnóstico.
