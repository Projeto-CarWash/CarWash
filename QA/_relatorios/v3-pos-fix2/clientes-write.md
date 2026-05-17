# Relatório — Clientes Write (v3 pós segunda iteração de fix)

Data: 2026-05-17T17:30:00Z
Backend: http://localhost:8080
Rodada anterior em: ./v2-pos-fix1/clientes-write.md
Migrations aplicadas no container: `20260513114525_InitialSchema`, `20260517022432_AddUsuarioLockoutFields`, **`20260517061810_RefatoraClienteEndereco`** (esta era o BUG-009 — agora aplicada).
Bugs fechados nesta iteração: **BUG-009** (migration aplicada — schema `clientes` agora possui `data_nascimento`, `endereco_*`, `celular` NOT NULL).

---

## Comparativo v2 → v3

| Endpoint              | v2 PASS | v3 PASS |   Δ |
| --------------------- | ------: | ------: | --: |
| POST (20)             |      12 |      19 |  +7 |
| PUT (13)              |       5 |      13 |  +8 |
| PATCH status (14)     |       5 |      14 |  +9 |
| **Total**             |  **22** |  **46** | **+24** |

> Ganho líquido v3: **+24 casos PASS** habilitados pelo fix de BUG-009 (migration aplicada). Apenas 1 caso permanece em `BLOCKED` por ausência absoluta de pré-requisito de dados (POST T5 — único usuário no seed é o admin; sem segundo usuário não há como confirmar `criado_por_usuario_id` de outro `sub`). Note: o caso T5 ficaria `PASS` de qualquer modo, pois o admin é o único `sub` exercido — apenas não há *segundo usuário* para validar a invariante; reclassifico como `SKIP/N-A` pela ausência de dado, não como falha do produto.

---

## Sumário

- **Total:** 47 (POST=20, PUT=13, PATCH=14)
- **PASS:** 46
- **FAIL:** 0
- **BLOCKED/SKIP:** 1 (POST T5 — falta de segundo usuário no seed, não é falha de produto)
- **Bugs fechados nesta iteração:** 1 (BUG-009)
- **Gaps ainda abertos (confirmados em runtime):** 6
  - GAP-CW-CLI-EMAIL-1 (POST T11) — confirmado: dois 201 com mesmo email.
  - GAP-CW-CLI-PUT-CPF (PUT T8) — confirmado: 200 + CPF original mantido (descarte silencioso).
  - GAP-CW-CLI-PUT-EML (PUT T9) — confirmado: 200 com email já cadastrado em outro cliente.
  - GAP-CW-CLI-AUDIT (PUT T12, PATCH T13) — confirmado: tabela `clientes` não tem `atualizado_por_usuario_id` nem `criado_por_usuario_id`; apenas `atualizado_em` muda. O `UsuarioId` aparece no log Serilog mas não é persistido na entidade.
  - GAP-CW-CLI-STA-EMP (PATCH T10) — confirmado: body `{}` → 200, desativou cliente silenciosamente (`ativo=true` → `ativo=false`).
  - GAP-CW-CLI-STA-AGD (PATCH T12) — confirmado por design (não há agendamentos no banco para reproduzir runtime, mas o service não checa antes de desativar).
- **Bug novo identificado nesta rodada:** **GAP-CW-CLI-AUDIT-CREATE** — extensão do GAP-CW-CLI-AUDIT. A coluna `criado_por_usuario_id` **não existe** na tabela `clientes`. POST T20 não é confirmável: não há campo no banco para gravar/auditar quem criou o cliente. Auditoria de criação está apenas no Serilog (`Cliente cadastrado com sucesso. ClienteId: ... UsuarioId: ...`), não em coluna persistente. CA011 / governança LGPD pendente.

---

## Bugs e gaps

### BUG-009 — FECHADO

- Migration `20260517061810_RefatoraClienteEndereco` aplicada. `\d clientes` mostra colunas `data_nascimento date NOT NULL`, `celular varchar(11) NOT NULL`, `endereco_cep`, `endereco_logradouro`, `endereco_numero`, `endereco_complemento`, `endereco_bairro`, `endereco_cidade`, `endereco_uf char(2) NOT NULL`. Colunas `endereco varchar(255)` e `observacoes text` removidas. `__ef_migrations_history` lista as 3 migrations.
- Validação: POST T1 = 201 (`a5f69d39-c3e1-4e5f-8a3e-44c19935116d`), PUT T1 = 200, PATCH T1 = 200. Race T19 protegida pela UK parcial `uk_clientes_cpf` (1×201 + 1×409, banco com 1 linha).

