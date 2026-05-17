# PUT /api/v1/clientes/{id} — Atualizar cliente (replace)

## Resumo

- **Método:** `PUT`
- **Path:** `/api/v1/clientes/{id:guid}`
- **Propósito:** atualizar dados cadastrais de um cliente existente. Operação de **replace completo** (não é PATCH parcial).
- **Autenticação:** obrigatória — header `Authorization: Bearer <token>`.
- **Produces:** `200 OK` (sucesso) | `400 Bad Request` (validação) | `401 Unauthorized` (sem token) | `404 Not Found` (cliente inexistente ou id não-Guid) | `409 Conflict` (somente se UNIQUE de banco for violado e o middleware mapear como conflict) | `500 Internal Server Error` (erro inesperado).
- **Campos imutáveis pelo DTO:** `cpf`, `cnpj` (não estão em `UpdateClienteRequest`, são silenciosamente descartados se enviados).

---

## Pré-requisitos

1. Backend rodando em `http://localhost:8080`.
2. Variável `$TOKEN` carregada com JWT válido emitido por `/api/v1/auth/login`.
3. Cliente já criado via `POST /api/v1/clientes` — guardar o `id` retornado em `$CLIENTE_ID`.
4. Para T9, ter um segundo cliente com email distinto (`$OUTRO_EMAIL`) para testar colisão.

```bash
export TOKEN="<jwt aqui>"
export CLIENTE_ID="<guid do cliente alvo>"
export OUTRO_EMAIL="outro.cliente@exemplo.com"
```

---

## Tabela resumo dos casos

| ID | Cenário | Esperado | Observação |
|----|---------|----------|------------|
| T1 | Golden path — body completo válido | 200 + `ClienteResponse` | atualizado com sucesso |
| T2 | Body parcial (falta `nome` ou `endereco`) | 400 | PUT é replace, não PATCH |
| T3 | Id inexistente (Guid válido) | 404 | `NotFoundException` |
| T4 | Id não-Guid (`abc`) | 404 | route constraint `:guid` |
| T5 | Sem header `Authorization` | 401 | `[Authorize]` no controller |
| T6 | Body vazio `{}` | 400 | lista de campos obrigatórios |
| T7 | `celular` não-numérico ou < 11 dígitos | 400 | validator |
| T8 | Body inclui `cpf`/`cnpj` | 200 (descartado) | gap UX — alerta |
| T9 | Email já usado por outro cliente | 200 (gap) ou 500/409 | service não valida unicidade |
| T10 | Caracteres unicode/especiais no nome | 200 | sem normalização exigida |
| T11 | Race condition — dois PUTs paralelos | last-write-wins | sem RowVersion/412 |
| T12 | `AtualizadoEm` muda; `CriadoEm`/`CriadoPorUsuarioId` imutáveis | 200 + invariantes | sem `AtualizadoPorUsuarioId` |
| T13 | Performance | < 500ms | medir `Total` do curl |

---

## Detalhamento dos casos

### T1 — Golden path: body completo válido

```bash
curl -i -X PUT "http://localhost:8080/api/v1/clientes/$CLIENTE_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @- <<'JSON'
{
  "nome": "Maria Silva Atualizada",
  "dataNascimento": "1985-04-12",
  "telefone": "1133224455",
  "celular": "11988887777",
  "email": "maria.atualizada@exemplo.com",
  "endereco": "Rua Nova, 456 - Apto 12 - Sao Paulo/SP"
}
JSON
```

**Resposta esperada:** `200 OK`

```json
{
  "id": "<CLIENTE_ID>",
  "nome": "Maria Silva Atualizada",
  "dataNascimento": "1985-04-12",
  "telefone": "1133224455",
  "celular": "11988887777",
  "email": "maria.atualizada@exemplo.com",
  "endereco": "Rua Nova, 456 - Apto 12 - Sao Paulo/SP",
  "documento": "<cpf ou cnpj original, inalterado>",
  "criadoEm": "<timestamp original>",
  "atualizadoEm": "<timestamp novo, > criadoEm>"
}
```

**Logs esperados:**

```
Cliente atualizado. ClienteId=<CLIENTE_ID>, UsuarioId=<usuario do token>
```

**Sinais de bug:**

- Resposta sem `atualizadoEm` ou com valor igual a `criadoEm`.
- `documento` veio alterado mesmo sem ser enviado.
- 500 em vez de 200.

