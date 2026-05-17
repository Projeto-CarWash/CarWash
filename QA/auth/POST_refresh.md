# POST /api/v1/auth/refresh

Renova o access token JWT do usuário autenticado a partir do cookie HttpOnly `carwash_refresh_token`. O endpoint é anônimo do ponto de vista de autorização (não exige Bearer), mas exige posse do cookie de refresh emitido em um `/auth/login` anterior. O refresh é rotacionado a cada chamada (single-use por família).

- **Método:** `POST`
- **Path:** `/api/v1/auth/refresh`
- **Autenticação:** cookie-based (`carwash_refresh_token`), sem `Authorization: Bearer`.
- **Produces:** `200 OK` (`RefreshResponse`), `401 Unauthorized` (`ProblemDetails`), `500 Internal Server Error` (apenas em falha não tratada).
- **Headers de resposta esperados:** `Set-Cookie: carwash_refresh_token=...` (rotacionado) e `Cache-Control: no-store`.
- **Body de resposta (200):**

```json
{
  "accessToken": "eyJhbGciOi...",
  "expiresAt": "2026-05-17T12:00:00Z",
  "usuario": {
    "id": "uuid",
    "nome": "string",
    "email": "string",
    "perfil": "Administrador|Gerente|Operador"
  }
}
```

> Observação extra de risco: diferente de `/auth/login`, este endpoint NÃO tem `RequireRateLimiting`. Pode ser explorado para força bruta de tokens de refresh ou para enumerar sessões. Registrar como item de hardening pendente.

## Pré-requisitos

- Backend rodando em `http://localhost:8080` (ambiente Development/Testing — `Secure=false` no cookie, portanto HTTP local funciona).
- Jar de cookies inicial obtido via login válido:

```bash
curl -s -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -c /tmp/carwash-cookies.txt \
  -d '{"email":"admin@carwash.local","senha":"Senha@123"}' | jq .
```

- Confirmar que o cookie foi salvo:

```bash
grep carwash_refresh_token /tmp/carwash-cookies.txt
```

- Acesso ao banco PostgreSQL para os casos T6/T7 (manipulação de `RefreshTokens` e `Usuarios`).
- `jq` instalado para extração de campos.

## Tabela resumo dos casos

| ID  | Cenário                                              | Status esperado | Validações principais                                            |
|-----|------------------------------------------------------|-----------------|------------------------------------------------------------------|
| T1  | Golden path com cookie válido                        | 200             | Novo `accessToken`, cookie rotacionado, `Cache-Control: no-store` |
| T2  | Sem cookie de refresh                                | 401             | `ProblemDetails` sem stack, sem `Set-Cookie`                     |
| T3  | Cookie com valor lixo/aleatório                      | 401             | `ProblemDetails` limpo, sem 500                                  |
| T4  | Reuse do mesmo cookie inicial duas vezes             | 200 + 401       | 2ª chamada falha; família revogada nos logs                      |
| T5  | Cookie de sessão previamente revogada via /logout    | 401             | Log `UsuarioSessaoRefreshFalha`                                  |
| T6  | Cookie expirado (TTL 7 dias)                         | 401             | Log com `Motivo=Expirado`                                        |
| T7  | Usuário inativado entre login e refresh              | 401             | Log com `Motivo=UsuarioInvalido`                                 |
| T8  | Body com payload arbitrário                          | 200             | Body ignorado, mesmo comportamento de T1                         |
| T9  | CSRF cross-site (Origin/Referer forjado)             | 200 via curl    | Limitação documentada; SameSite=Strict só protege em navegador   |
| T10 | Multi-refresh paralelo (concorrência)                | 1x200 + 1x401   | Apenas uma chamada vence; race documentada                       |

---

## T1 — Golden path

**Pré-condição:** login executado com sucesso, `carwash_refresh_token` presente em `/tmp/carwash-cookies.txt`.

```bash
# Captura o valor do cookie antes do refresh
ANTES=$(grep carwash_refresh_token /tmp/carwash-cookies.txt | awk '{print $7}')

curl -s -X POST http://localhost:8080/api/v1/auth/refresh \
  -b /tmp/carwash-cookies.txt \
  -c /tmp/carwash-cookies.txt \
  -i

# Captura o valor do cookie depois
DEPOIS=$(grep carwash_refresh_token /tmp/carwash-cookies.txt | awk '{print $7}')
test "$ANTES" != "$DEPOIS" && echo "ROTACIONADO OK" || echo "BUG: cookie NAO rotacionou"
```

**Resposta esperada:**

- Status: `200 OK`.
- Headers contêm:
  - `Cache-Control: no-store`
  - `Set-Cookie: carwash_refresh_token=<novo valor>; expires=...; httponly; samesite=strict`
- Body:

