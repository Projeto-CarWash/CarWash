# POST /api/v1/clientes — Cadastrar cliente (PF ou PJ)

## Resumo

- **Método:** `POST`
- **Path:** `/api/v1/clientes`
- **Propósito:** cria um cliente Pessoa Física (CPF) ou Pessoa Jurídica (CNPJ), com endereço obrigatório e auditoria do usuário criador.
- **Autenticação:** Bearer JWT obrigatório (`[Authorize]` na controller). Sem token → 401.
- **Produces:**
  - `201 Created` — corpo `CreateClienteResponse { id, traceId }` + header `Location: /api/v1/clientes/{id}`.
  - `400 Bad Request` — falha de validação (FluentValidation / model binding).
  - `401 Unauthorized` — ausência ou invalidez do Bearer.
  - `409 Conflict` — `ConflictException` por documento (CPF/CNPJ) já cadastrado.
- **Camada:** `ClientesController.Criar` (`backend/src/CarWash.Api/Controllers/ClientesController.cs:24`) → `IClienteService.CriarAsync`.

---

## Pré-requisitos

1. Backend em execução em `http://localhost:8080` (`dotnet run` no host de `CarWash.Api`).
2. Banco PostgreSQL acessível e com migrations aplicadas.
3. Obter token Bearer via login:

```bash
curl -s -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@carwash.local","senha":"SenhaForte#2026"}' \
  | jq -r '.accessToken'
```

Armazenar em variável de shell:

```bash
export TOKEN="$(curl -s -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@carwash.local","senha":"SenhaForte#2026"}' | jq -r '.accessToken')"
```

4. Gerador de CPF/CNPJ válidos: usar `https://www.4devs.com.br/gerador_de_cpf` ou utilitário equivalente. Anotar o valor gerado em cada teste para reproduzir.
5. Cliente HTTP: `curl` + `jq` para inspeção de JSON.
6. Acesso ao banco para inspeção de `criado_por_usuario_id` (T20).

---

## Tabela resumo dos casos

| Caso | Cenário | Esperado |
|------|---------|----------|
| T1 | Golden PF (CPF válido, todos os campos) | 201 + `id` + `traceId` + `Location` |
| T2 | Golden PJ (CNPJ válido) | 201 |
| T3 | Sem header `Authorization` | 401 |
| T4 | Bearer expirado | 401 |
| T5 | Bearer válido de outro usuário | 201 |
| T6 | CPF com dígitos verificadores errados | 400 |
| T7 | CNPJ inválido | 400 |
| T8 | CPF com máscara `.` e `-` | 400 |
| T9 | CPF/CNPJ já cadastrado | 409 |
| T10 | Email malformado | 400 |
| T11 | Email duplicado em outro cliente | 201 (gap conhecido) |
| T12 | Celular com 10 dígitos | 400 |
| T13 | Telefone fixo malformado | 400 |
| T14 | Nome vazio / <3 / >100 | 400 |
| T15 | Body vazio `{}` | 400 com lista de campos |
| T16 | Body JSON malformado | 400 |
| T17 | Whitespace trailing + UF minúsculo | verificar normalização |
| T18 | Acentos no nome | 201 |
| T19 | Race: 2 POSTs simultâneos com mesmo CPF | 1× 201 + 1× 409 |
| T20 | `criado_por_usuario_id` = `sub` do token | conferir via SQL |

---

## Detalhamento por caso

### T1 — Golden PF (CPF válido)

```bash
curl -i -X POST http://localhost:8080/api/v1/clientes \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @- <<'JSON'
{
  "nome": "Maria Aparecida da Silva",
  "cpf": "39053344705",
  "dataNascimento": "1990-04-12",
  "email": "maria.silva@example.com",
  "celular": "11987654321",
  "telefone": "1133224455",
  "endereco": {
    "cep": "01310100",
    "logradouro": "Avenida Paulista",
    "numero": "1578",
    "complemento": "Apto 12",
    "bairro": "Bela Vista",
    "cidade": "Sao Paulo",
    "uf": "SP"
  }
}
JSON
```

```json
{
  "id": "8f4c8a6b-1d2e-4a35-9b77-c9e1f5b1d2a4",
  "traceId": "0HMV9ABCDE12-00000001"
}
```

