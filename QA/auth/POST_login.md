# POST /api/v1/auth/login

Endpoint anonimo de autenticacao por credenciais (email + senha). Emite `accessToken` JWT no corpo e `carwash_refresh_token` em cookie `HttpOnly`. Protegido por rate limiting `auth-login` (10 requisicoes por minuto por IP). Aplica lockout temporario apos 3 tentativas invalidas consecutivas para o mesmo usuario (15 minutos).

- Metodo: `POST`
- Path: `/api/v1/auth/login`
- Autenticacao: anonima
- Content-Type esperado: `application/json`
- Produces: `200 OK`, `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `429 Too Many Requests`, `500 Internal Server Error`
- Proposito: validar credenciais, emitir par `accessToken` + `refreshToken`, registrar sessao e devolver dados publicos do usuario autenticado.

## Pre-requisitos

- Backend em execucao em `http://localhost:8080` (perfil Development).
- Banco PostgreSQL acessivel, com migrations aplicadas e seed executado.
- Usuario `admin@carwash.local` provisionado via migration `20260513114525_InitialSchema` e senha definida na variavel `CARWASH_SEED_ADMIN_PASSWORD` (lida do `.env`).
- Cookie jar para persistir o refresh token: `/tmp/carwash-cookies.txt` (limpar com `: > /tmp/carwash-cookies.txt` antes de cenarios sensiveis).
- Para os cenarios T9/T11 e altamente recomendado criar um usuario dedicado de teste (`qa-login@carwash.local`) para nao bloquear/desativar o admin do seed.
- `jq` instalado para inspecionar respostas JSON.
- Acesso aos logs do backend (stdout do `dotnet run` ou arquivo Serilog) para validar mensagens mascaradas.

## Tabela resumo

| ID  | Descricao | Status esperado |
|-----|-----------|-----------------|
| T1  | Golden path: admin valido com senha correta | 200 |
| T2  | Email inexistente | 401 |
| T3  | Senha incorreta para usuario existente | 401 |
| T4  | Email malformado (sem `@`) | 401 (anti-enumeracao) |
| T5  | Senha vazia ou nula | 400 |
| T6  | Body vazio `{}` | 400 |
| T7  | JSON malformado | 400 |
| T8  | Content-Type ausente ou incorreto | 415 ou 400 |
| T9  | Lockout apos 3 falhas consecutivas | 403 |
| T10 | Rate limit por IP (11+ tentativas/min) | 429 |
| T11 | Usuario inativo | 403 |
| T12 | Login simultaneo / race | 200 + 200 (descritivo) |
| T13 | Rotacao do refresh token entre dois logins | 200 + 200 |

---

## T1 - Golden path

Pre-condicao: backend ativo, seed aplicado, `CARWASH_SEED_ADMIN_PASSWORD` exportada no shell. Cookie jar limpo.

```bash
: > /tmp/carwash-cookies.txt
curl -i \
  -c /tmp/carwash-cookies.txt \
  --fail-with-body \
  -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"admin@carwash.local\",\"senha\":\"${CARWASH_SEED_ADMIN_PASSWORD}\"}"
```

```json
{
  "email": "admin@carwash.local",
  "senha": "<valor de CARWASH_SEED_ADMIN_PASSWORD>"
}
```

Resposta esperada:

- Status: `200 OK`.
- Headers chave:
  - `Cache-Control: no-store`.
  - `Set-Cookie: carwash_refresh_token=<valor>; path=/api/v1/auth; samesite=strict; httponly` (sem `Secure` no perfil Development).
  - `Content-Type: application/json; charset=utf-8`.
- Body (`LoginResponse`):

```json
{
  "accessToken": "<JWT>",
  "expiresAt": "2026-05-17T12:34:56Z",
  "usuario": {
    "id": "<guid>",
    "nome": "Administrador",
    "email": "admin@carwash.local",
    "perfil": "Admin"
  }
}
```

- O `accessToken` deve ser decodificavel em https://jwt.io com claims `sub` (id do usuario), `perfil` e `exp` coerente com `expiresAt`.
- O campo `refreshToken` NAO deve aparecer no body.

Verificacao nos logs:

```
Login bem-sucedido. UsuarioId={...}, Email=ad***@carwash.local, SessaoId={...}
```

Sinais de bug:

