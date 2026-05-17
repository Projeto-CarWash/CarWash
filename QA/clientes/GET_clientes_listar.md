# GET /api/v1/clientes — Listagem paginada de clientes

## Resumo

- **Método:** `GET`
- **Path:** `/api/v1/clientes`
- **Propósito:** Listar clientes do tenant autenticado com suporte a busca textual, filtro por status (ativo/inativo) e paginação.
- **Autenticação:** Obrigatória (`[Authorize]`). Requer `Authorization: Bearer <JWT>` válido.
- **Produces:** `200 OK` com `application/json` no contrato `ListaClientesResponse`.
- **Controller:** `backend/src/CarWash.Api/Controllers/ClientesController.cs:46` (`Listar`).
- **Query params suportados:**
  - `busca` (string, opcional) — termo livre aplicado em campos textuais do cliente.
  - `ativo` (bool, opcional) — `true` retorna apenas ativos, `false` apenas inativos, ausente retorna ambos.
  - `pagina` (int, default `1`) — número da página (1-based).
  - `tamanhoPagina` (int, default `20`, clamp interno `> 100 → 100`).

## Pré-requisitos

1. Backend de desenvolvimento rodando em `http://localhost:8080`.
2. Variável de ambiente `TOKEN` exportada com JWT válido do tenant em teste:
   ```bash
   export TOKEN="<jwt-do-login>"
   ```
3. Popular o tenant com pelo menos **30 clientes** para validar paginação real (alguns ativos, alguns inativos, nomes com acento, nomes repetidos para validar estabilidade de ordenação). Sugestão: usar `POST /api/v1/clientes` em loop, ou seed dedicado.
4. Conferir que existem pelo menos:
   - 1 cliente com nome contendo `joao` (sem acento).
   - 1 cliente com nome contendo `joão` (com acento).
   - 2 clientes com mesmo nome (ex.: `Silva`) para validar paginação estável.
   - 1 cliente inativo (`ativo = false`).

## Tabela resumo dos casos

| ID  | Cenário                                                         | Query                                                                | Esperado                                | Tipo            |
| --- | --------------------------------------------------------------- | -------------------------------------------------------------------- | --------------------------------------- | --------------- |
| T1  | Golden path sem filtros                                         | _(nenhuma)_                                                          | 200, até 20 itens, `Total=N`            | Happy           |
| T2  | Paginação válida                                                | `?pagina=2&tamanhoPagina=5`                                          | 200, offset correto                     | Happy           |
| T3  | Busca por termo simples                                         | `?busca=joao`                                                        | 200, itens que casam                    | Happy           |
| T4  | Busca vazia                                                     | `?busca=`                                                            | 200, equivalente a sem busca            | Edge            |
| T5  | SQL injection em busca                                          | `?busca=' OR 1=1 --`                                                 | 200, 0 ou poucos resultados, sem erro   | Segurança       |
| T6  | Filtro ativos                                                   | `?ativo=true`                                                        | 200, só ativos                          | Happy           |
| T7  | Filtro inativos                                                 | `?ativo=false`                                                       | 200, só inativos                        | Happy           |
| T8  | `ativo` inválido                                                | `?ativo=xyz`                                                         | 400 binding                             | Validação       |
| T9  | Página zero/negativa                                            | `?pagina=0` / `?pagina=-1`                                           | 200 normalizado (gap: deveria ser 400)  | Gap             |
| T10 | TamanhoPagina inválido                                          | `?tamanhoPagina=0` / `-5` / `10000`                                  | Clamp para 100, mas JSON inconsistente  | Gap             |
| T11 | Página além do total                                            | `?pagina=999999`                                                     | 200, `Itens=[]`, `Total=N`              | Edge            |
| T12 | Sem Authorization                                               | _(sem header)_                                                       | 401                                     | Segurança       |
| T13 | Token expirado                                                  | _(JWT expirado)_                                                     | 401                                     | Segurança       |
| T14 | Combinação completa                                             | `?busca=silva&ativo=true&pagina=1&tamanhoPagina=50`                  | 200                                     | Happy           |
| T15 | Busca com acento sem `unaccent`                                 | `?busca=joão`                                                        | 200, NÃO casa com `joao` (gap)          | Gap             |
| T16 | Performance com volume                                          | `?tamanhoPagina=100` em base com 1000+ clientes                      | < 500ms                                 | Performance     |
| T17 | PII em claro                                                    | _(qualquer chamada)_                                                 | CPF/CNPJ em claro (gap LGPD)            | Segurança/LGPD  |