```json
{
  "accessToken": "eyJ...",
  "expiresAt": "2026-05-17T...Z",
  "usuario": { "id": "...", "nome": "...", "email": "...", "perfil": "Administrador" }
}
```

**Logs Serilog esperados:**

- `Sessão renovada` com `@Event = "UsuarioSessaoRenovada"`, `UsuarioId`, `RefreshTokenIdAntigo`, `RefreshTokenIdNovo`.

**Sinais de bug:**

- Cookie igual antes/depois (não rotacionou).
- Body contendo `refreshToken` em texto claro.
- `Cache-Control` ausente ou `public`.

---

## T2 — Sem cookie de refresh

**Pré-condição:** jar de cookies vazio.

```bash
: > /tmp/carwash-empty.txt

curl -s -X POST http://localhost:8080/api/v1/auth/refresh \
  -b /tmp/carwash-empty.txt \
  -c /tmp/carwash-empty.txt \
  -i
```

**Resposta esperada:**

- Status: `401 Unauthorized`.
- Body: `ProblemDetails` com `title` genérico (ex.: `"Refresh token inválido"`), sem stack trace.
- Sem header `Set-Cookie` na resposta.
- `Cache-Control: no-store` continua presente.

**Logs Serilog esperados:**

- `UsuarioSessaoRefreshFalha` com `Motivo=CookieAusente`.

**Sinais de bug:**

- 200 quando não há cookie (falha grave de autenticação).
- 500 em vez de 401.
- Stack trace exposto no body.

---

## T3 — Cookie com valor lixo/aleatório

**Pré-condição:** forjar cookie inválido.

```bash
cat > /tmp/carwash-lixo.txt <<'EOF'
# Netscape HTTP Cookie File
localhost	FALSE	/	FALSE	0	carwash_refresh_token	LIXO-NAO-EXISTE-NO-BANCO-1234567890
EOF

curl -s -X POST http://localhost:8080/api/v1/auth/refresh \
  -b /tmp/carwash-lixo.txt \
  -c /tmp/carwash-lixo.txt \
  -i
```

**Resposta esperada:**

- Status: `401 Unauthorized`.
- Body: `ProblemDetails` limpo, sem detalhes internos.
- Sem `Set-Cookie` rotacionado.

**Logs Serilog esperados:**

- `UsuarioSessaoRefreshFalha` com `Motivo=TokenNaoEncontrado` ou `Motivo=Invalido`.

**Sinais de bug:**

- Resposta 500 com stack trace.
- Vazamento do hash do token nos logs em nível Information.
- Aceitar o token (200) — falha catastrófica.

---

## T4 — Reuse do mesmo cookie (token reuse detection)

**Pré-condição:** salvar uma cópia do jar logo após o login.

```bash
cp /tmp/carwash-cookies.txt /tmp/carwash-snapshot.txt

# Primeira chamada — usa o snapshot, sucesso esperado
curl -s -X POST http://localhost:8080/api/v1/auth/refresh \
  -b /tmp/carwash-snapshot.txt \
  -c /tmp/carwash-rotated.txt \
  -i

# Segunda chamada — REUSA o snapshot ANTIGO (o cookie ja foi consumido)
curl -s -X POST http://localhost:8080/api/v1/auth/refresh \
  -b /tmp/carwash-snapshot.txt \
  -c /tmp/carwash-rotated2.txt \
  -i
```

**Resposta esperada:**

- 1ª chamada: `200 OK`.
- 2ª chamada: `401 Unauthorized`. Toda a família de refresh deve ser revogada (i.e., o cookie rotacionado emitido na 1ª chamada também passa a ser inválido, por política de segurança).

**Logs Serilog esperados:**

- 1ª chamada: `UsuarioSessaoRenovada`.
- 2ª chamada: `UsuarioSessaoRefreshFalha` com `Motivo=ReuseDetectado` (ou equivalente) e log de família revogada.

**Sinais de bug:**

- 2ª chamada retorna 200 — significa que não há single-use enforcement; vulnerabilidade séria.
- Família NÃO é revogada (cookie rotacionado da 1ª chamada continua válido após reuse detectado).

---

## T5 — Cookie de sessão revogada via /logout

**Pré-condição:** após login válido, chamar `/auth/logout`, restaurar o cookie revogado e tentar usar.

```bash
# Snapshot do cookie ANTES do logout
cp /tmp/carwash-cookies.txt /tmp/carwash-pre-logout.txt

curl -s -X POST http://localhost:8080/api/v1/auth/logout \
  -b /tmp/carwash-cookies.txt \
  -c /tmp/carwash-cookies.txt \
  -i

# Tentar refresh com o cookie revogado
curl -s -X POST http://localhost:8080/api/v1/auth/refresh \
  -b /tmp/carwash-pre-logout.txt \
  -c /tmp/carwash-tentativa.txt \
  -i
```