- Body retornando `refreshToken` em texto claro.
- Cookie sem `HttpOnly` ou sem `SameSite=Strict`.
- Header `Cache-Control: no-store` ausente.
- `expiresAt` fora do formato ISO 8601 UTC.
- JWT vazio, sem claim `perfil` ou `exp` no passado.

---

## T2 - Email inexistente

Pre-condicao: garantir que `nao-existe@carwash.local` nao esta cadastrado.

```bash
curl -i \
  -c /tmp/carwash-cookies.txt \
  -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"nao-existe@carwash.local","senha":"qualquer-senha-123"}'
```

```json
{
  "email": "nao-existe@carwash.local",
  "senha": "qualquer-senha-123"
}
```

Resposta esperada:

- Status: `401 Unauthorized`.
- Body em `application/problem+json` com mensagem generica do tipo `"Credenciais invalidas."` (sem revelar se foi email ou senha).
- Nenhum `Set-Cookie` de refresh token.

Verificacao nos logs:

```
Falha de login (credencial invalida). Email=na***@carwash.local
```

Sinais de bug:

- Mensagem diferenciando "usuario nao existe" vs "senha incorreta".
- Status `404`.
- Tempo de resposta significativamente menor que T3 (oraculo de enumeracao - o `DummyPasswordHash` no handler existe justamente para igualar o custo).

---

## T3 - Senha incorreta para usuario existente

Pre-condicao: `admin@carwash.local` cadastrado.

```bash
curl -i \
  -c /tmp/carwash-cookies.txt \
  -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@carwash.local","senha":"senha-totalmente-errada"}'
```

```json
{
  "email": "admin@carwash.local",
  "senha": "senha-totalmente-errada"
}
```

Resposta esperada:

- Status: `401 Unauthorized`.
- Body em `application/problem+json` com a MESMA mensagem generica de T2.
- Nenhum `Set-Cookie` de refresh token.

Verificacao nos logs:

```
Falha de login (credencial invalida). Email=ad***@carwash.local
```

Sinais de bug:

- Mensagem diferente da retornada em T2.
- Diferenca de timing relevante entre T2 e T3 (verificar com `curl -w '%{time_total}\n' -o /dev/null -s`).
- Incremento de tentativas invalidas nao registrado (validar via T9).

---

## T4 - Email malformado (sem `@`)

Pre-condicao: backend rodando.

```bash
curl -i \
  -c /tmp/carwash-cookies.txt \
  -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin-sem-arroba","senha":"qualquer-senha-123"}'
```

```json
{
  "email": "admin-sem-arroba",
  "senha": "qualquer-senha-123"
}
```

Resposta esperada:

- Status: `401 Unauthorized` (decisao anti-enumeracao: `LoginValidator` nao valida formato de email).
- Body em `application/problem+json` com a mesma mensagem generica de credenciais invalidas.

Verificacao nos logs:

```
Falha de login (credencial invalida). Email=ad***
```

Sinais de bug:

- Status `400` retornando `ValidationException` para o campo `email` (vazaria a politica anti-enumeracao).
- Body apontando erro especifico de "Email invalido".

---

## T5 - Senha vazia ou nula

Pre-condicao: backend rodando.

```bash
curl -i \
  -c /tmp/carwash-cookies.txt \
  -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@carwash.local","senha":""}'
```

```json
{
  "email": "admin@carwash.local",
  "senha": ""
}
```

Tambem testar com `"senha": null`:

```bash
curl -i \
  -c /tmp/carwash-cookies.txt \
  -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@carwash.local","senha":null}'
```

Resposta esperada:

- Status: `400 Bad Request`.
- Body `application/problem+json` indicando que o campo `senha` e obrigatorio (validator `LoginValidator` exige senha nao vazia).
- Sem `Set-Cookie` de refresh token.

Verificacao nos logs: registro de `ValidationException` ou warning equivalente (sem dados sensiveis).

Sinais de bug:

- Status `401` (mascara validacao com credenciais invalidas, atrapalha cliente).
- Status `500` por NullReferenceException.
- Mensagem expondo regra interna ou stack trace.

---

## T6 - Body vazio `{}`

Pre-condicao: backend rodando.

```bash
curl -i \
  -c /tmp/carwash-cookies.txt \
  -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{}'
```

```json
{}
```

Resposta esperada:

- Status: `400 Bad Request`.
- Body `application/problem+json` com erros de validacao para `email` e `senha` obrigatorios.

Verificacao nos logs: warning de validacao, sem PII no payload logado.