---

## Detalhamento dos casos

### T1 — Golden path sem filtros

```bash
curl -i -X GET "http://localhost:8080/api/v1/clientes" \
  -H "Authorization: Bearer $TOKEN"
```

Resposta esperada:

```json
{
  "Itens": [
    {
      "Id": "9c2e1a3b-...-...",
      "Nome": "Ana Souza",
      "Email": "ana@exemplo.com",
      "Telefone": "11999990000",
      "Cpf": "12345678901",
      "Cnpj": null,
      "Ativo": true,
      "CriadoEm": "2026-05-01T12:00:00Z"
    }
  ],
  "Total": 30,
  "Pagina": 1,
  "TamanhoPagina": 20
}
```

Logs esperados: `GET /api/v1/clientes 200` no Serilog do backend, sem stack trace.

---

### T2 — Paginação válida

```bash
curl -i -X GET "http://localhost:8080/api/v1/clientes?pagina=2&tamanhoPagina=5" \
  -H "Authorization: Bearer $TOKEN"
```

Resposta esperada (`Itens` deve conter o 6º ao 10º cliente conforme `OrderBy(Nome)`):

```json
{
  "Itens": [ /* 5 itens */ ],
  "Total": 30,
  "Pagina": 2,
  "TamanhoPagina": 5
}
```

Validar:

- `Itens.length == 5`.
- IDs não repetidos com a página 1.
- Total continua refletindo o universo, não a página.

---

### T3 — Busca por termo simples

```bash
curl -i -G "http://localhost:8080/api/v1/clientes" \
  -H "Authorization: Bearer $TOKEN" \
  --data-urlencode "busca=joao"
```

Resposta esperada:

```json
{
  "Itens": [
    { "Nome": "Joao Pereira", "...": "..." }
  ],
  "Total": 1,
  "Pagina": 1,
  "TamanhoPagina": 20
}
```

Validar:

- Todos os `Itens[].Nome` (ou demais campos textuais filtrados) contêm `joao` (case-insensitive conforme implementação).
- `Total` reflete somente o filtro aplicado, não o universo.

---

### T4 — Busca vazia

```bash
curl -i -G "http://localhost:8080/api/v1/clientes" \
  -H "Authorization: Bearer $TOKEN" \
  --data-urlencode "busca="
```

Resposta esperada: equivalente ao T1 (`Total=N`, primeira página). Confirmar que `busca=""` não dispara filtro acidentalmente que zere resultados.

---

### T5 — SQL injection em busca

```bash
curl -i -G "http://localhost:8080/api/v1/clientes" \
  -H "Authorization: Bearer $TOKEN" \
  --data-urlencode "busca=' OR 1=1 --"
```

Resposta esperada: `200` com lista vazia ou poucos resultados (string literal não casa com nenhum cliente). **Nunca** deve retornar a tabela inteira nem 500.

Logs esperados: nenhum stack trace de `Npgsql`. Se aparecer `syntax error at or near`, é regressão grave — Npgsql parametriza por padrão.

---

### T6 — Filtro ativos

```bash
curl -i -X GET "http://localhost:8080/api/v1/clientes?ativo=true" \
  -H "Authorization: Bearer $TOKEN"
```

Resposta esperada: todos `Itens[].Ativo == true`. Comparar `Total` com contagem direta no banco (`SELECT count(*) FROM "Clientes" WHERE "Ativo" = true AND "TenantId" = ...`).

---

### T7 — Filtro inativos

```bash
curl -i -X GET "http://localhost:8080/api/v1/clientes?ativo=false" \
  -H "Authorization: Bearer $TOKEN"
```

Resposta esperada: todos `Itens[].Ativo == false`. Se vier lista vazia em base que tem inativos, **filtro está sendo ignorado** — bug crítico.

---

### T8 — `ativo` inválido

```bash
curl -i -X GET "http://localhost:8080/api/v1/clientes?ativo=xyz" \
  -H "Authorization: Bearer $TOKEN"
```

Resposta esperada: `400 Bad Request` com erro de model binding apontando o parâmetro `ativo`. Se vier `200` ignorando o parâmetro, marcar como gap de validação.

---

