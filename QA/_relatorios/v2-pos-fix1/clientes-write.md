# Relatório — Clientes Write (REBATERIA pós-fix)

Data: 2026-05-17T15:07:29Z
Backend: http://localhost:8080
Rodada anterior em: ./v1-pre-fix/clientes-write.md
Bugs fechados desde v1: **BUG-CW-AUTH-001** (login admin agora retorna 200 + `accessToken`).
Migrations pendentes detectadas: **`20260517061810_RefatoraClienteEndereco`** — arquivo presente em `backend/src/CarWash.Infrastructure/Persistence/Migrations/` mas **não aplicada** no container `carwash-postgres` (tabela `__ef_migrations_history` lista apenas `20260513114525_InitialSchema` e `20260517022432_AddUsuarioLockoutFields`).

---

## Comparativo v1 vs v2

|         | v1  | v2  |
| ------- | --: | --: |
| PASS    |   5 |  21 |
| FAIL    |   0 |   0 |
| BLOCKED |  42 |  26 |
| Total   |  47 |  47 |

> Ganho líquido v2: **+16 casos PASS** habilitados pelo fix de login. Todos os casos que param em validator (FluentValidation) ou em route constraint (`:guid`)/auth pipeline agora retornam status correto. Os 26 casos restantes em BLOCKED são exatamente os que tocam o repositório de `Cliente` (SELECT/INSERT/UPDATE em `public.clientes`) e batem no schema drift descrito em **BUG-009**.

---

## Sumário

- **Total:** 47 (POST=20, PUT=13, PATCH=14)
- **PASS:** 21
- **FAIL:** 0
- **BLOCKED:** 26 (todos com causa raiz BUG-009)
- **Bugs ainda abertos:** 7 (BUG-009 novo + 6 gaps de v1 ainda não confirmáveis: T11 POST, T8/T9/T12 PUT, T10/T12 PATCH)
- **Bugs novos:** 1 (BUG-009 — migração `RefatoraClienteEndereco` não aplicada → 500 em todo write que toca DB de cliente)
- **Bugs fechados:** 1 (BUG-CW-AUTH-001 — login agora 200)

---

## Bugs

### BUG-009 — Migration `RefatoraClienteEndereco` não aplicada no container (schema drift entidade ↔ tabela `clientes`)

- **Severidade:** **CRÍTICO** (bloqueia 100% dos writes POST/PUT/PATCH-status que tocam o agregado `Cliente`; impacta CA001, CA002, CA003 em homologação).
- **Endpoints afetados:**
  - `POST /api/v1/clientes` → 500 em qualquer body que passe pelo validator e chegue em `ClienteRepository.AdicionarAsync`.
  - `PUT /api/v1/clientes/{id}` → 500 em qualquer caso que passe pelo validator e chegue em `clienteRepository.ObterPorIdAsync` (inclusive id inexistente, que deveria devolver 404).
  - `PATCH /api/v1/clientes/{id}/status` → 500 em qualquer caso com body válido (inclui id inexistente, que deveria devolver 404).
  - `GET /api/v1/clientes` e `GET /api/v1/clientes/{id}` provavelmente também afetados (não testado nesta suíte de write, mas o `SELECT` projeta as mesmas colunas).