Sinais de bug:

- Status `500`.
- Body com apenas um dos campos reportados quando ambos estao ausentes.

---

## T7 - JSON malformado

Pre-condicao: backend rodando.

```bash
curl -i \
  -c /tmp/carwash-cookies.txt \
  -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  --data-binary '{"email":"admin@carwash.local","senha":'
```

```json
{"email":"admin@carwash.local","senha":
```

Resposta esperada:

- Status: `400 Bad Request` (BadHttpRequestException do pipeline do ASP.NET Core ao desserializar).
- Body `application/problem+json` com `title` informando JSON invalido.

Verificacao nos logs: registro de falha de desserializacao.

Sinais de bug:

- Status `500` com stack trace.
- Conexao encerrada sem resposta (`curl: (52) Empty reply from server`).

---

## T8 - Content-Type ausente ou incorreto

Pre-condicao: backend rodando.

Sem `Content-Type`:

```bash
curl -i \
  -c /tmp/carwash-cookies.txt \
  -X POST http://localhost:8080/api/v1/auth/login \
  -d '{"email":"admin@carwash.local","senha":"qualquer-senha-123"}'
```

Com `Content-Type: text/plain`:

```bash
curl -i \
  -c /tmp/carwash-cookies.txt \
  -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: text/plain" \
  -d '{"email":"admin@carwash.local","senha":"qualquer-senha-123"}'
```

```json
{
  "email": "admin@carwash.local",
  "senha": "qualquer-senha-123"
}
```

Resposta esperada:

- Status: `415 Unsupported Media Type` (padrao Minimal API) ou `400 Bad Request`.
- Body `application/problem+json` indicando media type nao suportado.

Verificacao nos logs: trace de pipeline rejeitando antes do handler.

Sinais de bug:

- Status `200` (handler aceitando payload sem negociar tipo).
- Status `500` por excecao nao tratada.

---

## T9 - Lockout apos 3 falhas consecutivas

Pre-condicao: criar e usar um usuario de teste dedicado (`qa-login@carwash.local`) para nao bloquear o admin. Limpar tentativas previas, se houver.

```bash
EMAIL="qa-login@carwash.local"
for i in 1 2 3 4; do
  echo "== tentativa $i =="
  curl -i \
    -X POST http://localhost:8080/api/v1/auth/login \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"${EMAIL}\",\"senha\":\"errada-${i}\"}"
  echo
done
```

```json
{
  "email": "qa-login@carwash.local",
  "senha": "errada-N"
}
```

Resposta esperada:

- Tentativas 1, 2 e 3: `401 Unauthorized` (credenciais invalidas, mensagem generica).
- Tentativa 4: `403 Forbidden` com body `application/problem+json` contendo extensions:

```json
{
  "type": "...",
  "title": "Usuario bloqueado.",
  "status": 403,
  "bloqueadoAte": "2026-05-17T13:00:00Z",
  "retryAfterSeconds": 900
}
```

- Header `Retry-After: 900` (segundos restantes ate o desbloqueio, 15 minutos no maximo).
- Mesmo enviando a senha correta dentro da janela de 15 minutos, a resposta segue `403 UsuarioBloqueadoException`.

Verificacao nos logs:

```
Conta bloqueada por excesso de tentativas invalidas. UsuarioId={...}, Email=qa***@carwash.local
Falha de login (usuario bloqueado). Email=qa***@carwash.local, BloqueadoAte=2026-05-17T13:00:00Z
```

Sinais de bug:

- Lockout disparando antes da 4a tentativa ou nunca disparando.
- `bloqueadoAte` em formato diferente de ISO 8601 UTC.
- Header `Retry-After` ausente ou divergente de `retryAfterSeconds`.
- Reset do contador a cada nova falha (em vez de janela continua).

---

## T10 - Rate limit por IP

Pre-condicao: cliente unico, mesmo IP de origem. Usar um usuario que NAO esta proximo do lockout (ou um email inexistente) para garantir que o gatilho seja o rate limit, nao o lockout.

```bash
for i in $(seq 1 15); do
  echo "== req $i =="
  curl -s -o /dev/null -w "%{http_code}\n" \
    -X POST http://localhost:8080/api/v1/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"rate-limit-probe@carwash.local","senha":"x"}'
done
```

```json
{
  "email": "rate-limit-probe@carwash.local",
  "senha": "x"
}
```