---

### T2 — Body parcial (PUT exige replace completo)

```bash
curl -i -X PUT "http://localhost:8080/api/v1/clientes/$CLIENTE_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @- <<'JSON'
{
  "dataNascimento": "1985-04-12",
  "telefone": "1133224455",
  "celular": "11988887777",
  "email": "maria.atualizada@exemplo.com",
  "endereco": "Rua Nova, 456"
}
JSON
```

**Resposta esperada:** `400 Bad Request`

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "Nome": ["O campo Nome e obrigatorio."]
  }
}
```

**Sinais de bug:** 200 com `nome` vazio/null (validator aceitou parcial) ou 500 por NRE no service.

---

### T3 — Id inexistente (Guid válido, sem registro)

```bash
curl -i -X PUT "http://localhost:8080/api/v1/clientes/00000000-0000-0000-0000-000000000000" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @- <<'JSON'
{
  "nome": "Qualquer",
  "dataNascimento": "1990-01-01",
  "telefone": "1133224455",
  "celular": "11988887777",
  "email": "qualquer@exemplo.com",
  "endereco": "Rua X, 1"
}
JSON
```

**Resposta esperada:** `404 Not Found` (middleware mapeia `NotFoundException`).

```json
{
  "title": "Not Found",
  "status": 404,
  "detail": "Cliente nao encontrado."
}
```

**Sinais de bug:** `500` em vez de `404` (middleware não pegou a exception).

---

### T4 — Id não-Guid (route constraint)

```bash
curl -i -X PUT "http://localhost:8080/api/v1/clientes/abc" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{}'
```

**Resposta esperada:** `404 Not Found` — a rota nem casa por causa do constraint `:guid`. O controller não chega a executar.

**Sinais de bug:** `500` (parsing chegando no controller) ou `400` (constraint mal configurado).

---

### T5 — Sem `Authorization`

```bash
curl -i -X PUT "http://localhost:8080/api/v1/clientes/$CLIENTE_ID" \
  -H "Content-Type: application/json" \
  -d @- <<'JSON'
{
  "nome": "Teste",
  "dataNascimento": "1990-01-01",
  "telefone": "1133224455",
  "celular": "11988887777",
  "email": "teste@exemplo.com",
  "endereco": "Rua X, 1"
}
JSON
```

**Resposta esperada:** `401 Unauthorized` (vazio ou ProblemDetails padrão do ASP.NET).

**Sinais de bug:** `200` (endpoint ficou anônimo) ou `500`.

---

### T6 — Body vazio `{}`

```bash
curl -i -X PUT "http://localhost:8080/api/v1/clientes/$CLIENTE_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{}'
```

**Resposta esperada:** `400 Bad Request` com todos os campos obrigatórios listados.

```json
{
  "status": 400,
  "errors": {
    "Nome": ["O campo Nome e obrigatorio."],
    "DataNascimento": ["..."],
    "Telefone": ["..."],
    "Celular": ["..."],
    "Email": ["..."],
    "Endereco": ["..."]
  }
}
```

**Sinais de bug:** lista incompleta (validator diferente do Create) ou `500`.

---

### T7 — Celular inválido (não-numérico ou < 11 dígitos)

```bash
curl -i -X PUT "http://localhost:8080/api/v1/clientes/$CLIENTE_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @- <<'JSON'
{
  "nome": "Maria",
  "dataNascimento": "1985-04-12",
  "telefone": "1133224455",
  "celular": "abc12345",
  "email": "maria@exemplo.com",
  "endereco": "Rua Nova, 456"
}
JSON
```

**Resposta esperada:** `400 Bad Request` com erro em `Celular`.

Repetir com `"celular": "1198888"` (7 dígitos).

**Sinais de bug:** 200 aceitando lixo no campo — regra de Create não foi espelhada no Update.

---

### T8 — Body inclui `cpf` ou `cnpj` (documento é imutável)

```bash
curl -i -X PUT "http://localhost:8080/api/v1/clientes/$CLIENTE_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @- <<'JSON'
{
  "nome": "Maria",
  "dataNascimento": "1985-04-12",
  "telefone": "1133224455",
  "celular": "11988887777",
  "email": "maria@exemplo.com",
  "endereco": "Rua Nova, 456",
  "cpf": "99999999999",
  "cnpj": "99999999000199"
}
JSON
```

**Resposta esperada:** `200 OK` — `cpf` e `cnpj` são **silenciosamente descartados** porque não fazem parte de `UpdateClienteRequest`. O campo `documento` na resposta deve permanecer **igual ao original**.

**Sinal de alerta (UX bug):** o backend não devolve nenhum aviso ao cliente da API. Um operador que enviou `cpf` novo vai achar que mudou — e não mudou. Sugestão: validator rejeitar payload com campos extras (`UnknownProperties = false`) ou middleware logar warn.

**Sinais de bug:** `documento` mudou (ou seja, DTO está ligado em algum campo escondido) — investigar mapeamento.

---

### T9 — Email duplicado entre clientes

Pré-condição: outro cliente já existe com `email = $OUTRO_EMAIL`.

```bash
curl -i -X PUT "http://localhost:8080/api/v1/clientes/$CLIENTE_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @- <<JSON
{
  "nome": "Maria",
  "dataNascimento": "1985-04-12",
  "telefone": "1133224455",
  "celular": "11988887777",
  "email": "$OUTRO_EMAIL",
  "endereco": "Rua Nova, 456"
}
JSON
```

**Comportamento atual:** `200 OK` — `ClienteService.AtualizarAsync` **não verifica unicidade de email/documento**. Isso é um **gap de regra de negócio**.

**Variação:** se houver índice `UNIQUE` em `Email` no banco, o `SaveChanges` lança `DbUpdateException`. O middleware atual mapeia `Conflict` → 409, mas `DbUpdateException` provavelmente cai no genérico → **500**.

**Sinais de bug:**

- 200 silencioso (gap de regra — reportar para o dev).
- 500 com `DbUpdateException` exposta no body (vazamento de detalhe interno).
- 409 sem `detail` claro sobre qual campo conflitou.

---

### T10 — Caracteres unicode/especiais no nome

```bash
curl -i -X PUT "http://localhost:8080/api/v1/clientes/$CLIENTE_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @- <<'JSON'
{
  "nome": "Joao da Silva Acentuacao ç ã é ô — emoji nao usar — caracteres / \\ ' \" ok",
  "dataNascimento": "1985-04-12",
  "telefone": "1133224455",
  "celular": "11988887777",
  "email": "joao@exemplo.com",
  "endereco": "Rua dos Andradas, 100"
}
JSON
```

**Resposta esperada:** `200 OK` com o `nome` preservado byte-a-byte (UTF-8).

**Sinais de bug:** caracteres trocados por `?`, mojibake (`Ã§`), `400` por regex restritiva, ou `500` por encoding mal configurado.

---

### T11 — Race condition (dois PUTs paralelos)

```bash
curl -s -X PUT "http://localhost:8080/api/v1/clientes/$CLIENTE_ID" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"nome":"Versao A","dataNascimento":"1985-04-12","telefone":"1133224455","celular":"11988887777","email":"a@exemplo.com","endereco":"Rua A"}' &