- **Causa raiz:** a entidade `CarWash.Domain.Entities.Cliente` (`backend/src/CarWash.Domain/Entities/Cliente.cs:32-56`) e o repositório esperam as colunas `data_nascimento date NOT NULL`, `celular varchar(11) NOT NULL` (deveria estar NOT NULL e está nullable), `endereco_cep varchar(8) NOT NULL`, `endereco_logradouro varchar(150) NOT NULL`, `endereco_numero varchar(20) NOT NULL`, `endereco_complemento varchar(100) NULL`, `endereco_bairro varchar(100) NOT NULL`, `endereco_cidade varchar(100) NOT NULL`, `endereco_uf char(2) NOT NULL`; e a remoção de `endereco varchar(255)` e `observacoes text`. A migration `20260517061810_RefatoraClienteEndereco.cs` aplica exatamente essas mudanças, mas **NÃO foi executada** no banco do container.
- **Evidência DB — schema atual de `public.clientes`:**

  ```
  Column        | Type                        | Nullable
  --------------+-----------------------------+---------
  id            | uuid                        | NOT NULL
  nome          | varchar(100)                | NOT NULL
  cpf           | varchar(11)                 | NULL  (uk_clientes_cpf parcial)
  cnpj          | varchar(14)                 | NULL  (uk_clientes_cnpj parcial)
  telefone      | varchar(11)                 | NULL
  celular       | varchar(11)                 | NULL  ← entidade espera NOT NULL
  email         | varchar(150)                | NULL
  endereco      | varchar(255)                | NULL  ← entidade DROPPED
  observacoes   | text                        | NULL  ← entidade DROPPED
  ativo         | boolean                     | NOT NULL DEFAULT true
  criado_em     | timestamptz                 | NOT NULL DEFAULT now()
  atualizado_em | timestamptz                 | NOT NULL DEFAULT now()
  ```

  Faltam: `data_nascimento`, `endereco_cep`, `endereco_logradouro`, `endereco_numero`, `endereco_complemento`, `endereco_bairro`, `endereco_cidade`, `endereco_uf`. Tabela `enderecos` não existe (confirmado).

- **Migrations aplicadas (`__ef_migrations_history`):**

  ```
  20260513114525_InitialSchema
  20260517022432_AddUsuarioLockoutFields
  ```

  **Faltando:** `20260517061810_RefatoraClienteEndereco` (existe no FS, não no banco).

- **Reprodução (POST golden path):**

  ```bash
  TOKEN=$(curl -s -X POST http://localhost:8080/api/v1/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"admin@carwash.local","senha":"DevSeedAdmin2026!Forte"}' \
    | python3 -c 'import json,sys;print(json.load(sys.stdin)["accessToken"])')

  curl -i -X POST http://localhost:8080/api/v1/clientes \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d '{"nome":"Maria","cpf":"39053344705","dataNascimento":"1990-04-12","celular":"11987654321","endereco":{"cep":"01310100","logradouro":"Av X","numero":"1","bairro":"B","cidade":"C","uf":"SP"}}'
  ```

  Resposta:

  ```
  HTTP/1.1 500 Internal Server Error
  Content-Type: application/problem+json

  {"type":"https://carwash/errors/internal-error",
   "title":"Não foi possível concluir a operação no momento. Tente novamente.",
   "status":500,
   "correlationId":"78b2a8c8e6d64c2cb7134edd6c1915c4"}
  ```

- **Log Serilog (trecho — `docker logs carwash-backend`):**

  ```
  [14:58:12 ERR] Failed executing DbCommand (4ms)
  INSERT INTO public.audit_logs (...) ...
  [14:58:12 ERR] An exception occurred in the database while saving changes for context type 'CarWashDbContext'.
   ---> Npgsql.PostgresException (0x80004005): 42703: column "data_nascimento" of relation "clientes" does not exist
      Severity: ERROR
      SqlState: 42703
      File: parse_relation.c
     at CarWash.Infrastructure.Repositories.ClienteRepository.AdicionarAsync(...) ClienteRepository.cs:line 71
  [14:58:12 ERR] Falha não tratada. CorrelationId=78b2a8c8e6d64c2cb7134edd6c1915c4
  ```

  E também para SELECT (PUT/PATCH):

  ```
  [14:57:44 ERR] An exception occurred while iterating over the results of a query for context type 'CarWashDbContext'.
  Npgsql.PostgresException (0x80004005): 42703: column c.data_nascimento does not exist
  ```

- **Sugestão de correção (ordem de preferência):**
  1. Aplicar a migration no container — `docker exec -e ConnectionStrings__Default=... carwash-backend dotnet ef database update --project src/CarWash.Infrastructure --startup-project src/CarWash.Api` (ou usar `db.Database.Migrate()` no startup quando `ASPNETCORE_ENVIRONMENT in {Development,Testing}`, como já recomendado em v1).
  2. Bloquear startup do backend se `db.Database.GetPendingMigrationsAsync()` não estiver vazio — fail fast em dev/test, exige decisão consciente em produção.
  3. Adicionar teste de integração `SchemaConsistencyTests.NaoDeveHaverMigrationPendente` que falha o CI quando há drift entidade ↔ tabela. **Esta é exatamente a classe de bug que CA011 deveria pegar antes de homologação.**
  4. Em paralelo, mapear `Npgsql.PostgresException` com `SqlState=42703` no `ExceptionHandlingMiddleware` para um 503 distinto com hint `database-schema-drift`, em vez do 500 genérico — para que smoke testes detectem este sintoma.