Resposta esperada:

- As 10 primeiras requisicoes retornam `401` (ou `400` para senha vazia, conforme caso usado).
- A partir da 11a no mesmo minuto: `429 Too Many Requests`.
- Body em `application/problem+json`:

```json
{
  "title": "Muitas tentativas. Aguarde um instante e tente novamente.",
  "status": 429
}
```

- Header `Retry-After` presente, em segundos.

Verificacao nos logs: warning de `OnRejected` do middleware de rate limit (sem PII).

Sinais de bug:

- Permitir mais de 10 requests/min/IP.
- Header `Retry-After` ausente.
- Body diferente do contrato (texto cru, HTML, stack trace).
- Bloqueio se estendendo alem da janela de 1 minuto.

---

## T11 - Usuario inativo

Pre-condicao: marcar o usuario de teste como inativo direto no banco. Reverter ao final.

```bash
# desativar
psql "$DATABASE_URL" -c "UPDATE usuarios SET ativo = false WHERE email = 'qa-login@carwash.local';"

# tentar login com senha CORRETA
curl -i \
  -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"qa-login@carwash.local","senha":"<senha-correta-do-usuario-de-teste>"}'

# reverter
psql "$DATABASE_URL" -c "UPDATE usuarios SET ativo = true WHERE email = 'qa-login@carwash.local';"
```

```json
{
  "email": "qa-login@carwash.local",
  "senha": "<senha-correta-do-usuario-de-teste>"
}
```

Resposta esperada:

- Status: `403 Forbidden` (`UsuarioInativoException`).
- Body `application/problem+json` com mensagem indicando conta inativa, sem revelar detalhes do banco.
- Nenhum `Set-Cookie` de refresh token.

Verificacao nos logs:

```
Falha de login (usuario inativo). UsuarioId={...}, Email=qa***@carwash.local
```

Sinais de bug:

- Status `401` (confunde com credenciais invalidas).
- Token emitido apesar de `ativo = false`.
- Stack trace exposto no body.

---

## T12 - Login simultaneo / race

Pre-condicao: duas execucoes paralelas com a MESMA credencial valida (preferivelmente em um usuario de teste, nao no admin de producao).

```bash
EMAIL="qa-login@carwash.local"
SENHA="<senha-correta-do-usuario-de-teste>"
seq 1 2 | xargs -I{} -P2 bash -c "
  curl -i -s -c /tmp/carwash-cookies-{}.txt \
    -X POST http://localhost:8080/api/v1/auth/login \
    -H 'Content-Type: application/json' \
    -d '{\"email\":\"$EMAIL\",\"senha\":\"$SENHA\"}' \
    -o /tmp/login-{}.out
"
grep -E 'HTTP/|Set-Cookie' /tmp/login-1.out
grep -E 'HTTP/|Set-Cookie' /tmp/login-2.out
```

```json
{
  "email": "qa-login@carwash.local",
  "senha": "<senha-correta-do-usuario-de-teste>"
}
```

Resposta esperada:

- Ambas as requisicoes podem retornar `200 OK` (duas sessoes independentes sao validas).
- Cada resposta tras um `carwash_refresh_token` distinto.
- Devem existir duas linhas separadas no log de `Login bem-sucedido` com `SessaoId` diferentes.

Verificacao nos logs:

```
Login bem-sucedido. UsuarioId={...}, Email=qa***@carwash.local, SessaoId={A}
Login bem-sucedido. UsuarioId={...}, Email=qa***@carwash.local, SessaoId={B}
```

Sinais de bug:

- Uma das requisicoes retornando `409 Conflict` ou `500` por deadlock.
- Mesmo refresh token sendo emitido nas duas respostas (colisao).
- Apenas uma sessao sendo persistida quando duas eram esperadas (a menos que exista regra explicita de sessao unica).

---

## T13 - Rotacao do refresh token entre dois logins sequenciais

Pre-condicao: usuario valido.

```bash
EMAIL="admin@carwash.local"
SENHA="${CARWASH_SEED_ADMIN_PASSWORD}"

# primeiro login
: > /tmp/carwash-cookies-1.txt
curl -i -s -c /tmp/carwash-cookies-1.txt \
  -X POST http://localhost:8080/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"$EMAIL\",\"senha\":\"$SENHA\"}" \
  -o /tmp/login-A.out

# segundo login
: > /tmp/carwash-cookies-2.txt
curl -i -s -c /tmp/carwash-cookies-2.txt \
  -X POST http://localhost:8080/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"$EMAIL\",\"senha\":\"$SENHA\"}" \
  -o /tmp/login-B.out

grep carwash_refresh_token /tmp/carwash-cookies-1.txt
grep carwash_refresh_token /tmp/carwash-cookies-2.txt
```