- **Status esperado:** `201 Created`.
- **Header esperado:** `Location: /api/v1/clientes/{id}` apontando para `ObterPorId`.
- **Log Serilog:** `Cliente cadastrado com sucesso. TraceId: {TraceId}. ClienteId: {ClienteId}. UsuarioId: {UsuarioId}`.
- **Sinais de bug:** ausência do `Location`, `id` igual a `Guid.Empty`, `traceId` vazio, log sem `UsuarioId`.

---

### T2 — Golden PJ (CNPJ válido)

```bash
curl -i -X POST http://localhost:8080/api/v1/clientes \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @- <<'JSON'
{
  "nome": "Auto Lavagem Brilho Total LTDA",
  "cnpj": "11222333000181",
  "dataNascimento": "1985-01-01",
  "email": "contato@brilhototal.com.br",
  "celular": "11988887777",
  "endereco": {
    "cep": "04567010",
    "logradouro": "Rua das Industrias",
    "numero": "250",
    "bairro": "Vila Industrial",
    "cidade": "Sao Paulo",
    "uf": "SP"
  }
}
JSON
```

- **Status esperado:** `201 Created`.
- **Observação:** `dataNascimento` permanece obrigatório no DTO mesmo para PJ — confirmar com PO se isso é regra ou gap.
- **Sinais de bug:** 400 quando CNPJ é válido; aceitar PJ sem `dataNascimento`.

---

### T3 — Sem header Authorization

```bash
curl -i -X POST http://localhost:8080/api/v1/clientes \
  -H "Content-Type: application/json" \
  -d '{"nome":"Teste","cpf":"39053344705","dataNascimento":"1990-04-12","celular":"11987654321","endereco":{"cep":"01310100","logradouro":"X","numero":"1","bairro":"Y","cidade":"Z","uf":"SP"}}'
```

- **Status esperado:** `401 Unauthorized`.
- **Header esperado:** `WWW-Authenticate: Bearer ...`.
- **Sinais de bug:** retorno 400 (validação antes do auth), 500, ou 401 sem `WWW-Authenticate`.

---

### T4 — Token expirado

Gerar um token e aguardar expiração configurada em `Jwt:ExpirationMinutes`, ou usar token capturado de execução anterior com `exp` no passado.

```bash
export TOKEN_EXP="<jwt_com_exp_no_passado>"
curl -i -X POST http://localhost:8080/api/v1/clientes \
  -H "Authorization: Bearer $TOKEN_EXP" \
  -H "Content-Type: application/json" \
  -d '{}'
```

- **Status esperado:** `401 Unauthorized` com `WWW-Authenticate: Bearer error="invalid_token", error_description="The token expired at ..."`.
- **Sinais de bug:** 200/201 (token expirado aceito), 500.

---

### T5 — Token válido de outro usuário

Logar com um segundo usuário (qualquer perfil autorizado) e reutilizar o token.

```bash
export TOKEN_OUTRO="$(curl -s -X POST http://localhost:8080/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"operador@carwash.local","senha":"SenhaForte#2026"}' | jq -r '.accessToken')"

curl -i -X POST http://localhost:8080/api/v1/clientes \
  -H "Authorization: Bearer $TOKEN_OUTRO" \
  -H "Content-Type: application/json" \
  -d @t1-body.json
```

- **Status esperado:** `201 Created` — qualquer usuário autenticado pode criar cliente (sem policy de role específica nesta rota).
- **Verificar:** `criado_por_usuario_id` no banco bate com o `sub` desse segundo token.
- **Sinais de bug:** 403 (significa que a rota tem policy não documentada); audit gravando o admin original.

---

### T6 — CPF com dígitos verificadores errados

```bash
curl -i -X POST http://localhost:8080/api/v1/clientes \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @- <<'JSON'
{
  "nome": "Joao Teste",
  "cpf": "12345678900",
  "dataNascimento": "1990-04-12",
  "celular": "11987654321",
  "endereco": {
    "cep": "01310100",
    "logradouro": "Rua X",
    "numero": "1",
    "bairro": "Centro",
    "cidade": "Sao Paulo",
    "uf": "SP"
  }
}
JSON
```

- **Status esperado:** `400 Bad Request` com `errors.cpf` mencionando CPF inválido.
- **Sinais de bug:** aceitar como 201, retornar 500.

---

### T7 — CNPJ inválido