curl -s -X PUT "http://localhost:8080/api/v1/clientes/$CLIENTE_ID" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"nome":"Versao B","dataNascimento":"1985-04-12","telefone":"1133224455","celular":"11988887777","email":"b@exemplo.com","endereco":"Rua B"}' &

wait
```

Depois, `GET /api/v1/clientes/$CLIENTE_ID`:

```bash
curl -s -X GET "http://localhost:8080/api/v1/clientes/$CLIENTE_ID" \
  -H "Authorization: Bearer $TOKEN" | jq
```

**Comportamento atual:** sem `RowVersion`/ETag → **last-write-wins**. O cliente final terá os dados da requisição que persistiu por último, sem nenhum `412 Precondition Failed`.

**Sinais de bug a investigar:**

- Estado misturado (ex.: `nome = "Versao A"` e `email = "b@exemplo.com"`) → indica falta de atomicidade.
- 500 por deadlock no PostgreSQL → transações concorrentes mal isoladas.
- Logs duplicados sem correlação clara.

**Gap conhecido:** sem concorrência otimista — reportar como dívida técnica.

---

### T12 — Invariantes de auditoria

Antes do PUT, capturar:

```bash
curl -s -X GET "http://localhost:8080/api/v1/clientes/$CLIENTE_ID" \
  -H "Authorization: Bearer $TOKEN" | jq '{criadoEm, atualizadoEm}'