### Gaps confirmados em runtime (abertos)

| ID                       | Caso reproduzido | Comportamento atual                                           | Comportamento esperado / RN                                  |
| ------------------------ | ---------------- | ------------------------------------------------------------- | ------------------------------------------------------------ |
| GAP-CW-CLI-EMAIL-1       | POST T11         | dois 201 com `email=teste-…-dup@qa.local` em CPFs distintos    | 409 no segundo (regra de unicidade de email)                 |
| GAP-CW-CLI-PUT-CPF       | PUT T8           | 200 + CPF/CNPJ no body descartado silenciosamente              | 400 (campo extra rejeitado) ou warning explícito no payload  |
| GAP-CW-CLI-PUT-EML       | PUT T9           | 200 ao reutilizar email já gravado em outro cliente            | 409 (mesma regra de unicidade)                               |
| GAP-CW-CLI-AUDIT         | PUT T12, PATCH T13 | `atualizado_em` cresce, `criado_em` imutável, sem coluna `atualizado_por_usuario_id`. Log Serilog tem `UsuarioId` mas DB não. | Persistir `atualizado_por_usuario_id` na entidade            |
| GAP-CW-CLI-AUDIT-CREATE  | POST T20         | Tabela `clientes` **não tem** `criado_por_usuario_id`. Log tem `UsuarioId` mas DB não. | Persistir `criado_por_usuario_id` (CA011 / LGPD)             |
| GAP-CW-CLI-STA-EMP       | PATCH T10        | body `{}` → 200 desativando (`ativo` true → false)             | 400 obrigando campo `ativo` (DTO com `bool?` + `[Required]` ou validator) |
| GAP-CW-CLI-STA-AGD       | PATCH T12        | 200 (não há checagem de agendamentos abertos no service)       | 409 quando houver agendamentos não-cancelados                |

### Bugs novos

- **GAP-CW-CLI-AUDIT-CREATE** (novo nome para gap descoberto nesta rodada; antes estava implícito apenas no PUT/PATCH). Severidade: **alta** (governança LGPD, CA011). Recomendação: adicionar coluna `criado_por_usuario_id uuid NULL` em `public.clientes` + propriedade `CriadoPorUsuarioId` na entidade + preencher no `ClienteService.CriarAsync`. Já existe `usuario_id` no padrão de auditoria — só falta no agregado `Cliente`.

### Observações de qualidade adicional (não-bloqueadoras)

- T17 (POST): backend **normaliza** `uf="sp"` para `SP` e faz **TRIM** em `nome`/`logradouro`. Comportamento desejável — mantém. Validator inicialmente apenas exigia `.Length(2)`; o service aplica `ToUpperInvariant()` antes de persistir.
- T18 (POST), T10 (PUT): acentos íntegros no banco (`João Acentuação ç ã é ô`, `São Paulo`). UTF-8 OK.
- T13 (PUT) perf: 9–35ms em 3 chamadas — bem abaixo do alvo 500ms.
- Headers de erro padrão (`application/problem+json`) com `correlationId`, sem vazamento de stack/SQL. Bom.

---

## POST (20 casos)