```bash
curl -i -X POST http://localhost:8080/api/v1/clientes \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @- <<'JSON'
{
  "nome": "Empresa Falsa LTDA",
  "cnpj": "00000000000000",
  "dataNascimento": "1985-01-01",
  "celular": "11988887777",
  "endereco": {
    "cep": "04567010",
    "logradouro": "Rua Y",
    "numero": "2",
    "bairro": "Vila",
    "cidade": "Sao Paulo",
    "uf": "SP"
  }
}
JSON
```

- **Status esperado:** `400 Bad Request` com `errors.cnpj`.
- **Sinais de bug:** aceitar CNPJ com todos zeros; 500.

---

### T8 — CPF com máscara

```bash
curl -i -X POST http://localhost:8080/api/v1/clientes \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @- <<'JSON'
{
  "nome": "Maria Silva",
  "cpf": "390.533.447-05",
  "dataNascimento": "1990-04-12",
  "celular": "11987654321",
  "endereco": {
    "cep": "01310100",
    "logradouro": "Rua X",
    "numero": "1",
    "bairro": "Centro",
    "cidade": "Sao Paulo",
    "uf": "SP"
  }
}
JSON
```

- **Status esperado:** `400 Bad Request` — validator `ContainsOnlyDigits` rejeita ponto/hífen.
- **Sinais de bug:** aceitar máscara e gravar com pontos no banco (quebra UK).

---

### T9 — Documento duplicado

Executar T1 duas vezes com o mesmo CPF.

```bash
curl -i -X POST http://localhost:8080/api/v1/clientes \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @t1-body.json
# segunda chamada:
curl -i -X POST http://localhost:8080/api/v1/clientes \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @t1-body.json
```

- **Status esperado segunda chamada:** `409 Conflict` originado de `ConflictException` no service.
- **Body esperado:** `ProblemDetails` com `title` indicando documento já cadastrado.
- **Sinais de bug:** 500 com `DbUpdateException` vazando (violação de UK chegando ao banco) — significa que o service não fez o pré-check ou houve race; corrigir mapeamento de exceção.

---

### T10 — Email malformado

```bash
curl -i -X POST http://localhost:8080/api/v1/clientes \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @- <<'JSON'
{
  "nome": "Maria Silva",
  "cpf": "52998224725",
  "dataNascimento": "1990-04-12",
  "email": "isso-nao-eh-email",
  "celular": "11987654321",
  "endereco": {
    "cep": "01310100",
    "logradouro": "Rua X",
    "numero": "1",
    "bairro": "Centro",
    "cidade": "Sao Paulo",
    "uf": "SP"
  }
}
JSON
```

- **Status esperado:** `400 Bad Request` com `errors.email`.
- **Sinais de bug:** aceitar string sem `@`.

---

### T11 — Email duplicado em outro cliente (GAP CONHECIDO)

1. Criar cliente A com `email=duplicado@example.com` e CPF X (esperado 201).
2. Criar cliente B com `email=duplicado@example.com` e CPF Y diferente.

```bash
curl -i -X POST http://localhost:8080/api/v1/clientes \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @- <<'JSON'
{
  "nome": "Cliente B",
  "cpf": "11144477735",
  "dataNascimento": "1992-02-02",
  "email": "duplicado@example.com",
  "celular": "11912345678",
  "endereco": {
    "cep": "01310100",
    "logradouro": "Rua X",
    "numero": "1",
    "bairro": "Centro",
    "cidade": "Sao Paulo",
    "uf": "SP"
  }
}
JSON
```

- **Status atual observado:** `201 Created` — service só valida unicidade de CPF/CNPJ, não de email.
- **Esperado pela regra de negócio:** 409.
- **Marcar como bug em aberto.** Abrir issue com este teste como reprodução. Não fechar este caso até decisão do PO + correção em `IClienteService.CriarAsync`.

---

### T12 — Celular com 10 dígitos

```bash
curl -i -X POST http://localhost:8080/api/v1/clientes \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @- <<'JSON'
{
  "nome": "Maria Silva",
  "cpf": "52998224725",
  "dataNascimento": "1990-04-12",
  "celular": "1187654321",
  "endereco": {
    "cep": "01310100",
    "logradouro": "Rua X",
    "numero": "1",
    "bairro": "Centro",
    "cidade": "Sao Paulo",
    "uf": "SP"
  }
}
JSON
```