### T9 — Página zero/negativa (gap conhecido)

```bash
curl -i -X GET "http://localhost:8080/api/v1/clientes?pagina=0" \
  -H "Authorization: Bearer $TOKEN"

curl -i -X GET "http://localhost:8080/api/v1/clientes?pagina=-1" \
  -H "Authorization: Bearer $TOKEN"
```

Comportamento atual: `200`, repo normaliza silenciosamente para `pagina=1` (mas o JSON pode refletir o valor original em `Pagina`).

**Expectativa correta:** `400 Bad Request` com mensagem clara (`pagina deve ser >= 1`). Documentar como gap a corrigir.

---

### T10 — TamanhoPagina inválido (gap de contrato)

```bash
curl -i -X GET "http://localhost:8080/api/v1/clientes?tamanhoPagina=0" \
  -H "Authorization: Bearer $TOKEN"

curl -i -X GET "http://localhost:8080/api/v1/clientes?tamanhoPagina=-5" \
  -H "Authorization: Bearer $TOKEN"

curl -i -X GET "http://localhost:8080/api/v1/clientes?tamanhoPagina=10000" \
  -H "Authorization: Bearer $TOKEN"
```

Comportamento atual:

- Repo clampa internamente `> 100 → 100`.
- **JSON de resposta retorna `TamanhoPagina=10000`** (valor original do request), mas `Itens.length` será no máximo `100`. Inconsistência de contrato.

Validar:

- `Itens.length <= 100` mesmo com `?tamanhoPagina=10000`.
- Conferir o valor de `TamanhoPagina` no JSON — se for o original, registrar bug.

**Expectativa correta:** ou retornar `400` para fora da faixa `[1..100]`, ou refletir o valor clampado no JSON.

---

### T11 — Página além do total

```bash
curl -i -X GET "http://localhost:8080/api/v1/clientes?pagina=999999" \
  -H "Authorization: Bearer $TOKEN"
```

Resposta esperada:

```json
{
  "Itens": [],
  "Total": 30,
  "Pagina": 999999,
  "TamanhoPagina": 20
}
```

Validar: `Total` continua correto (universo), não zero. `Itens` vazio.

---

### T12 — Sem Authorization

```bash
curl -i -X GET "http://localhost:8080/api/v1/clientes"
```

Resposta esperada: `401 Unauthorized`. Body pode estar vazio ou com `ProblemDetails`. Nenhum dado de cliente deve vazar.

---

### T13 — Token expirado

```bash
curl -i -X GET "http://localhost:8080/api/v1/clientes" \
  -H "Authorization: Bearer <jwt-expirado>"
```

Resposta esperada: `401 Unauthorized`. Header `WWW-Authenticate` indicando erro (`invalid_token`, `token expired`).

---

### T14 — Combinação completa de filtros

```bash
curl -i -G "http://localhost:8080/api/v1/clientes" \
  -H "Authorization: Bearer $TOKEN" \
  --data-urlencode "busca=silva" \
  --data-urlencode "ativo=true" \
  --data-urlencode "pagina=1" \
  --data-urlencode "tamanhoPagina=50"
```

Resposta esperada: `200` com `Itens` que satisfaçam **todos** os filtros (`Nome` contém `silva` AND `Ativo == true`). Validar `Total` confere com `SELECT count(*)` equivalente.

---

### T15 — Busca com acento (gap unaccent)

```bash
curl -i -G "http://localhost:8080/api/v1/clientes" \
  -H "Authorization: Bearer $TOKEN" \
  --data-urlencode "busca=joão"
```

Comportamento atual: `200`, mas **NÃO casa** com clientes cujo nome está armazenado como `joao` (sem acento), e vice-versa. Service não aplica `unaccent` no Postgres nem normalização Unicode antes da comparação.

**Expectativa correta:** busca insensível a acento. Marcar como gap funcional.

---

### T16 — Performance com volume

Pré-requisito: popular base com pelo menos 1000 clientes.

```bash
time curl -s -o /dev/null -w "%{http_code} %{time_total}\n" \
  -X GET "http://localhost:8080/api/v1/clientes?tamanhoPagina=100" \
  -H "Authorization: Bearer $TOKEN"
```

Critério: `time_total < 0.5s` no ambiente local. Se exceder, abrir alerta de performance e revisar índices em `Nome` e `Ativo`.