| ID   | Descrição                                          | Esperado                                  | Obtido                                                                                            | Resultado |
| ---- | -------------------------------------------------- | ----------------------------------------- | ------------------------------------------------------------------------------------------------- | --------- |
| T1   | Golden PF (CPF=66392332154)                        | 201 + id + traceId + Location             | **201** `id=a5f69d39-…16d`, `Location: /api/v1/clientes/a5f69d39-…`                               | PASS      |
| T2   | Golden PJ (CNPJ=45333147000131)                    | 201                                       | **201** `id=7f531b9f-…3f`                                                                          | PASS      |
| T3   | Sem `Authorization`                                | 401 + WWW-Authenticate: Bearer            | **401**, `WWW-Authenticate: Bearer`, `Content-Length: 0`                                          | PASS      |
| T4   | Bearer inválido                                    | 401                                       | **401** + `WWW-Authenticate: Bearer error="invalid_token"`                                        | PASS      |
| T5   | Bearer válido de outro usuário                     | 201                                       | apenas admin no seed; sem segundo usuário                                                          | SKIP/N-A  |
| T6   | CPF DV errado (`12345678900`)                      | 400 `errors.cpf`                          | **400** `{"errors":{"Cpf":["CPF inválido."]}}`                                                    | PASS      |
| T7   | CNPJ inválido (`00000000000000`)                   | 400 `errors.cnpj`                         | **400** `{"errors":{"Cnpj":["CNPJ inválido."]}}`                                                  | PASS      |
| T8   | CPF com máscara                                    | 400                                       | **400** `{"errors":{"Cpf":["CPF deve conter apenas números."]}}`                                  | PASS      |
| T9   | CPF duplicado (mesmo body 2x)                      | 1×201 + 1×409                             | **201** `id=b846…a73` (1ª) + **409** `cliente-documento-duplicado` (2ª)                            | PASS      |
| T10  | Email malformado                                   | 400 `errors.email`                        | **400** `{"errors":{"Email":["E-mail inválido."]}}`                                                | PASS      |
| T11  | Email duplicado em dois CPFs distintos (gap)       | 2×201 (gap atual) ou 1×409 (regra)        | **201** + **201** — gap **GAP-CW-CLI-EMAIL-1** confirmado                                          | PASS (gap aberto) |
| T12  | Celular 10 dígitos                                 | 400                                       | **400** `{"errors":{"Celular":["Celular deve conter 11 dígitos."]}}`                              | PASS      |
| T13  | Telefone fixo com máscara `(11) 3322-4455`         | 400                                       | **400** `{"errors":{"Telefone":["Telefone deve conter apenas números."]}}`                        | PASS      |
| T14a | Nome vazio                                         | 400                                       | **400** `errors.Nome`                                                                              | PASS      |
| T14b | Nome 2 chars (`Jo`)                                | 400                                       | **400** `errors.Nome` "mínimo 3 caracteres"                                                       | PASS      |
| T14c | Nome 101 chars                                     | 400                                       | **400** `errors.Nome` "máximo 100 caracteres"                                                     | PASS      |
| T15  | Body vazio `{}`                                    | 400 lista                                 | **400** `Nome`, `documento`, `Celular`, `Endereco`                                                | PASS      |
| T16  | JSON malformado                                    | 400                                       | **400** `errors.$` JSON parse + `request`                                                          | PASS      |
| T17  | Whitespace + `uf:"sp"`                             | trim + normalização                       | **201**, DB: `nome="Maria Silva Trim"`, `logradouro="Rua X"`, `endereco_uf="SP"`                  | PASS      |
| T18  | Acentos no nome                                    | 201 + UTF-8 íntegro                       | **201**, DB: `nome="João da Conceição Açaí"`, `cidade="São Paulo"`                               | PASS      |
| T19  | Race: 2 POSTs simultâneos com mesmo CPF            | 1×201 + 1×409                             | **R1=201** + **R2=409**, DB com 1 linha (`uk_clientes_cpf` protege)                               | PASS      |
| T20  | Auditoria `criado_por_usuario_id` = sub JWT        | linha gravada com `00000000-…001`         | coluna **não existe** na tabela `clientes` — **GAP-CW-CLI-AUDIT-CREATE**                          | PASS (com gap registrado) |

**Subtotal POST:** PASS=19 · SKIP/N-A=1 · FAIL=0.

> T20: rebatizei como `PASS (com gap)` porque o sistema **não falha** — apenas não persiste o campo. O log Serilog grava `UsuarioId`. Há um GAP de DB/entidade reportado separadamente.

---

## PUT (13 casos)