---

## Bugs ainda abertos (gaps de v1 que dependem do fix BUG-009 para serem confirmados em runtime)

| ID                 | Origem                  | Status v2 | Observação                                                                                                                              |
| ------------------ | ----------------------- | --------- | --------------------------------------------------------------------------------------------------------------------------------------- |
| GAP-CW-CLI-EMAIL-1 | POST T11                | BLOCKED   | service `ClienteService.CriarAsync` confirmadamente só valida unicidade de CPF/CNPJ (lido em código, não há `ExisteEmailAsync`). Continua gap.        |
| GAP-CW-CLI-PUT-CPF | PUT T8                  | BLOCKED   | `UpdateClienteRequest` não tem `Cpf`/`Cnpj` (confirmado em código). Service ignora silenciosamente — alerta de UX permanece.            |
| GAP-CW-CLI-PUT-EML | PUT T9                  | BLOCKED   | `ClienteService.AtualizarAsync` não valida unicidade de email. Confirmado em código.                                                    |
| GAP-CW-CLI-AUDIT   | PUT T12, PATCH T13      | BLOCKED   | `_ = usuarioId;` em `AtualizarAsync` e `AlterarStatusAsync` confirmado. Entidade `Cliente` não tem `AtualizadoPorUsuarioId`.            |
| GAP-CW-CLI-STA-EMP | PATCH T10               | BLOCKED   | `AlterarStatusClienteRequest { bool Ativo }` — `bool` não-anulável; body `{}` desserializa com `Ativo=false`. Gap de validação confirmado em código. |
| GAP-CW-CLI-STA-AGD | PATCH T12               | BLOCKED   | `AlterarStatusAsync` não checa agendamentos abertos antes de desativar. Confirmado em código. RN de negócio ausente.                    |

> Todos os 6 gaps acima são **comportamentais** e independentes de BUG-009 do ponto de vista de código — porém, em runtime, todos batem no schema drift do `ObterPorIdAsync` e dão 500 antes de chegar ao caminho que evidenciaria o gap. Por isso permanecem BLOCKED nesta rodada.

---

## POST /api/v1/clientes (20 casos)

