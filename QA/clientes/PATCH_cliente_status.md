# PATCH /api/v1/clientes/{id:guid}/status

## Resumo

- **Metodo:** `PATCH`
- **Path:** `/api/v1/clientes/{id:guid}/status`
- **Proposito:** alternar o flag `ativo` do cliente (ativar/desativar) sem hard delete.
- **Autenticacao:** obrigatoria (`[Authorize]`). Sem `Authorization` retorna 401.
- **Produces:** `200 OK` (ClienteResponse atualizado), `400 Bad Request` (body invalido), `404 Not Found` (cliente inexistente).
- **Controller:** `backend/src/CarWash.Api/Controllers/ClientesController.cs:86` (`AlterarStatus`).
- **DTO:** `AlterarStatusClienteRequest { bool Ativo }` em `backend/src/CarWash.Application/DTOs/Clientes/CreateClienteRequest.cs:54`.
- **Service:** `ClienteService.AlterarStatusAsync` (linha ~133).

## Pre-requisitos

- Backend rodando em `http://localhost:8080`.
- Token JWT valido exportado:

```bash
export TOKEN="eyJhbGciOi..."
```

- Cliente existente, com id exportado:

```bash
export CLIENTE_ID="11111111-1111-1111-1111-111111111111"
```

- Para T12, um agendamento futuro/ativo vinculado a `$CLIENTE_ID`.

## Resumo dos casos

| #   | Cenario                                                | Body                              | Esperado | Bug? |
| --- | ------------------------------------------------------ | --------------------------------- | -------- | ---- |
| T1  | Desativar cliente ativo                                | `{"ativo": false}`                | 200      | nao  |
| T2  | Reativar cliente inativo                               | `{"ativo": true}`                 | 200      | nao  |
| T3  | Toggle repetido (idempotencia)                         | `{"ativo": false}` x2             | 200      | nao  |
| T4  | Id Guid inexistente                                    | `{"ativo": false}`                | 404      | nao  |
| T5  | Id nao-Guid (route constraint)                         | `{"ativo": false}`                | 404      | nao  |
| T6  | Sem `Authorization`                                    | `{"ativo": false}`                | 401      | sim se != 401 |
| T7  | Body ausente (sem `--data`)                            | (vazio)                           | 400      | sim se 500    |
| T8  | `ativo: null`                                          | `{"ativo": null}`                 | 400      | sim se 200    |
| T9  | `ativo: "sim"` (string em bool)                        | `{"ativo": "sim"}`                | 400      | sim se 200    |
| T10 | Body `{}`                                              | `{}`                              | 400 (atual: 200) | sim    |
| T11 | Campo extra                                            | `{"ativo": true, "foo": "bar"}`   | 200      | verificar mass assignment |
| T12 | Desativar cliente com agendamentos ativos              | `{"ativo": false}`                | 409 esperado (atual: 200) | sim |
| T13 | Inspecionar log com `TraceId`/`UsuarioId`              | `{"ativo": false}`                | log valido        | sim se ausente |
| T14 | Race condition (PATCH paralelos opostos)               | `{"ativo": true/false}` em paralelo | sem 409 (last-write-wins) | sim se 500 |

## Detalhamento

### T1 — Golden desativar

```bash
curl -i -X PATCH "http://localhost:8080/api/v1/clientes/$CLIENTE_ID/status" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  --data '{"ativo": false}'
```

```json
{ "ativo": false }
```

**Resposta esperada:** `200 OK` com `ClienteResponse` contendo `"ativo": false` e demais campos preservados.

**Log esperado:** `Status do cliente alterado. ClienteId=<id>, Ativo=False, UsuarioId=<guid-usuario>` e header/log com `TraceId`.

**Sinais de bug:** 500 (exception nao tratada), 200 com `ativo: true`, ausencia de `UsuarioId` no log.

### T2 — Reativar

```bash
curl -i -X PATCH "http://localhost:8080/api/v1/clientes/$CLIENTE_ID/status" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  --data '{"ativo": true}'
```

```json
{ "ativo": true }
```

**Resposta esperada:** `200 OK`, `"ativo": true`.

**Sinais de bug:** persistencia nao reflete em `GET /api/v1/clientes/{id}`; cliente continua marcado como inativo.

### T3 — Toggle repetido (idempotencia)

```bash
curl -s -X PATCH "http://localhost:8080/api/v1/clientes/$CLIENTE_ID/status" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  --data '{"ativo": false}'
curl -i -X PATCH "http://localhost:8080/api/v1/clientes/$CLIENTE_ID/status" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  --data '{"ativo": false}'
```

```json
{ "ativo": false }
```

**Resposta esperada:** ambas `200 OK`, sem efeito colateral. Idealmente o log da segunda chamada deve sinalizar "sem alteracao" — verificar se ha ruido.

**Sinais de bug:** 409 ou 500 na segunda chamada; campo `atualizadoEm` atualizando mesmo sem mudanca de valor.

