# GET /api/v1/usuarios/{id} — Obter usuário por id

## Resumo

- **Método:** `GET`
- **Path:** `/api/v1/usuarios/{id}`
- **Propósito:** retornar os dados públicos de um usuário do sistema a partir do seu identificador (`Guid`).
- **Autenticação observada (atual):** **anônima**. A rota NÃO chama `RequireAuthorization()` em `backend/src/CarWash.Api/Endpoints/Usuarios/UsuariosEndpoints.cs:37` — qualquer cliente sem `Authorization` consegue ler dados de usuário interno. Esse é o cenário registrado como **Tbug-Auth** (bug crítico LGPD/RNF segurança).
- **Autenticação esperada:** `Authorization: Bearer <token>` válido, com perfil autorizado. Sem token deveria responder `401 Unauthorized`; sem permissão, `403 Forbidden`.
- **Produces:**
  - `200 OK` — `UsuarioResponse { Id, Nome, Email, Perfil, Ativo, CriadoEm, AtualizadoEm }`.
  - `400 Bad Request` — `id` mal formado no route binding.
  - `404 Not Found` — usuário inexistente (`ProblemDetails`, RFC 7807).
- **Handler:** `ObterPorIdAsync` (`backend/src/CarWash.Api/Endpoints/Usuarios/UsuariosEndpoints.cs:37`). Lança `NotFoundException` quando o registro não existe, traduzida para `404 ProblemDetails` pelo middleware global.

## Pré-requisitos

1. Backend em execução em `http://localhost:8080` (dev local).
2. Banco PostgreSQL com a migration aplicada (`dotnet ef database update` ou Testcontainers de QA).
3. Criar um usuário válido via `POST /api/v1/usuarios` e salvar o `id` em variável de shell para reuso:

```bash
USUARIO_ID=$(curl -s -X POST http://localhost:8080/api/v1/usuarios \
  -H 'Content-Type: application/json' \
  -d '{
    "nome": "QA Tester",
    "email": "qa.tester+get-by-id@carwash.local",
    "senha": "Senha@Forte123",
    "perfil": "Operador"
  }' | jq -r '.id')

echo "USUARIO_ID=$USUARIO_ID"
```

4. (Opcional, quando a autenticação for corrigida) Obter token Bearer válido via `POST /api/v1/auth/login` e exportar como `TOKEN`:

```bash
TOKEN=$(curl -s -X POST http://localhost:8080/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@carwash.local","senha":"Admin@123"}' | jq -r '.accessToken')
```

5. Ter `jq` instalado para inspeção de respostas JSON.

## Tabela resumo dos casos

| # | Caso | Entrada-chave | Esperado | Tipo |
|---|------|---------------|----------|------|
| Tbug-Auth | GET sem `Authorization` | nenhum header de auth | **Esperado 401**; atualmente retorna **200** com PII | Bug crítico LGPD |
| T1 | Golden path | `id` válido + (Bearer válido) | `200 OK` + `UsuarioResponse` completo sem campos sensíveis | Funcional |
| T2 | Guid bem formado inexistente | `Guid.NewGuid()` aleatório | `404 Not Found` + `ProblemDetails` | Funcional |
| T3 | Path não-Guid | `abc` | `400 Bad Request` (route binding) | Negativo |
| T4 | `Guid.Empty` | `00000000-0000-0000-0000-000000000000` | `404 Not Found` (não 500) | Boundary |
| T5 | Guid em maiúsculas | `XXXXXXXX-XXXX-...` upper-case | `200 OK` (case-insensitive) | Robustez |
| T6 | `Accept: application/xml` | header de negociação | `200 OK` com body JSON (ASP.NET 8 padrão) | Robustez |
| T7 | Query params irrelevantes | `?foo=bar&drop=table` | `200 OK` (ignora) | Robustez |
| T8 | Tentativa SQL injection no path | `' OR 1=1` | `400 Bad Request` (Guid binding bloqueia) | Segurança |
| T9 | Verificação de campos sensíveis | `id` válido | response NÃO inclui `senhaHash`, `salt`, `tentativasInvalidas`, `bloqueadoAte` | Segurança/LGPD |
| T10 | Performance | `id` válido | tempo de resposta `< 300ms` (RNF005) | Não-funcional |

---