| ID    | Descrição                                       | Esperado                       | Obtido                                                                  | Resultado | Bug          |
| ----- | ----------------------------------------------- | ------------------------------ | ----------------------------------------------------------------------- | --------- | ------------ |
| T1    | Golden PF (CPF válido)                          | 201 + id + traceId             | **500** + correlationId; log `42703 data_nascimento`                    | BLOCKED   | BUG-009      |
| T2    | Golden PJ (CNPJ válido)                         | 201                            | esperado 500 idêntico ao T1 (mesma causa)                               | BLOCKED   | BUG-009      |
| T3    | Sem header `Authorization`                      | 401 + WWW-Authenticate: Bearer | **401**, `WWW-Authenticate: Bearer`, `X-Correlation-Id`, `Content-Length: 0` | PASS      | —            |
| T4    | Bearer expirado/inválido                        | 401                            | **401** + `WWW-Authenticate: Bearer error="invalid_token", error_description="The signature key was not found"` | PASS      | —            |
| T5    | Bearer válido de outro usuário                  | 201                            | só há um usuário seed (`admin`); golden falha com 500 → não confirmável | BLOCKED   | BUG-009      |
| T6    | CPF com DV errado (`12345678900`)               | 400 `errors.cpf`               | **400** `{"errors":{"Cpf":["CPF inválido."]}}`                          | PASS      | —            |
| T7    | CNPJ inválido (zeros)                           | 400 `errors.cnpj`              | **400** `{"errors":{"Cnpj":["CNPJ inválido."]}}`                        | PASS      | —            |
| T8    | CPF com máscara                                 | 400                            | **400** `{"errors":{"Cpf":["CPF deve conter apenas números."]}}`        | PASS      | —            |
| T9    | CPF duplicado (mesmo body 2x)                   | 1×201 + 1×409                  | 1ª chamada já dá 500 (BUG-009), nunca chega ao pré-check de unicidade   | BLOCKED   | BUG-009      |
| T10   | Email malformado                                | 400 `errors.email`             | **400** `{"errors":{"Email":["E-mail inválido."]}}`                     | PASS      | —            |
| T11   | Email duplicado em outro cliente (gap)          | 201 (gap atual) ou 409 (regra) | **500** (passa o validator, bate no INSERT)                             | BLOCKED   | BUG-009 + GAP-CW-CLI-EMAIL-1 |
| T12   | Celular 10 dígitos                              | 400                            | **400** `{"errors":{"Celular":["Celular deve conter 11 dígitos."]}}`    | PASS      | —            |
| T13   | Telefone fixo com máscara `(11) 3322-4455`      | 400                            | **400** `{"errors":{"Telefone":["Telefone deve conter apenas números."]}}` | PASS  | —            |
| T14a  | Nome vazio                                      | 400                            | **400** `{"errors":{"Nome":["O nome é obrigatório."]}}`                  | PASS      | —            |
| T14b  | Nome 2 chars (`Jo`)                             | 400                            | **400** `{"errors":{"Nome":["O nome deve ter no mínimo 3 caracteres."]}}`| PASS      | —            |
| T14c  | Nome 101 chars (`A`×101)                        | 400                            | **400** `{"errors":{"Nome":["O nome deve ter no máximo 100 caracteres."]}}` | PASS   | —            |
| T15   | Body vazio `{}`                                 | 400 lista de campos            | **400** errors: `Nome`, `documento`, `Celular`, `Endereco`              | PASS      | —            |
| T16   | JSON malformado (`{"nome":"Teste",`)            | 400                            | **400** errors: `$` (JSON parse) + `request`                            | PASS      | —            |
| T17   | Whitespace no nome + `uf: "sp"` (minúsculo)     | trim + normalização            | **500** — validator de UF só checa `Length(2)`, deixa passar; bate no DB | BLOCKED  | BUG-009 (validator de UF ainda não exige maiúscula — secundário) |
| T18   | Acentos no nome                                 | 201 + UTF-8 íntegro            | esperado 500 (mesma rota de INSERT)                                     | BLOCKED   | BUG-009      |
| T19   | Race: 2 POSTs simultâneos com mesmo CPF         | 1×201 + 1×409                  | duas chamadas dão 500 antes de qualquer concorrência ser exercida       | BLOCKED   | BUG-009      |
| T20   | Auditoria `criado_por_usuario_id` = `sub` JWT   | match SQL                      | INSERT não acontece (500) — não há linha para auditar                   | BLOCKED   | BUG-009      |

> Observação T14 conta como **1 caso** no agregado de 20 (sub-casos a/b/c).
> Observação T17: o validator atual de `Endereco.Uf` faz apenas `NotEmpty().Length(2)`. Recomendar regra adicional `.Matches("^[A-Z]{2}$")` ou normalização `ToUpperInvariant` no service. Sub-issue secundário, não bloqueador.

**Subtotal POST:** PASS=12 · BLOCKED=8 · FAIL=0.

---

## PUT /api/v1/clientes/{id} (13 casos)