### T4 — Id Guid inexistente

```bash
curl -i -X PATCH "http://localhost:8080/api/v1/clientes/00000000-0000-0000-0000-000000000000/status" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  --data '{"ativo": false}'
```

**Resposta esperada:** `404 Not Found` com `ProblemDetails` contendo `traceId`.

**Sinais de bug:** 500, 200 com objeto vazio, ausencia de `traceId`.

### T5 — Id nao-Guid (route constraint)

```bash
curl -i -X PATCH "http://localhost:8080/api/v1/clientes/abc/status" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  --data '{"ativo": false}'
```

**Resposta esperada:** `404 Not Found` (route constraint `:guid` nao casa) — a request nem chega no handler.

**Sinais de bug:** 400 vazando detalhe interno; 500.

### T6 — Sem Authorization

```bash
curl -i -X PATCH "http://localhost:8080/api/v1/clientes/$CLIENTE_ID/status" \
  -H "Content-Type: application/json" \
  --data '{"ativo": false}'
```

**Resposta esperada:** `401 Unauthorized`, sem corpo sensivel, com `WWW-Authenticate`.

**Sinais de bug:** 200 (status alterado sem autenticacao — CRITICO); 500.

### T7 — Body ausente (sem --data)

```bash
curl -i -X PATCH "http://localhost:8080/api/v1/clientes/$CLIENTE_ID/status" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json"
```

**Resposta esperada:** `400 Bad Request`. O controller usa `ArgumentNullException.ThrowIfNull(request)`; o middleware global deve mapear `ArgumentNullException` para `400` com `ProblemDetails` (`title: "Bad Request"`, `traceId`).

**Sinais de bug:** `500 Internal Server Error` com stack trace vazando (mapeamento ausente para `ArgumentNullException`).

### T8 — ativo: null

```bash
curl -i -X PATCH "http://localhost:8080/api/v1/clientes/$CLIENTE_ID/status" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  --data '{"ativo": null}'
```

**Resposta esperada:** `400 Bad Request` — `bool` (nao anulavel) nao aceita `null` na desserializacao do `System.Text.Json`. Mensagem deve apontar para `ativo`.

**Sinais de bug:** 200 com `ativo: false` (fallback silencioso), 500.

### T9 — ativo: "sim" (string em bool)

```bash
curl -i -X PATCH "http://localhost:8080/api/v1/clientes/$CLIENTE_ID/status" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  --data '{"ativo": "sim"}'
```

**Resposta esperada:** `400 Bad Request` com erro de conversao do tipo.

**Sinais de bug:** 200 (coercao surpresa); 500.

### T10 — Body {} (bug de validacao conhecido)

```bash
curl -i -X PATCH "http://localhost:8080/api/v1/clientes/$CLIENTE_ID/status" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  --data '{}'
```

**Resposta atual:** `200 OK`, `ativo: false` (valor default do `bool`).

**Resposta esperada:** `400 Bad Request` indicando que `ativo` e obrigatorio.

**Marcar como bug:** falta validacao (FluentValidation ou `[Required]` em wrapper). Desativacao silenciosa pode quebrar operacao se PATCH for chamado por mistake (botao com payload errado no front).

### T11 — Campo extra

```bash
curl -i -X PATCH "http://localhost:8080/api/v1/clientes/$CLIENTE_ID/status" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  --data '{"ativo": true, "foo": "bar"}'
```

**Resposta esperada:** `200 OK`, ignorando `foo`. Comportamento padrao do System.Text.Json e ignorar membros desconhecidos.

**Validar (mass assignment):** alterar payload para incluir campos sensiveis e confirmar que NAO sao persistidos:

```bash
curl -i -X PATCH "http://localhost:8080/api/v1/clientes/$CLIENTE_ID/status" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  --data '{"ativo": true, "id": "99999999-9999-9999-9999-999999999999", "criadoEm": "1970-01-01T00:00:00Z", "ativo2": false}'
```

Depois confirmar:

```bash
curl -s -H "Authorization: Bearer $TOKEN" "http://localhost:8080/api/v1/clientes/$CLIENTE_ID" | jq '{id, criadoEm, ativo}'
```

**Sinais de bug:** `id` ou `criadoEm` alterados — mass assignment ativo no DTO. CRITICO.

### T12 — Desativar cliente com agendamentos ativos (RN ausente — exploratorio)

Cenario: cliente possui >=1 agendamento futuro com status diferente de `Cancelado`/`Concluido`.

```bash
curl -i -X PATCH "http://localhost:8080/api/v1/clientes/$CLIENTE_ID/status" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  --data '{"ativo": false}'
```

**Comportamento atual:** `200 OK` — service NAO valida agendamentos abertos antes de desativar.

**Comportamento esperado (proposta de RN):** `409 Conflict` com `ProblemDetails` apontando os agendamentos abertos, OU no minimo um warning no log e um campo `agendamentosAbertos` na resposta.