**Resposta esperada:**

- Status: `401 Unauthorized`.
- Sem `Set-Cookie` rotacionado.

**Logs Serilog esperados:**

- `UsuarioSessaoRefreshFalha` com `Motivo=Revogado`.

**Sinais de bug:**

- 200 após logout — sessão zumbi, falha grave.

---

## T6 — Cookie expirado (TTL 7 dias = 604800s)

**Pré-condição:** três caminhos possíveis. Documentar a forma escolhida no relatório.

### (a) Esperar passivamente

Inviável em QA. Apenas mencionar.

### (b) Manipular `expires_at` no PostgreSQL

```bash
# Pega o ID do refresh atual (extrair do log ou via tabela)
psql -h localhost -U carwash -d carwash -c \
  "UPDATE \"RefreshTokens\" SET \"ExpiresAt\" = NOW() - INTERVAL '1 minute' WHERE \"Revoked\" = false AND \"UsuarioId\" = '<UUID>';"

curl -s -X POST http://localhost:8080/api/v1/auth/refresh \
  -b /tmp/carwash-cookies.txt \
  -c /tmp/carwash-cookies.txt \
  -i
```

### (c) Adiantar o relógio do container

```bash
docker exec carwash-api date -s "+8 days"
curl -s -X POST http://localhost:8080/api/v1/auth/refresh \
  -b /tmp/carwash-cookies.txt -c /tmp/carwash-cookies.txt -i
docker exec carwash-api date -s "-8 days"  # restaurar
```

**Resposta esperada:**

- Status: `401 Unauthorized`.

**Logs Serilog esperados:**

- `UsuarioSessaoRefreshFalha` com `Motivo=Expirado`.

**Sinais de bug:**

- 200 após expiração: falha de validação de `ExpiresAt`.
- 500 ao parsear data inválida.

---

## T7 — Usuário inativado entre login e refresh

**Pré-condição:** login válido, depois desativar o usuário no banco.

```bash
psql -h localhost -U carwash -d carwash -c \
  "UPDATE \"Usuarios\" SET \"Ativo\" = false WHERE \"Email\" = 'admin@carwash.local';"

curl -s -X POST http://localhost:8080/api/v1/auth/refresh \
  -b /tmp/carwash-cookies.txt \
  -c /tmp/carwash-cookies.txt \
  -i

# Reativar para não poluir os próximos testes
psql -h localhost -U carwash -d carwash -c \
  "UPDATE \"Usuarios\" SET \"Ativo\" = true WHERE \"Email\" = 'admin@carwash.local';"
```

**Resposta esperada:**

- Status: `401 Unauthorized`.

**Logs Serilog esperados:**

- `UsuarioSessaoRefreshFalha` com `Motivo=UsuarioInvalido` (ou `UsuarioInativo`).

**Sinais de bug:**

- 200 com usuário desativado: bypass de controle de acesso.
- Mensagem detalhada expondo estado do usuário.

---

## T8 — Body com payload arbitrário

**Pré-condição:** cookie válido. O endpoint não declara `[FromBody]`, portanto o corpo deve ser ignorado.

```bash
curl -s -X POST http://localhost:8080/api/v1/auth/refresh \
  -b /tmp/carwash-cookies.txt \
  -c /tmp/carwash-cookies.txt \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"tentativa-de-injecao","admin":true,"foo":"bar"}' \
  -i
```

**Resposta esperada:**

- Status: `200 OK`, idêntico ao T1. Body da requisição é descartado.
- Cookie rotacionado.

**Logs Serilog esperados:**

- `UsuarioSessaoRenovada` normal.

**Sinais de bug:**

- 400/415 — endpoint passou a exigir body, regressão de contrato.
- Algum campo do body influenciar o comportamento (ex.: `admin=true` elevar privilégios) — vulnerabilidade gravíssima.

---

## T9 — CSRF cross-site (Origin/Referer)

**Pré-condição:** simular um pedido vindo de outro domínio.

```bash
curl -s -X POST http://localhost:8080/api/v1/auth/refresh \
  -b /tmp/carwash-cookies.txt \
  -c /tmp/carwash-cookies.txt \
  -H "Origin: https://evil.example.com" \
  -H "Referer: https://evil.example.com/attack.html" \
  -i
```

**Resposta esperada (via curl):**

- Status: `200 OK`. Em curl não há contexto de site, então `SameSite=Strict` não atua.

**Limitação documentada:**

- A proteção real contra CSRF aqui vem do atributo `SameSite=Strict` do cookie, que só é enforçado pelo navegador. Em curl/Postman o ataque é trivial porque a ferramenta não respeita SameSite.
- Para validar CSRF de verdade, complementar com teste Playwright cross-origin (ver suíte E2E). Em uma página servida por origem diferente, o navegador NÃO deve anexar o cookie.