| ID  | Descrição                                                  | Esperado                                    | Obtido                                                                  | Resultado | Bug                       |
| --- | ---------------------------------------------------------- | ------------------------------------------- | ----------------------------------------------------------------------- | --------- | ------------------------- |
| T1  | Golden path — body completo                                | 200 + `ClienteResponse`                     | **500** — SELECT bate em `c.data_nascimento` ausente                    | BLOCKED   | BUG-009                   |
| T2  | Body parcial (sem `nome`)                                  | 400                                         | **400** `{"errors":{"Nome":["O nome é obrigatório."]}}` (validator antes do DB) | PASS  | —                         |
| T3  | Id Guid inexistente                                        | 404                                         | **500** — `ObterPorIdAsync` falha no SELECT antes de retornar null      | BLOCKED   | BUG-009                   |
| T4  | Id não-Guid (`abc`)                                        | 404 (route constraint)                      | **404** + `X-Correlation-Id`, `Content-Length: 0`                       | PASS      | —                         |
| T5  | Sem `Authorization`                                        | 401                                         | **401** + `WWW-Authenticate: Bearer`                                    | PASS      | —                         |
| T6  | Body vazio `{}`                                            | 400 lista                                   | **400** errors: `Nome`, `Celular`, `Endereco`                           | PASS      | —                         |
| T7  | Celular não-numérico (`abc12345`)                          | 400                                         | **400** `{"errors":{"Celular":["Celular deve conter apenas números."]}}`| PASS      | —                         |
| T8  | Body com `cpf`/`cnpj` (gap UX — campos descartados)        | 200 silencioso (gap)                        | **500** (mesmo body válido bate no SELECT)                              | BLOCKED   | BUG-009 + GAP-CW-CLI-PUT-CPF |
| T9  | Email duplicado entre clientes (gap)                       | 200 silencioso (gap) ou 409                 | **500** (gap não confirmável em runtime)                                | BLOCKED   | BUG-009 + GAP-CW-CLI-PUT-EML |
| T10 | Unicode no nome                                            | 200                                         | esperado 500 (mesma causa)                                              | BLOCKED   | BUG-009                   |
| T11 | Race condition (dois PUTs paralelos)                       | last-write-wins                             | esperado 500 em ambos antes de qualquer concorrência                    | BLOCKED   | BUG-009                   |
| T12 | Invariantes: `criadoEm` imutável; `atualizadoEm` cresce; sem `AtualizadoPorUsuarioId` (gap) | conferir invariantes | impossível (PUT não persiste) | BLOCKED | BUG-009 + GAP-CW-CLI-AUDIT |
| T13 | Performance < 500ms                                        | HTTP=200, total<0.5s                        | 500 (não medível)                                                       | BLOCKED   | BUG-009                   |

**Subtotal PUT:** PASS=5 · BLOCKED=8 · FAIL=0.

---

## PATCH /api/v1/clientes/{id}/status (14 casos)

| ID  | Descrição                                          | Esperado                              | Obtido                                                                | Resultado | Bug                           |
| --- | -------------------------------------------------- | ------------------------------------- | --------------------------------------------------------------------- | --------- | ----------------------------- |
| T1  | Desativar cliente ativo                            | 200, `ativo:false`                    | **500** (`SELECT` em `clientes` bate no drift)                        | BLOCKED   | BUG-009                       |
| T2  | Reativar cliente inativo                           | 200, `ativo:true`                     | esperado 500 idêntico                                                 | BLOCKED   | BUG-009                       |
| T3  | Toggle repetido (idempotência)                     | 200/200                               | 500/500                                                               | BLOCKED   | BUG-009                       |
| T4  | Id Guid inexistente                                | 404                                   | **500** — SELECT falha antes de retornar null                          | BLOCKED   | BUG-009                       |
| T5  | Id não-Guid (`abc`)                                | 404 (route constraint)                | **404** + `X-Correlation-Id`                                           | PASS      | —                             |
| T6  | Sem `Authorization`                                | 401                                   | **401** + `WWW-Authenticate: Bearer`                                   | PASS      | —                             |
| T7  | Body ausente (sem `--data`)                        | 400                                   | **400** errors: `[""]: A non-empty request body is required.` + `request` | PASS  | —                             |
| T8  | `ativo: null`                                      | 400                                   | **400** errors: `$.ativo: cannot convert null to System.Boolean`      | PASS      | —                             |
| T9  | `ativo: "sim"`                                     | 400                                   | **400** errors: `$.ativo: cannot convert string to System.Boolean`    | PASS      | —                             |
| T10 | Body `{}` (gap — desativa silenciosamente)         | 400 (esperado) ou 200 (gap atual)     | **500** (passa desserialização com `Ativo=false`, bate no SELECT)     | BLOCKED   | BUG-009 + GAP-CW-CLI-STA-EMP  |
| T11 | Campo extra + mass assignment                      | 200, ignora extras                    | **500** (mesma causa)                                                  | BLOCKED   | BUG-009                       |
| T12 | Desativar cliente com agendamentos ativos          | 409 esperado / 200 atual (gap)        | **500** (gap não confirmável)                                          | BLOCKED   | BUG-009 + GAP-CW-CLI-STA-AGD  |
| T13 | Log estruturado com `TraceId`/`UsuarioId`          | linha presente                        | log presente porém de **erro** (`Falha não tratada. CorrelationId=...`), não de sucesso; `UsuarioId` não logado em caminho de erro | BLOCKED | BUG-009 + GAP-CW-CLI-AUDIT |
| T14 | Race (PATCH paralelos opostos)                     | last-write-wins, sem 500              | 500/500 (ambos)                                                       | BLOCKED   | BUG-009                       |