```json
{
  "email": "admin@carwash.local",
  "senha": "<valor de CARWASH_SEED_ADMIN_PASSWORD>"
}
```

Resposta esperada:

- Status `200 OK` em ambos.
- Valores de `carwash_refresh_token` nos dois cookie jars sao distintos.
- O `accessToken` emitido tambem deve ser distinto entre os dois logins (claims `jti` diferentes ou `iat`/`exp` diferentes).
- Se a regra de rotacao estiver ativa, o refresh emitido em A deve estar revogado apos B (validar consultando a tabela de refresh tokens / sessoes no banco).

Verificacao nos logs:

```
Login bem-sucedido. UsuarioId={...}, Email=ad***@carwash.local, SessaoId={A}
Login bem-sucedido. UsuarioId={...}, Email=ad***@carwash.local, SessaoId={B}
```

Sinais de bug:

- Mesmo valor de cookie em A e B (refresh estatico).
- A sessao anterior continuar valida em endpoints `/refresh` se a regra de revogacao estiver definida.
- Ausencia de registro da nova sessao no banco.

---

## Bugs e crashes a observar

- `500 Internal Server Error` mascarando o que deveria ser `400`, `401` ou `403`. Sempre cruzar com os logs.
- Cookie `carwash_refresh_token` sem flag `HttpOnly` (deve estar presente inclusive em Development).
- Flag `Secure` presente em Development (so deve aparecer fora de Development/Testing).
- Vazamento de stack trace ou nomes internos de classes no body de erro.
- Divergencia de mensagem ou de timing entre T2 (usuario inexistente) e T3 (senha incorreta) - oraculo de enumeracao. O `DummyPasswordHash` no `LoginHandler` existe para igualar o custo computacional; se o desvio for perceptivel, abrir bug.
- Header `Cache-Control: no-store` ausente em qualquer 200 (`LoginResponse` carrega `accessToken` e nao pode ser cacheado).
- Campo `refreshToken` aparecendo dentro do body do `LoginResponse` (deve estar exclusivamente no cookie).
- `accessToken` vazio, sem `.` (nao e JWT), sem claim `sub`, sem claim `perfil`, ou com `exp` ja expirado no momento da emissao. Decodificar em https://jwt.io.
- Lockout (T9) acionando antes da 4a tentativa ou ignorando o limite.
- Rate limit (T10) permitindo mais de 10 requests/min/IP ou sem `Retry-After`.
- Logs com email em texto claro (deveria estar mascarado, ex.: `ad***@carwash.local`).
- Logs com senha em texto claro - bug critico, abrir incidente.

## Como reportar para o dev

Modelo a anexar no ticket:

```
Caso: T<numero> - <descricao curta>
Ambiente: localhost:8080 / branch <branch> / commit <sha>
Data/hora (UTC): <ISO 8601>
correlationId / traceId: <valor capturado do header de resposta ou do log>

Request:
  Method/URL: POST /api/v1/auth/login
  Headers relevantes: Content-Type, Cookie, X-Forwarded-For
  Body:
    {
      "email": "...",
      "senha": "<MASCARAR>"
    }

Response:
  Status: <codigo>
  Headers relevantes: Set-Cookie, Cache-Control, Retry-After, Content-Type
  Body:
    <copia integral, com senhas/tokens redacted se necessario>

Logs do backend (Serilog):
  <colar linha exata, incluindo SessaoId/UsuarioId quando presentes>

Comportamento esperado: <descricao curta + referencia ao caso T do checklist>
Comportamento observado: <o que aconteceu de fato>
Impacto: <baixo | medio | alto | critico>
Reproducao: <passos minimos ou comando curl que reproduz>
```

Lembrar de anexar o JWT (decodificado em jwt.io) e o conteudo bruto do cookie jar (`/tmp/carwash-cookies.txt`) quando o bug envolver autenticacao ou rotacao de sessao. Para incidentes envolvendo dados sensiveis nos logs, marcar o ticket como confidencial e notificar o responsavel de seguranca antes de publicar.