**Reportar como:** regra de negocio ausente. Discutir com PO/PM se o produto exige bloqueio ou apenas alerta. Sem essa regra, e possivel "perder" cliente do filtro `ativo=true` mantendo agenda ja marcada para ele, gerando inconsistencia operacional.

### T13 — Verificar log com TraceId/UsuarioId

Apos T1, inspecionar a saida do backend ou o sink do Serilog:

```bash
docker logs carwash-api --since 2m | grep "Status do cliente alterado"
```

**Esperado:** linha contendo `ClienteId=<guid>`, `Ativo=False`, `UsuarioId=<guid>` e correlacao com `TraceId` da request (mesmo `traceId` retornado no header `traceparent` ou em `ProblemDetails` de erros).

**Sinais de bug:**

- `UsuarioId=` vazio ou `00000000-...` — claim `sub`/`nameidentifier` nao lido corretamente.
- Nenhum log emitido (operacao de mutacao sem auditoria — CRITICO para LGPD/governanca).
- `TraceId` ausente, dificultando correlacao com APM.

### T14 — Race: 2 PATCH paralelos com valores opostos

```bash
printf '%s\n' true false | xargs -P2 -I{} curl -s -o /dev/null -w "%{http_code}\n" \
  -X PATCH "http://localhost:8080/api/v1/clientes/$CLIENTE_ID/status" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  --data '{"ativo": {}}'
```

Em seguida:

```bash
curl -s -H "Authorization: Bearer $TOKEN" "http://localhost:8080/api/v1/clientes/$CLIENTE_ID" | jq '.ativo'
```

**Resposta esperada:** ambas `200 OK`, estado final coincide com o ultimo write (last-write-wins). Sem controle de concorrencia otimista nesta rota — aceitavel para toggle, desde que documentado.

**Sinais de bug:** `500` por `DbUpdateConcurrencyException` nao tratada; resposta `200` mas banco em estado inconsistente (ex.: `ativo=true` no banco e `ativo=false` na resposta); deadlock no Postgres.

## Bugs e crashes a observar

- **500 em body null (T7):** se o middleware nao mapear `ArgumentNullException` para `400`, vaza stack trace e quebra contrato. Critico.
- **Status alterado sem `Authorization` (T6):** falha de seguranca grave — verificar se o `[Authorize]` do controller esta efetivo (pode estar com pipeline sem `UseAuthorization`).
- **Audit log ausente (T13):** mutacao sem `UsuarioId`/`TraceId` no Serilog quebra rastreabilidade e governanca multiunidade.
- **GET vs PATCH inconsistente:** cliente desativado ainda aparece em `GET /api/v1/clientes?ativo=true`. Sintoma de cache, query errada ou commit nao propagado.
- **Mass assignment (T11):** campos como `id`, `criadoEm`, `nome`, `cpf` sendo aceitos no DTO de status e sobrescrevendo entidade. Critico — pode permitir hijack do cliente via PATCH de status.
- **Body `{}` desativando silenciosamente (T10):** ausencia de validacao explicita do campo `ativo` permite operacao perigosa por payload acidental.
- **Sem RN para agendamentos abertos (T12):** desativacao quebra operacao sem aviso; conflito com escopo multiunidade.
- **Race (T14):** se aparecer `DbUpdateConcurrencyException` 500, falta tratamento; se houver divergencia entre resposta e banco, transacao mal gerenciada.

## Como reportar para o dev

Ao abrir issue/PR de bug encontrado nesta rota, incluir:

1. **Titulo:** `[bug][clientes][PATCH /status] <sintoma curto>`.
2. **Caso reproduzido:** numero do T (ex.: T7) e payload exato (`curl` completo, com header e body).
3. **Esperado x obtido:** status HTTP, corpo, log esperado vs o que aconteceu.
4. **Evidencia:**
   - Response headers e body (cole bruto).
   - Linha do log do backend (Serilog) com `TraceId`.
   - Estado do banco antes/depois (`SELECT id, ativo, atualizado_em FROM clientes WHERE id = ...`).
5. **Impacto:** critico/alto/medio/baixo. Sinalizar se ha risco de seguranca (T6, T11) ou perda de auditoria (T13).
6. **Sugestao de correcao:** referencia ao arquivo/linha (`backend/src/CarWash.Api/Controllers/ClientesController.cs:86`, `ClienteService.AlterarStatusAsync` ~linha 133, DTO em `CreateClienteRequest.cs:54`).
7. **Teste sugerido:** caso xUnit/integration a adicionar (`[Trait("CA","011")]` quando aplicavel a CA006-CA010).
8. **Owner:** atribuir a `dev-dotnet-carwash`; envolver `arquiteto-carwash` em mudancas de contrato (T10/T11) ou `po-pm-carwash` para regra ausente (T12).
