# GET /api/v1/clientes/{id:guid} — Obter cliente por id

## Resumo

- **Método:** `GET`
- **Path:** `/api/v1/clientes/{id:guid}`
- **Propósito:** retornar os dados completos de um cliente pelo identificador.
- **Autenticação:** obrigatória (`[Authorize]`). Cookie JWT ou `Authorization: Bearer <token>`.
- **Produces:** `200 OK` (`ClienteResponse`) ou `404 Not Found` (sem body).
- **Controller:** `backend/src/CarWash.Api/Controllers/ClientesController.cs:59` (`ObterPorId`).
- **Service:** `ClienteService.ObterPorIdAsync` — retorna `null` quando não encontrado, o que faz o controller responder `NotFound()` puro (sem ProblemDetails).

> Pegadinha conhecida: a route constraint `{id:guid}` rejeita ids malformados (ex.: `abc`) **no roteamento**, retornando `404` antes mesmo de entrar no controller. Não confunda com `400 Bad Request`.

> Risco LGPD: o endpoint expõe **PII em claro** (CPF, CNPJ, e-mail, celular, telefone, endereço completo). Não há filtro por tenant ou por dono — qualquer usuário autenticado lê qualquer cliente.

---

## Pré-requisitos

1. Backend de desenvolvimento ativo em `http://localhost:8080`.
2. Variável `TOKEN` com JWT válido exportada no shell:

```bash
export TOKEN="<jwt-valido>"
```

3. Criar um cliente para obter um `id` real (via `POST /api/v1/clientes`) e exportar:

```bash
export CLIENTE_ID="<guid-retornado-pelo-post>"
```

4. Opcional: segundo usuário autenticado (`TOKEN_USER_B`) para validar ausência de isolamento de tenant.

---

## Resumo dos casos

| ID  | Cenário                                          | Entrada                                  | Esperado                                  |
| --- | ------------------------------------------------ | ---------------------------------------- | ----------------------------------------- |
| T1  | Golden path                                      | id existente válido                      | 200 + `ClienteResponse` completo          |
| T2  | Guid válido inexistente                          | Guid aleatório                           | 404 sem body                              |
| T3  | Id não-Guid                                      | `abc`                                    | 404 (route constraint, não chega no ctrl) |
| T4  | Guid zero                                        | `00000000-0000-0000-0000-000000000000`   | 404 sem body                              |
| T5  | Guid em maiúsculas                               | id em uppercase                          | 200 (Guid é case-insensitive)             |
| T6  | Sem `Authorization`                              | header ausente                           | 401                                       |
| T7  | Token expirado                                   | JWT vencido                              | 401                                       |
| T8  | Token de outro usuário                           | id criado por user A, lido por user B    | 200 (sem filtro de tenant)                |
| T9  | Header `Cache-Control` na resposta               | qualquer GET válido                      | `no-store` ou `private` (PII)             |
| T10 | Exposição de PII                                 | qualquer GET válido                      | CPF/CNPJ em claro no body                 |
| T11 | Performance                                      | qualquer GET válido                      | latência < 300ms                          |

---

## Detalhamento dos casos

### T1 — Golden path (200)

```bash
curl -X GET "http://localhost:8080/api/v1/clientes/$CLIENTE_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -i
```

Resposta esperada (`200 OK`):

```json
{
  "id": "f3a1c2b4-1111-2222-3333-444455556666",
  "nome": "Maria Silva",
  "dataNascimento": "1990-05-12",
  "cpf": "12345678901",
  "cnpj": null,
  "celular": "11999990000",
  "telefone": "1133334444",
  "email": "maria@example.com",
  "endereco": {
    "cep": "01001000",
    "logradouro": "Praça da Sé",
    "numero": "100",
    "complemento": "Apto 12",
    "bairro": "Sé",
    "cidade": "São Paulo",
    "uf": "SP"
  },
  "ativo": true,
  "criadoEm": "2026-05-17T13:00:00Z",
  "atualizadoEm": "2026-05-17T13:00:00Z"
}
```