## Tbug-Auth — GET sem Authorization (BUG CRÍTICO LGPD)

Verifica que a rota está exposta anonimamente, o que viola RNF de segurança e LGPD (dados pessoais de usuário interno).

```bash
curl -i "http://localhost:8080/api/v1/usuarios/$USUARIO_ID"
```

**Resposta observada hoje (BUG):**

```
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8

{
  "id": "...",
  "nome": "QA Tester",
  "email": "qa.tester+get-by-id@carwash.local",
  "perfil": "Operador",
  "ativo": true,
  "criadoEm": "2026-05-17T12:00:00Z",
  "atualizadoEm": "2026-05-17T12:00:00Z"
}
```

**Resposta esperada após correção:**

```
HTTP/1.1 401 Unauthorized
```

**Logs Serilog esperados (após correção):**

```
[INF] Authorization failed for request GET /api/v1/usuarios/{id} (no token)
```

**Logs Serilog observados hoje:**

```
[INF] HTTP GET /api/v1/usuarios/{id} responded 200 in XX ms
[INF] Usuário obtido por id {UsuarioId}
```

**Sinais de bug:**

- Status `200` sem qualquer credencial.
- Ausência de qualquer log de "authorization" no Serilog.
- Rota não aparece com cadeado no Swagger.

**Mitigação no código:** adicionar `.RequireAuthorization()` no `MapGet("/{id}", ...)` e cobrir com teste de integração `[Trait("CA","011")]` para impedir regressão.

---

## T1 — Golden path

```bash
curl -i "http://localhost:8080/api/v1/usuarios/$USUARIO_ID" \
  -H "Authorization: Bearer $TOKEN"
```

**Resposta esperada:**

```
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
```

```json
{
  "id": "b1f9c1d3-2e88-4f0b-9b3a-2c5b6d8e9f10",
  "nome": "QA Tester",
  "email": "qa.tester+get-by-id@carwash.local",
  "perfil": "Operador",
  "ativo": true,
  "criadoEm": "2026-05-17T12:00:00Z",
  "atualizadoEm": "2026-05-17T12:00:00Z"
}
```

**Logs Serilog esperados:**

```
[INF] Usuário obtido por id {UsuarioId}
[INF] HTTP GET /api/v1/usuarios/{id} responded 200 in XX ms
```

**Sinais de bug:**

- Body vazio com `200 OK`.
- Tipos inconsistentes (`ativo` como string, datas em fuso local).
- Campo `perfil` retornando inteiro em vez do nome do enum.

---

## T2 — Guid bem formado mas inexistente

```bash
NAO_EXISTE=$(uuidgen)
curl -i "http://localhost:8080/api/v1/usuarios/$NAO_EXISTE" \
  -H "Authorization: Bearer $TOKEN"
```

**Resposta esperada (RFC 7807):**

```
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
```

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Recurso não encontrado",
  "status": 404,
  "detail": "Usuário não encontrado.",
  "traceId": "00-..."
}
```

**Logs Serilog esperados:**

```
[WRN] NotFoundException: Usuário {UsuarioId} não encontrado
[INF] HTTP GET /api/v1/usuarios/{id} responded 404 in XX ms
```

**Sinais de bug:**

- `200 OK` com body `null` em vez de `404`.
- `500 Internal Server Error` (NullReferenceException no handler).
- ProblemDetails sem `traceId` ou sem `type`.

---

## T3 — Path não-Guid

```bash
curl -i "http://localhost:8080/api/v1/usuarios/abc" \
  -H "Authorization: Bearer $TOKEN"
```

**Resposta esperada:**

```
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
```

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "id": ["The value 'abc' is not valid."]
  }
}
```

**Logs Serilog esperados:**

```
[WRN] Failed to bind parameter "Guid id" from "abc"
[INF] HTTP GET /api/v1/usuarios/abc responded 400 in XX ms
```

**Sinais de bug:**

- `404 Not Found` em vez de `400`.
- `500` por exceção não tratada de parsing.
- Mensagem de erro vazia ou sem indicar o campo `id`.

---

## T4 — Guid.Empty

```bash
curl -i "http://localhost:8080/api/v1/usuarios/00000000-0000-0000-0000-000000000000" \
  -H "Authorization: Bearer $TOKEN"
```

**Resposta esperada:**