Repetir 5x e considerar mediana.

---

### T17 — PII em claro (gap LGPD)

```bash
curl -s -X GET "http://localhost:8080/api/v1/clientes" \
  -H "Authorization: Bearer $TOKEN" | jq '.Itens[0] | {Cpf, Cnpj}'
```

Comportamento atual: `Cpf` e `Cnpj` retornam em claro (ex.: `"Cpf": "12345678901"`).

**Expectativa correta para uma listagem geral:**

- Mascarar (`"Cpf": "***.456.789-**"`).
- Ou expor somente quando o chamador tem permissão explícita (claim `clientes:ver-pii`).
- Ou retornar `null` na listagem e expor apenas no `GET /api/v1/clientes/{id}` com autorização adicional.

Documentar como gap LGPD bloqueante para release de produção.

---

## Bugs e crashes a observar

- **500 com `?tamanhoPagina=10000`:** se chegar a alocar lista de 10k em memória antes do clamp, pode estourar. Esperado: clamp acontece **antes** do `Take`.
- **Inconsistência de contrato:** `TamanhoPagina` no JSON reflete o valor do request, não o efetivamente aplicado. `Itens.length` pode ser menor que `TamanhoPagina` mesmo quando há registros suficientes — confunde o consumidor e quebra cálculo de páginas no front.
- **Filtro `ativo` ignorado:** chamada com `?ativo=false` retornando ativos também = bug crítico.
- **SQL injection em `busca`:** Npgsql parametriza, mas confirmar que não há string concat oculto. Qualquer 500 com `syntax error` em `busca` é regressão grave.
- **Total errado com filtros:** `Total` deve refletir o universo após filtros, não o universo bruto da tabela. Se vier o total bruto, paginação no front quebra.
- **Paginação instável:** `OrderBy(Nome)` sem `ThenBy(Id)` faz com que clientes com mesmo `Nome` apareçam em ordem indeterminada entre chamadas — registro pode "sumir" ou "duplicar" entre páginas. Validar T2 + T14 rodando 3x e comparando IDs.
- **PII em claro:** `Cpf`/`Cnpj` na listagem é gap LGPD. Documentar e priorizar.
- **Vazamento de senha:** clientes não têm senha no modelo, mas vale conferir que nenhum campo sensível adicional (token, hash, dado interno) vaze em `Itens`. Inspecionar JSON bruto com `jq 'keys'` no primeiro item.
- **Cross-tenant leak:** rodar T1 com tokens de dois tenants distintos e confirmar que `Itens` é disjunto e `Total` difere conforme cada tenant.

---

## Como reportar para o dev

Para cada bug encontrado, abrir issue (ou ticket no board) contendo:

1. **ID do caso** desta suíte (ex.: `GET-clientes-listar / T10`).
2. **Severidade sugerida:**
   - `crítico` — vazamento de PII, cross-tenant, SQL injection, 500.
   - `alto` — filtro ignorado, total errado, paginação instável.
   - `médio` — inconsistência de contrato (T9, T10), gap unaccent (T15).
   - `baixo` — mensagens de erro pouco descritivas.
3. **Passos para reproduzir:** comando `curl` exato usado, incluindo `TOKEN` redigido.
4. **Resposta obtida:** status, headers relevantes (`WWW-Authenticate`, `Content-Type`), JSON completo (truncado se > 50 linhas).
5. **Resposta esperada:** baseada nesta documentação.
6. **Evidência adicional:** trecho do log do backend (Serilog) com timestamp correspondente.
7. **Sugestão de correção (quando aplicável):**
   - T9/T10 → adicionar validação no DTO ou refletir clamp no JSON.
   - T15 → habilitar extensão `unaccent` no Postgres e aplicar na query.
   - T17 → introduzir DTO de listagem sem PII completa.
   - Paginação instável → adicionar `ThenBy(c => c.Id)` ao `OrderBy`.
8. **Referência ao controller:** `backend/src/CarWash.Api/Controllers/ClientesController.cs:46`.
9. **Rótulo:** `qa`, `endpoint:clientes`, `metodo:GET`, e tag específica (`lgpd`, `seguranca`, `contrato`, `performance` conforme o caso).

Antes de fechar o ticket, exigir teste automatizado de regressão correspondente em `backend/tests/` (xUnit + WebApplicationFactory + Testcontainers) cobrindo o caso reportado.