```

Executar T1 (golden path). Depois:

```bash
curl -s -X GET "http://localhost:8080/api/v1/clientes/$CLIENTE_ID" \
  -H "Authorization: Bearer $TOKEN" | jq '{criadoEm, atualizadoEm}'
```

**Esperado:**

- `criadoEm` igual ao anterior (imutável).
- `atualizadoEm` maior que o anterior (interceptor de EF disparou).

**Gap documentado:** entidade `Cliente` **não tem** `AtualizadoPorUsuarioId`. O `usuarioId` é descartado no service (`_ = usuarioId;`). Não há rastro de **quem** alterou — apenas **quando**. Reportar para o dev como pendência de auditoria.

**Sinais de bug:**

- `atualizadoEm` não mudou → interceptor falhou.
- `criadoEm` mudou → bug grave de mapeamento.
- `criadoPorUsuarioId` (se exposto) sumiu da resposta.

---

### T13 — Performance

```bash
curl -o /dev/null -s -w "HTTP=%{http_code} Total=%{time_total}s\n" \
  -X PUT "http://localhost:8080/api/v1/clientes/$CLIENTE_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @- <<'JSON'
{
  "nome": "Maria",
  "dataNascimento": "1985-04-12",
  "telefone": "1133224455",
  "celular": "11988887777",
  "email": "maria@exemplo.com",
  "endereco": "Rua Nova, 456"
}
JSON
```

**Esperado:** `HTTP=200` e `Total < 0.5s` em ambiente dev local.

**Sinais de bug:** > 1s consistente — investigar N+1, falta de índice em `id`, ou logging síncrono pesado.

---

## Bugs e crashes a observar

- **500 em vez de 404** quando `id` é Guid válido mas inexistente — middleware não está pegando `NotFoundException`.
- **500 em vez de 409** quando há índice UNIQUE de email/documento e o `SaveChanges` lança `DbUpdateException` não tratada.
- **PUT aceitando `cpf`/`cnpj` no payload** e descartando silenciosamente — UX confusa, sem feedback ao cliente da API.
- **Validator inconsistente com Create** (regras diferentes para telefone/celular/email) — usuário consegue gravar dado pelo PUT que o POST recusaria.
- **`AtualizadoEm` não atualizado** após PUT bem-sucedido — interceptor de EF não rodou.
- **Ausência de rate-limit** no endpoint — possível abuso por atualizações em loop.
- **Ausência de concorrência otimista** (RowVersion/ETag) — last-write-wins silencioso em T11.
- **Resposta vazando campos internos** (ex.: `criadoPorUsuarioId`, hashes, dados de auditoria não filtrados pelo `ClienteResponse`).
- **Falta de campo `AtualizadoPorUsuarioId`** na entidade — impossível rastrear quem editou.
- **Service descartando `usuarioId`** (`_ = usuarioId;`) — confirma o gap de auditoria.
- **Mojibake/encoding** em T10 — content-type ou serializer mal configurados.
- **Logs sem `ClienteId`/`UsuarioId`** — dificulta correlação em incidentes.

---

## Como reportar para o dev

Para cada falha, abrir issue com:

1. **Caso reproduzido:** `T<N>` deste documento.
2. **Comando curl exato** executado (com `$CLIENTE_ID` resolvido).
3. **Request body** enviado.
4. **Resposta recebida:** status code, headers relevantes (`Content-Type`, `WWW-Authenticate`), body completo.
5. **Resposta esperada:** conforme este documento.
6. **Logs do backend:** trecho com `traceId` ou timestamp da requisição. Procurar por `Cliente atualizado. ClienteId=...`.
7. **Estado no banco antes/depois** quando relevante (T11, T12).
8. **Severidade sugerida:**
   - Crítico: 500 em fluxos previsíveis (T3, T9), perda de dados (T11), bypass de auth (T5 retornando 200).
   - Alto: validator inconsistente (T2, T6, T7), gap de unicidade (T9), `AtualizadoEm` não atualizado (T12).
   - Médio: PUT aceitando `cpf`/`cnpj` (T8), ausência de `AtualizadoPorUsuarioId` (T12), performance (T13).
   - Baixo: encoding/unicode (T10) salvo se quebrar persistência.
9. **CA impactado** (referência ao DRP) quando aplicável.
10. **Anexar** screenshot do log estruturado (Serilog) se disponível.