Logs esperados: `GET /api/v1/clientes/{id} responded 200`. Sem stack trace.

---

### T2 — Guid válido inexistente (404)

```bash
curl -X GET "http://localhost:8080/api/v1/clientes/11111111-2222-3333-4444-555555555555" \
  -H "Authorization: Bearer $TOKEN" \
  -i
```

Resposta esperada: `404 Not Found`, **sem body** (controller usa `NotFound()` sem ProblemDetails).

Logs esperados: `ObterPorIdAsync retornou null`. Nenhum erro.

> Inconsistência conhecida: outros endpoints retornam `ProblemDetails` no 404. Reportar.

---

### T3 — Id não-Guid (`abc`) → 404 pelo route constraint

```bash
curl -X GET "http://localhost:8080/api/v1/clientes/abc" \
  -H "Authorization: Bearer $TOKEN" \
  -i
```

Resposta esperada: `404 Not Found` **do roteamento** (a rota nem casa porque `{id:guid}` falha). Não é `400`.

Logs esperados: nenhum log do controller; possivelmente log do middleware de roteamento.

> Pegadinha de QA: muita gente espera `400 Bad Request` aqui. Documentar no contrato e alinhar com front que `404` significa tanto "id malformado" quanto "id válido inexistente".

---

### T4 — Guid zero (404)

```bash
curl -X GET "http://localhost:8080/api/v1/clientes/00000000-0000-0000-0000-000000000000" \
  -H "Authorization: Bearer $TOKEN" \
  -i
```

Resposta esperada: `404 Not Found`, sem body.

> Bug a observar: se o service não tratar `Guid.Empty` e tentar consultar, EF Core pode lançar exceção e gerar `500`. Esperado: 404 limpo.

Logs esperados: nenhuma exceção. Se aparecer `ArgumentException` ou `Npgsql` exception, reportar como bug.

---

### T5 — Guid em maiúsculas (200)

```bash
CLIENTE_ID_UPPER=$(echo "$CLIENTE_ID" | tr '[:lower:]' '[:upper:]')
curl -X GET "http://localhost:8080/api/v1/clientes/$CLIENTE_ID_UPPER" \
  -H "Authorization: Bearer $TOKEN" \
  -i
```

Resposta esperada: `200 OK` com mesmo body do T1 (Guid é case-insensitive).

Logs esperados: idênticos ao T1.

---

### T6 — Sem `Authorization` (401)

```bash
curl -X GET "http://localhost:8080/api/v1/clientes/$CLIENTE_ID" \
  -i
```

Resposta esperada: `401 Unauthorized`. Body pode ser vazio ou `WWW-Authenticate: Bearer`.

Logs esperados: log de autenticação falhando, sem stack trace.

---

### T7 — Token expirado (401)

```bash
curl -X GET "http://localhost:8080/api/v1/clientes/$CLIENTE_ID" \
  -H "Authorization: Bearer <jwt-expirado>" \
  -i
```

Resposta esperada: `401 Unauthorized`, header `WWW-Authenticate: Bearer error="invalid_token", error_description="The token expired at ..."`.

Logs esperados: `Bearer was not authenticated. Failure message: IDX10223: Lifetime validation failed`. Esperado.

---

### T8 — Token de outro usuário (200, sem filtro de tenant)

Pré-requisito: cliente criado por `USER_A`, token de `USER_B`.

```bash
curl -X GET "http://localhost:8080/api/v1/clientes/$CLIENTE_ID" \
  -H "Authorization: Bearer $TOKEN_USER_B" \
  -i
```

Resposta esperada **atual**: `200 OK` com o body completo do cliente de outro usuário.

> Risco de segurança/LGPD: não existe filtro de tenant nem de dono. Qualquer usuário autenticado consegue ler qualquer cliente do sistema. Reportar como bug de segurança e bloqueador para multiunidade (RN multiunidade e §5 do DVS).

Logs esperados: `200`, sem warning de autorização — porque não há checagem de propriedade.

---

### T9 — Header `Cache-Control` na resposta