```
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
```

```json
{
  "title": "Recurso não encontrado",
  "status": 404,
  "detail": "Usuário não encontrado."
}
```

**Logs Serilog esperados:**

```
[WRN] NotFoundException: Usuário 00000000-0000-0000-0000-000000000000 não encontrado
```

**Sinais de bug:**

- `500 Internal Server Error` (handler não trata `Guid.Empty`).
- `200 OK` retornando o primeiro usuário do banco (query mal formada).
- `400` em vez de `404` (Guid.Empty é Guid sintaticamente válido).

---

## T5 — Guid em letras maiúsculas

```bash
ID_UPPER=$(echo "$USUARIO_ID" | tr '[:lower:]' '[:upper:]')
curl -i "http://localhost:8080/api/v1/usuarios/$ID_UPPER" \
  -H "Authorization: Bearer $TOKEN"
```

**Resposta esperada:**

```
HTTP/1.1 200 OK
```

Mesmo body do T1 (Guid é case-insensitive na deserialização do .NET).

**Logs Serilog esperados:**

```
[INF] Usuário obtido por id {UsuarioId}
```

**Sinais de bug:**

- `404 Not Found` (comparação string-sensitive em vez de Guid).
- `400 Bad Request` rejeitando o formato maiúsculo.

---

## T6 — Accept: application/xml

```bash
curl -i "http://localhost:8080/api/v1/usuarios/$USUARIO_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Accept: application/xml"
```

**Resposta esperada (ASP.NET 8 com pipeline padrão):**

```
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
```

Body JSON idêntico ao T1. Documentar: o backend não tem `AddXmlSerializerFormatters`, então a negociação cai no JSON padrão; isso é o comportamento atual aceito.

**Logs Serilog esperados:**

```
[INF] HTTP GET /api/v1/usuarios/{id} responded 200 in XX ms
```

**Sinais de bug:**

- `406 Not Acceptable` (configuração estrita de content negotiation indesejada).
- `500` ao tentar serializar em XML sem formatter.

---

## T7 — Query params irrelevantes

```bash
curl -i "http://localhost:8080/api/v1/usuarios/$USUARIO_ID?foo=bar&drop=table" \
  -H "Authorization: Bearer $TOKEN"
```

**Resposta esperada:**

```
HTTP/1.1 200 OK
```

Body idêntico ao T1; query string é simplesmente ignorada pelo binding.

**Logs Serilog esperados:**

```
[INF] Usuário obtido por id {UsuarioId}
```

**Sinais de bug:**

- Reflexão dos query params na resposta (vazamento de input).
- Erro de binding `400` indevido.

---

## T8 — Tentativa de SQL injection no path

```bash
curl -i --path-as-is "http://localhost:8080/api/v1/usuarios/' OR 1=1" \
  -H "Authorization: Bearer $TOKEN"
```

**Resposta esperada:**

```
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
```

ProblemDetails idêntico em estrutura ao T3 (parsing do Guid falha antes de chegar à camada de dados).

**Logs Serilog esperados:**

```
[WRN] Failed to bind parameter "Guid id" from "' OR 1=1"
```

**Sinais de bug:**

- `200 OK` retornando lista de usuários (injeção bem-sucedida — crítico).
- `500` com stack trace exposto no body.
- Log indicando que a string chegou até o repositório/EF.

---

## T9 — Resposta NÃO inclui campos sensíveis

Reaproveita a chamada do T1. Validar via `jq` que campos sensíveis estão ausentes.

```bash
curl -s "http://localhost:8080/api/v1/usuarios/$USUARIO_ID" \
  -H "Authorization: Bearer $TOKEN" \
| jq 'keys'
```

**Resposta esperada:**

```json
[
  "ativo",
  "atualizadoEm",
  "criadoEm",
  "email",
  "id",
  "nome",
  "perfil"
]
```

Conjunto deve ser exatamente este. Em particular, NÃO deve conter:

- `senhaHash`
- `senha`
- `salt`
- `tentativasInvalidas`
- `bloqueadoAte`
- `ultimoLoginEm`
- `refreshTokenHash`

**Logs Serilog esperados:** nenhuma menção a hash ou senha no log estruturado.

**Sinais de bug:**