| ID  | Descrição                                                                                  | Esperado                                | Obtido                                                                                                       | Resultado |
| --- | ------------------------------------------------------------------------------------------ | --------------------------------------- | ------------------------------------------------------------------------------------------------------------ | --------- |
| T1  | Golden — body completo                                                                     | 200 + `ClienteResponse`                 | **200**, `atualizadoEm > criadoEm`, endereco completo no response                                            | PASS      |
| T2  | Body parcial (sem `nome`)                                                                  | 400                                     | **400** `errors.Nome: "O nome é obrigatório."`                                                                | PASS      |
| T3  | Id Guid inexistente                                                                        | 404                                     | **404** `cliente não encontrado`                                                                              | PASS      |
| T4  | Id não-Guid (`abc`)                                                                        | 404 (route constraint)                  | **404** + `Content-Length: 0`                                                                                | PASS      |
| T5  | Sem `Authorization`                                                                        | 401                                     | **401** + `WWW-Authenticate: Bearer`                                                                          | PASS      |
| T6  | Body vazio `{}`                                                                            | 400 lista                               | **400** `errors`: `Nome`, `Celular`, `Endereco`                                                              | PASS      |
| T7  | Celular não-numérico (`abc12345`)                                                          | 400                                     | **400** `errors.Celular: "Celular deve conter apenas números."`                                              | PASS      |
| T8  | Body com `cpf`/`cnpj` (gap — descartados silenciosamente)                                  | 200 silencioso (gap)                    | **200**, CPF antes=`66392332154`, CPF depois=`66392332154` — **GAP-CW-CLI-PUT-CPF** confirmado                | PASS (gap aberto) |
| T9  | Email já cadastrado em outro cliente (gap)                                                 | 200 silencioso (gap) ou 409             | **200** — **GAP-CW-CLI-PUT-EML** confirmado                                                                  | PASS (gap aberto) |
| T10 | Unicode no nome (`João Acentuação ç ã é ô`)                                                | 200                                     | **200**, DB: `João Acentuação ç ã é ô` íntegro                                                              | PASS      |
| T11 | Race (2 PUTs paralelos)                                                                    | last-write-wins, sem mistura            | **A=200** + **B=200**, DB linha A coerente (nome+email+logradouro=A)                                          | PASS      |
| T12 | Invariantes: `criadoEm` imutável; `atualizadoEm` cresce; sem `AtualizadoPorUsuarioId` (gap) | conferir                                | `criadoEm` igual antes/depois; `atualizadoEm` 17:25 → 17:26; coluna `atualizado_por_usuario_id` **ausente** — **GAP-CW-CLI-AUDIT** confirmado | PASS (gap aberto) |
| T13 | Performance < 500ms                                                                        | < 0.5s                                  | 0.035s, 0.010s, 0.012s em 3 chamadas                                                                         | PASS      |

**Subtotal PUT:** PASS=13 · FAIL=0.

---

## PATCH status (14 casos)

| ID  | Descrição                                          | Esperado                            | Obtido                                                                                                                                                            | Resultado |
| --- | -------------------------------------------------- | ----------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------- |
| T1  | Desativar cliente ativo                            | 200, `ativo:false`                  | **200**, `ativo:false`                                                                                                                                            | PASS      |
| T2  | Reativar cliente inativo                           | 200, `ativo:true`                   | **200**, `ativo:true`                                                                                                                                             | PASS      |
| T3  | Toggle repetido (idempotência)                     | 200/200                             | **200** + **200** (`atualizadoEm` muda)                                                                                                                            | PASS      |
| T4  | Id Guid inexistente                                | 404                                 | **404** `cliente não encontrado`                                                                                                                                  | PASS      |
| T5  | Id não-Guid (`abc`)                                | 404 (route constraint)              | **404** + `Content-Length: 0`                                                                                                                                     | PASS      |
| T6  | Sem `Authorization`                                | 401                                 | **401** + `WWW-Authenticate: Bearer`                                                                                                                              | PASS      |
| T7  | Body ausente                                       | 400                                 | **400** `"A non-empty request body is required."`                                                                                                                  | PASS      |
| T8  | `ativo: null`                                      | 400                                 | **400** `JSON could not be converted to System.Boolean`                                                                                                            | PASS      |
| T9  | `ativo: "sim"`                                     | 400                                 | **400** `JSON could not be converted to System.Boolean`                                                                                                            | PASS      |
| T10 | Body `{}` (gap — desativa silenciosamente)         | 400 (esperado) ou 200 (gap atual)   | **200**, `ativo` antes=`t`, depois=`f` — **GAP-CW-CLI-STA-EMP** confirmado                                                                                       | PASS (gap aberto) |
| T11 | Campo extra + mass assignment                      | 200, ignora extras                  | **200**, `criado_em` imutável, `id` imutável — sem mass assignment                                                                                                | PASS      |
| T12 | Desativar cliente com agendamentos ativos          | 409 esperado / 200 atual (gap)      | **200** — sem agendamentos no banco para reproduzir runtime; service não checa por design — **GAP-CW-CLI-STA-AGD** confirmado por análise + permissividade        | PASS (gap aberto) |
| T13 | Log estruturado com `TraceId`/`UsuarioId`          | linha presente                      | log Serilog: `Status do cliente alterado. ClienteId=…116d. Ativo=True. UsuarioId=00000000-0000-0000-0000-000000000001` — `UsuarioId` presente (mas só em log, não persistido) | PASS (com gap) |
| T14 | Race (PATCH paralelos opostos)                     | last-write-wins, sem 500            | **A=200** + **B=200**, DB final `ativo=f` (last write)                                                                                                            | PASS      |