**Logs Serilog esperados:**

- `UsuarioSessaoRenovada` (não há bloqueio server-side por Origin).

**Sinais de bug:**

- Se o backend começar a validar `Origin`/`Referer` server-side, mudar a expectativa.
- Cookie sem `SameSite=Strict` em produção (validar no `Set-Cookie` de Production, não em Dev).

---

## T10 — Multi-refresh paralelo (race condition)

**Pré-condição:** cookie válido. Disparar duas chamadas simultâneas usando o MESMO cookie inicial.

```bash
cp /tmp/carwash-cookies.txt /tmp/carwash-race.txt

printf '%s\n%s\n' \
  "http://localhost:8080/api/v1/auth/refresh" \
  "http://localhost:8080/api/v1/auth/refresh" | \
  xargs -P2 -n1 -I{} curl -s -o /dev/null -w "%{http_code}\n" \
    -X POST {} \
    -b /tmp/carwash-race.txt
```

**Resposta esperada:**

- Saída: uma linha `200` e uma linha `401`.
- Apenas um vencedor; o perdedor recebe 401 porque o token já foi rotacionado/marcado como usado pela transação que venceu.
- A família NÃO deve ser revogada por esta corrida natural (diferente de T4, onde há reuse explícito após sucesso).

**Logs Serilog esperados:**

- 1x `UsuarioSessaoRenovada`.
- 1x `UsuarioSessaoRefreshFalha` com `Motivo=ConcorrenciaPerdida` ou equivalente.

**Sinais de bug:**

- Duas respostas `200` — quebrou single-use, vulnerabilidade.
- Dois `Set-Cookie` rotacionados — família duplicada.
- 500 por deadlock ou violação de constraint não tratada.
- Família ser revogada nesta corrida natural (falso positivo de reuse).

---

## Bugs e crashes a observar

- **Cookie não rotacionado:** valor de `carwash_refresh_token` igual antes e depois do `/refresh`. Indica falha em `EscreverRefreshCookie` ou rotação desativada.
- **Refresh aceito sem cookie:** 200 em T2 — falha grave de autenticação.
- **500 em vez de 401 com cookie lixo:** exceção não capturada em `RefreshTokenInvalidoException`.
- **Header `Cache-Control: no-store` ausente:** risco de cache de proxy/CDN expor o `accessToken`.
- **Vazamento de stack trace:** body de erro contendo `at CarWash.Application...` — vazamento de stack em produção/homologação.
- **Reuse aceito:** T4 retornando 200 na segunda chamada. Sem detecção de reuse, single-use enforcement quebrado.
- **Família não revogada em reuse:** após reuse detectado, o token rotacionado válido deveria também ser invalidado.
- **Body do request influenciar comportamento:** T8 mostrando que campos JSON afetam resposta.
- **Race de T10 emitindo 2x 200:** dois access tokens válidos a partir de um refresh.
- **Falta de rate-limit:** endpoint permite milhares de tentativas/segundo (já registrado como risco extra).
- **`Secure=false` em Production:** validar via `dotnet run --environment Production` — em prod o cookie precisa ser `Secure`.
- **`SameSite` diferente de `Strict`:** abre brecha para CSRF.

## Como reportar para o dev

Ao abrir issue/PR comentário para `dev-dotnet-carwash`, anexar:

1. **ID do caso** (T1..T10) e ambiente (`Development` / `Testing`).
2. **Comando curl exato** usado (com placeholders para tokens) e timestamp UTC.
3. **Resposta completa:** status, headers (especialmente `Set-Cookie` e `Cache-Control`) e body. Mascarar o valor do refresh.
4. **Diff esperado vs obtido.** Ex.: "Esperado 401 com `Motivo=Expirado`, obtido 200 com novo accessToken."
5. **Trecho de log Serilog correlacionado** pelo `TraceId` da request (`X-Trace-Id` no header de resposta ou `traceparent`).
6. **Estado do banco quando relevante** (T6, T7): query usada e linhas afetadas.
7. **Reprodutibilidade:** quantas vezes em N tentativas (especialmente para T10, anexar 10 runs).
8. **Severidade sugerida:**
   - Crítica: T2, T4, T8 com efeito colateral, T10 com 2x 200, 500 em T3.
   - Alta: T5, T7, ausência de `no-store`, falta de rate-limit.
   - Média: T9 (limitação de teste, não bug).
9. **Sugestão de teste automatizado** a adicionar na suíte `[Trait("CA","011")]` cobrindo o cenário — toda regressão de auth precisa virar teste de integração com `WebApplicationFactory` antes do fix mergear.