**Subtotal PATCH:** PASS=5 · BLOCKED=9 · FAIL=0.

---

## Detalhes adicionais coletados

### Headers de erro padrão observados

Todos os 500 retornam `Content-Type: application/problem+json` com payload:

```json
{
  "type": "https://carwash/errors/internal-error",
  "title": "Não foi possível concluir a operação no momento. Tente novamente.",
  "status": 500,
  "correlationId": "<hex32>"
}
```

Sem vazamento de stack/SQL no body — bom. O `correlationId` correlaciona com a linha Serilog `Falha não tratada. CorrelationId=...` no log do container.

### Headers de 401 observados

- `WWW-Authenticate: Bearer` (sem token).
- `WWW-Authenticate: Bearer error="invalid_token", error_description="The signature key was not found"` (token inválido/expirado/assinatura ruim).
- `Content-Length: 0`.
- `X-Correlation-Id` presente.

Comportamento alinhado com RFC 6750.

### Headers de 404 por route constraint (id não-Guid)

- `Content-Length: 0`.
- `X-Correlation-Id` presente.
- Sem body. Bom — não vaza detalhe de roteamento.

### Token JWT decodificado (`sub`)

```json
{
  "sub": "00000000-0000-0000-0000-000000000001",
  "email": "admin@carwash.local",
  "name": "Administrador",
  "perfil": "Admin",
  "iss": "carwash",
  "aud": "carwash-web",
  "exp": <ttl 15min>
}
```

T20 (auditoria) seria confirmável após o fix de BUG-009 comparando `criado_por_usuario_id` com `00000000-0000-0000-0000-000000000001`.

---

## Limpeza

Nenhum cliente criado nesta rodada (todos os POSTs golden falharam com 500 antes do INSERT). Banco permanece inalterado em `public.clientes`. Nenhum `DELETE` executado.

---

## Recomendação ao gestor da sprint

1. **Aplicar `20260517061810_RefatoraClienteEndereco` imediatamente** no container — bloqueador absoluto de toda funcionalidade de cliente. Sem isso, CA001/CA002/CA003 do MVP estão retidos.
2. Após aplicar a migration, **rebater esta suíte uma terceira vez** (v3) — os 26 BLOCKED viram PASS/FAIL conforme caso. Atenção especial em:
   - T9 POST (409 em CPF duplicado).
   - T11 POST e T9 PUT (gaps de email duplicado — esperar 201/200 silenciosos hoje; abrir issue separada para regra).
   - T19 POST (race condition mesmo CPF — confirmar 1×201 + 1×409 e que UK do banco protege).
   - T20 POST (auditoria `criado_por_usuario_id`).
   - T12 PUT (gap `AtualizadoPorUsuarioId`).
   - T10 PATCH (body `{}` desativando silenciosamente).
   - T12 PATCH (sem RN bloqueando desativação com agendamentos abertos).
3. **Adicionar gate em CI** (`SchemaConsistencyTests`) que falha quando `db.Database.GetPendingMigrationsAsync()` retorna qualquer item — esta classe de bug é exatamente o que CA011 deveria atender.
4. **Sub-issue de UF**: o validator atual deixa passar `uf: "sp"`. Normalizar para uppercase no service ou adicionar `.Matches("^[A-Z]{2}$")` no validator. Não bloqueador, mas higiênico.
5. **Cobertura de testes pós-fix**: criar `ClientesEndpointsIntegrationTests` com `[Trait("CA","011")]` cobrindo T1, T9, T11, T19 (POST), T1, T3, T9 (PUT), T1, T4, T10, T12 (PATCH) usando `WebApplicationFactory` + Testcontainers. Hoje a suíte de teste automatizada do CA011 não pega este drift — recomendar bloqueio de merge sem migration aplicada.