- Qualquer um dos campos acima aparecendo no JSON.
- DTO de domínio sendo retornado direto em vez de `UsuarioResponse`.
- Log de Serilog imprimindo o objeto completo com `Destructure`, vazando hash no log.

---

## T10 — Performance (RNF005)

Medir tempo de resposta em 10 chamadas; mediana deve ser inferior a 300 ms em ambiente dev local.

```bash
for i in {1..10}; do
  curl -s -o /dev/null -w "%{time_total}\n" \
    "http://localhost:8080/api/v1/usuarios/$USUARIO_ID" \
    -H "Authorization: Bearer $TOKEN"
done | sort -n | awk 'NR==5{print "mediana:",$1"s"}'
```

**Resposta esperada:** mediana `< 0.300s`.

**Logs Serilog esperados:**

```
[INF] HTTP GET /api/v1/usuarios/{id} responded 200 in <300 ms
```

**Sinais de bug:**

- Tempo > 1s consistentemente (query sem índice em `Id`).
- N+1 query no log do EF (`Executed DbCommand` repetido).
- Tempo crescente entre chamadas (memory leak / connection pool exausto).

---

## Bugs e crashes a observar

- **Tbug-Auth (crítico, LGPD):** rota anônima expondo PII de usuário interno. Falta `.RequireAuthorization()` em `UsuariosEndpoints.cs:37`. Abrir issue como `bug-Auth` com severidade alta e bloqueio de release.
- `500 Internal Server Error` quando `id == Guid.Empty` — handler precisa tratar como ausência (404), não como exceção não-mapeada.
- Vazamento de `senhaHash`, `salt`, `tentativasInvalidas`, `bloqueadoAte` no body — indica que o endpoint está retornando a entidade de domínio em vez de `UsuarioResponse`.
- `404` mascarado como `200 OK` com `body: null` — quebra contrato e confunde o cliente.
- ProblemDetails inconsistente entre `400` e `404` — ambos devem seguir RFC 7807 com `type`, `title`, `status`, `detail`, `traceId`.
- Cabeçalho `Cache-Control` ausente ou permissivo: como o body contém PII, deve vir `Cache-Control: no-store` ou no mínimo `private, max-age=0`. Verificar com `curl -i` em todos os casos.
- Stack trace exposto no body em ambiente não-Development (deve ficar atrás de `app.UseDeveloperExceptionPage()` apenas em dev).
- Log do Serilog imprimindo o objeto de usuário com `@Usuario` destruturado — risco de hash de senha cair em log estruturado.

## Como reportar para o dev

Ao registrar bug, encaminhar ao agente `dev-dotnet-carwash` (par com `arquiteto-carwash` se for de design) com:

1. **Título objetivo:** ex.: `bug-Auth: GET /api/v1/usuarios/{id} acessível anonimamente (vazamento de PII)`.
2. **Severidade e impacto:** crítico (LGPD) / alto (consistência) / médio (robustez) / baixo (cosmético). Indicar se bloqueia release do MVP (CA001/CA011).
3. **Endpoint e arquivo:** `GET /api/v1/usuarios/{id}` — `backend/src/CarWash.Api/Endpoints/Usuarios/UsuariosEndpoints.cs:37`.
4. **Comando `curl` reproduzível** copiado da seção do caso correspondente.
5. **Resposta observada** (status + headers relevantes + body) em bloco ```http e ```json.
6. **Resposta esperada** com a mesma formatação.
7. **Trecho do log Serilog** capturado durante a execução (`docker logs` ou console do `dotnet run`).
8. **Caso de teste sugerido** com `[Trait("CA","011")]` em xUnit + WebApplicationFactory, garantindo regressão futura. Exemplo para Tbug-Auth:

```csharp
[Fact]
[Trait("CA", "011")]
public async Task GetUsuarioPorId_SemAuthorization_Retorna401()
{
    var client = factory.CreateClient();
    var resp = await client.GetAsync($"/api/v1/usuarios/{Guid.NewGuid()}");
    resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

9. **Referências cruzadas:** RF (cadastro/consulta de usuário), RNF de segurança/LGPD, CA001/CA011, e link para esta documentação (`QA/usuarios/GET_usuario_por_id.md`).
10. **Critério de aceite do fix:** todos os casos T1–T10 + Tbug-Auth verdes no pipeline; cobertura mantida; sem `[Skip]`.