- **Status esperado:** `400 Bad Request` com `errors.celular` exigindo 11 dígitos.
- **Sinais de bug:** aceitar 10 dígitos e gravar.

---

### T13 — Telefone fixo malformado

```bash
curl -i -X POST http://localhost:8080/api/v1/clientes \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @- <<'JSON'
{
  "nome": "Maria Silva",
  "cpf": "52998224725",
  "dataNascimento": "1990-04-12",
  "celular": "11987654321",
  "telefone": "(11) 3322-4455",
  "endereco": {
    "cep": "01310100",
    "logradouro": "Rua X",
    "numero": "1",
    "bairro": "Centro",
    "cidade": "Sao Paulo",
    "uf": "SP"
  }
}
JSON
```

- **Status esperado:** `400 Bad Request` — validator rejeita parênteses, espaços e hífen no telefone.
- Testar também: `telefone="123"` (curto) e `telefone="113322445566"` (12 dígitos).
- **Sinais de bug:** aceitar máscara, aceitar comprimento fora de 10–11.

---

### T14 — Nome vazio / curto / longo

Três sub-casos:

```bash
# 14a — vazio
... "nome": "" ...
# 14b — 2 chars
... "nome": "Jo" ...
# 14c — 101 chars
... "nome": "AAAAAAAAAA...(101)" ...
```

- **Status esperado nos três:** `400 Bad Request` com `errors.nome`.
- **Sinais de bug:** truncar silenciosamente em 100, aceitar string vazia.

---

### T15 — Body vazio `{}`

```bash
curl -i -X POST http://localhost:8080/api/v1/clientes \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{}'
```

- **Status esperado:** `400 Bad Request` com lista de erros para `nome`, `dataNascimento`, `celular`, `endereco` e regra XOR cpf/cnpj.
- **Sinais de bug:** 500 (validator não cobre todos os campos), 201 (caminho de teste exposto).

---

### T16 — Body JSON malformado

```bash
curl -i -X POST http://localhost:8080/api/v1/clientes \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"nome":"Teste",'
```

- **Status esperado:** `400 Bad Request` com `ProblemDetails` informando JSON inválido.
- **Sinais de bug:** 500 com stack trace; vazamento de mensagem de parser do `System.Text.Json`.

---

### T17 — Whitespace e UF minúsculo

```bash
curl -i -X POST http://localhost:8080/api/v1/clientes \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @- <<'JSON'
{
  "nome": "  Maria Silva  ",
  "cpf": "52998224725",
  "dataNascimento": "1990-04-12",
  "celular": "11987654321",
  "endereco": {
    "cep": "01310100",
    "logradouro": "  Rua X  ",
    "numero": "1",
    "bairro": "Centro",
    "cidade": "Sao Paulo",
    "uf": "sp"
  }
}
JSON
```

- **Verificar:**
  - O backend faz trim no `nome`? Se sim, confirmar no `GET /api/v1/clientes/{id}` retornando sem espaços.
  - `uf` em minúsculo: validator deve rejeitar com 400, ou normalizar para maiúsculo. Documentar o comportamento real e alinhar com PO se diverge da regra.
- **Sinais de bug:** persistir whitespace cru, persistir `uf="sp"`.

---

### T18 — Acentos no nome

```bash
curl -i -X POST http://localhost:8080/api/v1/clientes \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @- <<'JSON'
{
  "nome": "João da Conceição Açaí",
  "cpf": "52998224725",
  "dataNascimento": "1990-04-12",
  "celular": "11987654321",
  "endereco": {
    "cep": "01310100",
    "logradouro": "Rua das Açucenas",
    "numero": "1",
    "bairro": "Centro",
    "cidade": "São Paulo",
    "uf": "SP"
  }
}
JSON
```

- **Status esperado:** `201 Created`.
- **Verificar:** `GET` retorna acentos corretos (UTF-8 íntegro).
- **Sinais de bug:** `?` ou mojibake (`JoÃ£o`) no retorno — indica problema de encoding no provider ou no client.

---

### T19 — Race: dois POSTs simultâneos com mesmo CPF

Disparar em paralelo via shell:

```bash
BODY=$(cat <<'JSON'
{
  "nome": "Race Test",
  "cpf": "52998224725",
  "dataNascimento": "1990-04-12",
  "celular": "11987654321",
  "endereco": {
    "cep": "01310100",
    "logradouro": "Rua X",
    "numero": "1",
    "bairro": "Centro",
    "cidade": "Sao Paulo",
    "uf": "SP"
  }
}
JSON
)

(curl -s -o /tmp/r1.json -w "%{http_code}\n" -X POST http://localhost:8080/api/v1/clientes \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "$BODY") &
(curl -s -o /tmp/r2.json -w "%{http_code}\n" -X POST http://localhost:8080/api/v1/clientes \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "$BODY") &
wait
```

- **Status esperado:** exatamente 1× `201` + 1× `409`.
- **Banco:** apenas 1 linha com esse CPF.
- **Sinais de bug:** 2× 201 (UK do banco não está protegendo / pré-check sem transação), 1× 201 + 1× 500 (race derruba o request com `DbUpdateException` em vez de mapear para 409).

---

### T20 — Auditoria `criado_por_usuario_id`

Após T1, decodificar o `sub` do `$TOKEN` (`jwt.io` ou `jq -R 'split(".")[1] | @base64d | fromjson'`) e consultar:

```sql
SELECT id, nome, cpf, cnpj, criado_por_usuario_id, criado_em
FROM clientes
WHERE id = '<id retornado em T1>';
```

- **Esperado:** `criado_por_usuario_id` == `sub` do JWT usado no POST.
- **Esperado log:** `Cliente cadastrado com sucesso. TraceId: {TraceId}. ClienteId: {ClienteId}. UsuarioId: {UsuarioId}` com `UsuarioId` batendo.
- **Sinais de bug:** `criado_por_usuario_id` NULL, vazio ou apontando para outro usuário — auditoria quebrada, escalonar imediatamente.

---

## Bugs e crashes a observar

- `500 Internal Server Error` em vez de `409 Conflict` em violação de UK (CPF/CNPJ). Indica falha de pré-check ou middleware de exceção não mapeando `DbUpdateException`.
- Aceitar CPF/CNPJ com letras, máscara, dígitos verificadores errados ou somente zeros.
- Não normalizar documento (gravar com ponto/hífen) — quebra UK e busca posterior.
- `ProblemDetails` vazando detalhes do banco (nome de constraint, schema, stack) no body de erro.
- `401 Unauthorized` retornado sem header `WWW-Authenticate: Bearer ...`.
- **Email duplicado aceito como 201 (T11)** — gap atual do service, ainda não corrigido.
- `criado_por_usuario_id` não preenchido ou apontando para usuário errado (T20) — auditoria quebrada, viola DRP.
- Trim/normalização ausente em campos de endereço (T17) — dados sujos no banco.
- Body malformado (T16) retornando 500 em vez de 400.
- Race (T19) resultando em duplicidade ou 500 — RN011 análoga aplicável a unicidade de documento.

---

## Como reportar para o dev

Para cada falha, abrir issue com o seguinte template:

```
Título: [POST /api/v1/clientes] T<N> — <resumo objetivo do bug>

Ambiente:
- Branch / commit:
- URL backend: http://localhost:8080
- Versão do .NET / migrations aplicadas:

Passos para reproduzir:
1. Token usado (sub do JWT):
2. Comando curl exato (com body):
3. Hora UTC da execução:

Esperado:
- Status: <ex.: 409>
- Body: <ex.: ProblemDetails com documento duplicado>
- Log Serilog: <linha esperada>

Observado:
- Status: <ex.: 500>
- Body (na íntegra):
- Log Serilog (linhas correlatas, com TraceId):
- TraceId: <copiar da resposta ou logs>

Evidências anexadas:
- Resposta HTTP completa (headers + body).
- Linhas do log com o mesmo TraceId.
- Query SQL e resultado (se for caso de auditoria/UK).
- Captura do banco antes/depois.

Caso de teste de referência: QA/clientes/POST_clientes.md#T<N>
Criticidade sugerida: <bloqueador | alta | média | baixa>
Regra de negócio violada: <RFxxx / RNxxx / CAxxx, se aplicável>
```

Anexar sempre o `TraceId` retornado no body (ou no header `traceparent`) para o dev correlacionar com a entrada Serilog `Cliente cadastrado com sucesso. TraceId: {TraceId}...` (ou a entrada de erro equivalente). Casos de race (T19) e auditoria (T20) entram como **bloqueadores**. Caso T11 (email duplicado) é gap conhecido — abrir como bug com prioridade alinhada com PO/PM antes de fechar a sprint.