```bash
curl -X GET "http://localhost:8080/api/v1/clientes/$CLIENTE_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -I
```

Cabeçalhos esperados (mínimo aceitável para PII):

```
Cache-Control: no-store
```

ou

```
Cache-Control: private, no-cache, no-store, max-age=0
```

> Bug a observar: se vier `Cache-Control: public` ou ausente, proxies e CDNs podem cachear PII. Reportar como bug crítico de privacidade.

---

### T10 — Verificar PII exposta em claro

Reexecutar T1 e inspecionar o body. Confirmar a presença, em texto claro, de:

- `cpf`
- `cnpj` (se preenchido)
- `email`
- `celular`, `telefone`
- `endereco.cep`, `logradouro`, `numero`, `complemento`, `bairro`, `cidade`, `uf`
- `dataNascimento`

> Observação LGPD: campos não são mascarados nem redatados. Avaliar com PO/PM e arquiteto se o nível atual de exposição é aceitável no escopo do MVP, e registrar decisão. Logar minimização (não retornar campos que o front não consome) é mitigação possível.

---

### T11 — Performance (< 300ms)

```bash
curl -X GET "http://localhost:8080/api/v1/clientes/$CLIENTE_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -o /dev/null -s -w "tempo_total=%{time_total}s status=%{http_code}\n"
```

Resposta esperada: `status=200` e `tempo_total < 0.3s` em ambiente local com banco quente.

> Bug a observar: se passar de 1s consistentemente, investigar query N+1 no `Include` do endereço e índice em `clientes(id)` (PK já indexada — não deveria ocorrer).

---

## Bugs e crashes a observar

- **500 em vez de 404 com `Guid.Empty`** — service deve tratar `Guid.Empty` como id inválido e o controller responder 404, nunca 500.
- **Confusão 404 vs 400 com id malformado** — route constraint `{id:guid}` faz `abc` retornar `404` antes do controller. Documentar e alinhar com front.
- **Vazamento entre tenants/usuários** — ausência de filtro permite que qualquer autenticado leia qualquer cliente. Bloqueador para multiunidade.
- **`Cache-Control` permissivo** — qualquer valor diferente de `no-store`/`private` permite cache em proxy/CDN. Risco crítico de PII.
- **200 com body `null` em vez de 404** — se controller responder `Ok(null)` em algum caminho, o front quebra com `cliente.nome` undefined. Esperado: `NotFound()` puro.
- **Ausência de `ProblemDetails` no 404** — divergente do padrão dos outros endpoints (POST, PUT, DELETE). Padronizar.
- **Stack trace exposto no 500** — em produção, 500 nunca deve retornar stack trace; verificar `UseExceptionHandler` e ambiente.

---

## Como reportar para o dev

Ao abrir issue/bug, informar obrigatoriamente:

1. **Caso afetado:** ID do caso (T1–T11) e cenário descrito acima.
2. **Request:** método, URL completa, headers relevantes (sem expor token real — use `Bearer <redacted>`) e body se houver.
3. **Resposta obtida:** status HTTP, headers (especialmente `Cache-Control`, `WWW-Authenticate`), body completo.
4. **Resposta esperada:** conforme tabela e detalhamento.
5. **Logs do backend:** stack trace, correlation id, timestamp.
6. **Ambiente:** branch, commit SHA, banco usado (Testcontainers/local), data/hora.
7. **Reprodutibilidade:** quantas tentativas em quantas execuções (`n/10`).
8. **Severidade sugerida:** crítica (vazamento de PII, 500), alta (status errado), média (mensagem ruim), baixa (cosmético/inconsistência de contrato).
9. **Referência:** linha do controller (`backend/src/CarWash.Api/Controllers/ClientesController.cs:59`) e do service quando aplicável.
10. **Reproduzir com:** o `curl` exato do caso, para o dev rodar localmente.

Encaminhar para o `dev-dotnet-carwash` com cópia ao `arquiteto-carwash` quando o achado for de segurança/LGPD ou multiunidade.