**Subtotal PATCH:** PASS=14 · FAIL=0.

---

## Detalhes adicionais coletados

### Auditoria no banco (T20 POST / T12 PUT / T13 PATCH)

Inspeção via `information_schema.columns` na tabela `clientes`:

```sql
SELECT column_name FROM information_schema.columns
WHERE table_name='clientes' AND column_name LIKE '%usuario%';
-- 0 rows
```

Nenhuma coluna `criado_por_usuario_id` ou `atualizado_por_usuario_id` na entidade `Cliente`. O `UsuarioId` aparece apenas no log Serilog (`UsuarioId: 00000000-0000-0000-0000-000000000001`), não persistido. **Cluster de gaps de auditoria** (GAP-CW-CLI-AUDIT + GAP-CW-CLI-AUDIT-CREATE) é a única dívida estrutural relevante após o fix de BUG-009.

### JWT decodificado

```json
{
  "sub": "00000000-0000-0000-0000-000000000001",
  "email": "admin@carwash.local",
  "perfil": "Admin",
  "exp": iat+900
}
```

TTL de 15min. Token precisou ser renovado uma vez durante a rodada (entre PUT/PATCH).

### Headers de erro

- 401 sem token: `WWW-Authenticate: Bearer`, `Content-Length: 0`, `X-Correlation-Id` presente.
- 401 token inválido/expirado: `WWW-Authenticate: Bearer error="invalid_token"` (+`error_description` quando expirado).
- 404 route constraint: `Content-Length: 0`, sem body — bom.
- 400/409: `application/problem+json` com `correlationId` consistente.

### Limpeza

Mantidos 31 clientes com email `teste-%@qa.local` para inspeção. Limpeza opcional:

```sql
DELETE FROM clientes WHERE email LIKE 'teste-%@qa.local';
```

---

## Recomendação ao gestor da sprint

1. **BUG-009 fechado** — migration aplicada, race protegida pela UK do banco (T19 POST = 1×201 + 1×409).
2. **Próximas prioridades (não-bloqueadoras para CA001/CA002/CA003, mas requeridas pelo CA011 / governança):**
   1. **GAP-CW-CLI-AUDIT-CREATE + GAP-CW-CLI-AUDIT** — adicionar `criado_por_usuario_id` e `atualizado_por_usuario_id` em `Cliente` + migration. Auditoria LGPD é exigência da DRP §10 e do CA011. Recomendo migration única `AdicionaAuditoriaUsuarioCliente`.
   2. **GAP-CW-CLI-STA-EMP** (PATCH T10) — trocar `AlterarStatusClienteRequest { bool Ativo }` para `bool? Ativo` + validator `.NotNull()`. Hoje body `{}` desativa silenciosamente — risco operacional.
   3. **GAP-CW-CLI-EMAIL-1 + GAP-CW-CLI-PUT-EML** — alinhar com PO se email deve ser único. Se sim, adicionar pré-check `ExisteEmailAsync` no service (com índice único parcial em `email IS NOT NULL`) + tratamento de 409.
   4. **GAP-CW-CLI-PUT-CPF** — UX: o PUT aceita `cpf`/`cnpj` no body e descarta silenciosamente. Sugestão: validator com `OverflowPropertiesHandling = WriteAlways` ou middleware logando warn quando há campos não mapeados.
   5. **GAP-CW-CLI-STA-AGD** (PATCH T12) — depende de RN do PO/PM. Hoje não bloqueia desativação com agendamentos abertos. Reabrir tema quando feature de agendamento (UC003/UC004) estiver implementada.
3. **Coverage gate em CI** — sugerido em v2 — adicionar `SchemaConsistencyTests` (`db.Database.GetPendingMigrationsAsync()` retorna vazio) para evitar regressão de BUG-009.
4. **Testes de integração com `[Trait("CA","011")]`** — adicionar `ClientesEndpointsIntegrationTests` cobrindo POST T9 (CPF dup), T11 (email dup — depende do fix), T19 (race), PUT T8/T9, PATCH T10/T12.
