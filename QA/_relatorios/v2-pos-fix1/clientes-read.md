# Relatório — Clientes Read (REBATERIA pós-fix)

Data: 2026-05-17T15:07:00Z
Rodada anterior em: ../v1-pre-fix/clientes-read.md
Bugs fechados desde v1: **BUG-CR001 (login admin agora 200 + JWT válido)**.
Migrations pendentes detectadas:
  - `20260517061810_RefatoraClienteEndereco` (presente em `backend/src/CarWash.Infrastructure/Persistence/Migrations/` mas **não aplicada** — `__ef_migrations_history` lista somente `20260513114525_InitialSchema` e `20260517022432_AddUsuarioLockoutFields`).
  - Consequência confirmada: tabela `clientes` ainda no schema flat (endereço como `varchar(255)`, sem `data_nascimento`, sem `endereco_*`), enquanto a entidade/DTO do código já espera as novas colunas (`data_nascimento`, `endereco_cep`, `endereco_logradouro`, `endereco_numero`, `endereco_complemento`, `endereco_bairro`, `endereco_cidade`, `endereco_uf`).

Evidência (`\d clientes`):
```
nome, cpf, cnpj, telefone, celular, email, endereco (varchar 255), observacoes, ativo, criado_em, atualizado_em
```

Evidência do erro EF:
```
Npgsql.PostgresException 42703: column c.data_nascimento does not exist (POSITION 79)
```

## Comparativo v1 vs v2

|         | v1 | v2 |
|---------|---:|---:|
| PASS    |  6 |  6 |
| FAIL    |  0 |  2 |
| BLOCKED | 22 | 20 |

Observações:
- v1: 22 BLOCKED concentrados em "sem token" (BUG-CR001/login 500).
- v2: BUG-CR001 fechado, mas **BUG-009** (migration `RefatoraClienteEndereco` não aplicada) transferiu o bloqueio para o banco. Total de 500 em qualquer caminho que toque `clientes` no banco.
- Casos PASS de v1 foram reconfirmados (4) e somam +2 novos (T2 byid e T4 byid agora produzem evidência adicional, mas **falham**) — registrados como FAIL.

## Sumário

- Total: 28 | PASS: 6 | FAIL: 2 | BLOCKED: 20
- Bugs ainda abertos (herdados, não revalidáveis sem dados):
  - **BUG-CR002** — `[Authorize]` antes do binding (era baixa, agora dá pra confirmar T8 listar com token → 400 binding OK; pode marcar BUG-CR002 como **fechado** — ver tabela).
  - **BUG-LGPD-CLI** — PII em claro (T17 listar / T10 byid): não revalidável sem dados.
  - **BUG-TENANT-CLI** — ausência de filtro de tenant (T8 byid): não revalidável.
  - **BUG-CACHE-PII** — falta `Cache-Control: no-store` na resposta de PII (T9 byid): não revalidável.
  - **GAP-PAG-0** — `pagina<=0` normaliza silencioso (T9 listar): bloqueado por BUG-009.
  - **GAP-CLAMP** — `tamanhoPagina` clampa mas JSON reflete original (T10 listar): bloqueado.
  - **GAP-UNACCENT** — busca sem `unaccent` (T15 listar): bloqueado.
- Bugs novos descobertos:
  - **BUG-009** — migration `RefatoraClienteEndereco` pendente: schema atual sem `data_nascimento` e sem campos `endereco_*` → 500 em **todo** acesso a clientes (POST, GET, GET/{id}, PUT, PATCH). Bloqueante.
  - **BUG-010** — `ObterPorIdAsync` com id inexistente retorna 500 (Npgsql) em vez de 404. Mesmo que a migration estivesse aplicada, falta um caminho defensivo: o EF dispara a query bruta antes de o service mapear `null`. Hoje, sob BUG-009, T2/T4/T5/T9/T11 byid sobem para 500.
- Bugs fechados: **BUG-CR001** (login admin 200 — confirmado: `POST /api/v1/auth/login` → 200, accessToken válido por 15min).
- Sugerido fechar também **BUG-CR002**: com token válido, `?ativo=xyz` retorna `400` com `errors.ativo: ["The value 'xyz' is not valid."]` (model binder do ASP.NET dispara antes do service).

## Bugs

### BUG-009 — Migration `RefatoraClienteEndereco` não aplicada → 500 em todo CRUD de Clientes [CRÍTICO]

- **Severidade:** crítica (bloqueante para qualquer teste de Clientes que toque o banco).
- **Sintoma:** qualquer chamada a `/api/v1/clientes` (GET, GET/{id}, POST) com auth válida → `500` + `application/problem+json` com `correlationId`. Log:
  ```
  Npgsql.PostgresException 42703: column c.data_nascimento does not exist (POSITION 79)
  Npgsql.PostgresException 42703: column "data_nascimento" of relation "clientes" does not exist (no INSERT)
  ```
- **Causa:** `__ef_migrations_history` só tem `20260513114525_InitialSchema` e `20260517022432_AddUsuarioLockoutFields`. A migration `20260517061810_RefatoraClienteEndereco` existe no código mas não foi aplicada no banco.
- **Casos afetados (v2):**
  - Listar: T1, T2, T3, T4, T5, T6, T7, T9, T10, T11, T14, T15, T16, T17 (14 casos → 500 e BLOCKED para verificar contrato).
  - Por id: T1, T2, T4, T5, T8, T9, T10, T11 (8 casos).
- **Reprodução:**
  ```bash
  TOKEN=$(curl -s -X POST http://localhost:8080/api/v1/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"admin@carwash.local","senha":"DevSeedAdmin2026!Forte"}' | jq -r .accessToken)
  curl -i -X GET "http://localhost:8080/api/v1/clientes" -H "Authorization: Bearer $TOKEN"
  # → 500 + correlationId
  ```
- **Sugestão de correção:** rodar `dotnet ef database update --project backend/src/CarWash.Infrastructure --startup-project backend/src/CarWash.Api` (ou equivalente em pipeline). Garantir que CI/CD aplique migrations antes do healthcheck. Adicionar teste de smoke pós-deploy que faça `GET /api/v1/clientes` e exija 200 (ou 200 com lista vazia).
- **Recomendação de QA:** publicar gate de release que falhe se `__ef_migrations_history` divergir das migrations físicas do projeto.

### BUG-010 — `GET /api/v1/clientes/{id}` com id inexistente devolve 500 quando há erro de schema (deveria isolar)

- **Severidade:** alta (revelada pelo BUG-009, mas indica fragilidade de design).
- **Sintoma:** mesmo com Guid sintaticamente válido aleatório, T2 e T4 retornam `500 + correlationId`, não `404`. Sob schema correto, o esperado é `NotFound()` puro.
- **Reprodução:**
  ```bash
  curl -i -X GET "http://localhost:8080/api/v1/clientes/11111111-2222-3333-4444-555555555555" \
    -H "Authorization: Bearer $TOKEN"
  # → 500 (correlationId: 351dc032bcc2427d9cae34ec4bc1daa0)
  ```
- **Observação:** atualmente o 500 vem do BUG-009 (EF não consegue compilar a query). Após corrigir o schema, este caso deve voltar a 404 — mas vale validar que o `ExceptionHandlingMiddleware` mascara `correlationId` no body (faz) e que não vaza stack trace ao cliente (não vaza — body limpo). Recomenda-se adicionar teste de integração `xUnit` para Guid aleatório → 404 limpo após BUG-009 fechar.

### BUG-CR002 — `[Authorize]` antes do model binding [CANDIDATO A FECHAR]

- **Severidade:** baixa.
- **Status v2:** com token válido, `?ativo=xyz` retorna `400` com `{"errors":{"ativo":["The value 'xyz' is not valid."]}}`. Comportamento esperado de binding do ASP.NET, sem regressão. **Sugerido fechar.**

### Bugs herdados ainda abertos (não revalidáveis sem dados — BUG-009 bloqueia)

- **BUG-LGPD-CLI**: PII em claro na listagem e no get-by-id (T17 listar / T10 byid). Pendente.
- **BUG-TENANT-CLI**: ausência de filtro de tenant em `ObterPorIdAsync` (T8 byid). Pendente.
- **BUG-CACHE-PII**: ausência de `Cache-Control: no-store` na resposta com PII (T9 byid). Pendente.
- **GAP-PAG-0**: `pagina<=0` normaliza para 1 sem 400 (T9 listar). Pendente.
- **GAP-CLAMP**: `tamanhoPagina` clampa em 100 mas JSON devolve valor original (T10 listar). Pendente.
- **GAP-UNACCENT**: busca não usa `unaccent` (T15 listar). Pendente.

## GET /api/v1/clientes (17 casos)

| ID  | Cenário                                         | Esperado                                       | Obtido (v2)                                                                                                                                                           | Resultado | Bug             |
| --- | ----------------------------------------------- | ---------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------- | --------------- |
| T1  | Golden path sem filtros                         | 200 + lista paginada                           | `500 + correlationId` (`column c.data_nascimento does not exist`)                                                                                                      | BLOCKED   | BUG-009         |
| T2  | Paginação válida                                | 200 + 5 itens                                  | `500`                                                                                                                                                                  | BLOCKED   | BUG-009         |
| T3  | Busca `joao`                                    | 200 + itens com "joao"                         | `500`                                                                                                                                                                  | BLOCKED   | BUG-009         |
| T4  | Busca vazia                                     | 200 equivalente a sem busca                    | `500`                                                                                                                                                                  | BLOCKED   | BUG-009         |
| T5  | SQL injection em busca                          | 200, sem 500                                   | `500` (não dá pra validar parametrização — o schema quebra antes do filtro chegar à query)                                                                             | BLOCKED   | BUG-009         |
| T6  | Filtro `ativo=true`                             | 200, só ativos                                 | `500`                                                                                                                                                                  | BLOCKED   | BUG-009         |
| T7  | Filtro `ativo=false`                            | 200, só inativos                               | `500`                                                                                                                                                                  | BLOCKED   | BUG-009         |
| T8  | `ativo=xyz` inválido                            | 400 binding                                    | `HTTP/1.1 400 Bad Request` + `application/problem+json` + `{"errors":{"ativo":["The value 'xyz' is not valid."]}}` (binding do ASP.NET dispara antes do service)        | **PASS**  | —               |
| T9  | `pagina=0` / `-1`                               | 400 idealmente; gap: 200 normalizado           | `500` (sem evidência de normalização — query estoura no banco)                                                                                                         | BLOCKED   | BUG-009 / GAP-PAG-0 |
| T10 | `tamanhoPagina=0/-5/10000`                      | Clamp para 100; JSON inconsistente             | `500` em todos os três valores                                                                                                                                         | BLOCKED   | BUG-009 / GAP-CLAMP |
| T11 | Página além do total                            | 200, `Itens=[]`                                | `500`                                                                                                                                                                  | BLOCKED   | BUG-009         |
| T12 | Sem Authorization                               | 401                                            | `HTTP/1.1 401 Unauthorized`, `WWW-Authenticate: Bearer`, `Content-Length: 0`, `X-Correlation-Id` presente                                                              | **PASS**  | —               |
| T13 | Token inválido                                  | 401                                            | `HTTP/1.1 401 Unauthorized`, `WWW-Authenticate: Bearer error="invalid_token", error_description="The signature key was not found"`, body vazio                        | **PASS**  | —               |
| T14 | Combinação completa                             | 200                                            | `500`                                                                                                                                                                  | BLOCKED   | BUG-009         |
| T15 | Busca com acento `joão`                         | 200, não casa (gap)                            | `500`                                                                                                                                                                  | BLOCKED   | BUG-009 / GAP-UNACCENT |
| T16 | Performance `tamanhoPagina=100` (volume)        | < 500ms                                        | `status=500 time=0.0075s` (rápido, mas erro)                                                                                                                           | BLOCKED   | BUG-009         |
| T17 | PII em claro                                    | CPF/CNPJ em claro (gap LGPD)                   | `500` — sem body para inspecionar                                                                                                                                      | BLOCKED   | BUG-009 / BUG-LGPD-CLI |

## GET /api/v1/clientes/{id} (11 casos)

| ID  | Cenário                              | Esperado                                       | Obtido (v2)                                                                                                                                                       | Resultado | Bug             |
| --- | ------------------------------------ | ---------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ | --------- | --------------- |
| T1  | Golden path                          | 200 + `ClienteResponse`                        | Sem cliente persistível (POST cliente também 500 por BUG-009)                                                                                                       | BLOCKED   | BUG-009         |
| T2  | Guid válido inexistente              | 404 sem body                                   | `HTTP/1.1 500 Internal Server Error` + `application/problem+json` (correlationId 351dc032bcc2427d9cae34ec4bc1daa0) — **deveria ser 404**                            | **FAIL**  | BUG-009 / BUG-010 |
| T3  | Id não-Guid (`abc`)                  | 404 (route constraint)                         | `HTTP/1.1 404 Not Found`, `Content-Length: 0`, sem `WWW-Authenticate` (route constraint `{id:guid}` rejeita antes do Authorize)                                     | **PASS**  | —               |
| T4  | Guid zero                            | 404 sem body                                   | `HTTP/1.1 500 Internal Server Error` (correlationId 99f1afbdba4e4a16a678afee06bb1b53) — **deveria ser 404**                                                         | **FAIL**  | BUG-009 / BUG-010 |
| T5  | Guid maiúsculo                       | 200                                            | `500` (impossível obter 200 sem dados; ainda assim Guid case-insensitive funcionaria — schema quebra antes)                                                          | BLOCKED   | BUG-009         |
| T6  | Sem `Authorization`                  | 401                                            | `HTTP/1.1 401 Unauthorized`, `WWW-Authenticate: Bearer`, `Content-Length: 0`                                                                                        | **PASS**  | —               |
| T7  | Token inválido/expirado              | 401                                            | `HTTP/1.1 401 Unauthorized`, `WWW-Authenticate: Bearer error="invalid_token"`, body vazio                                                                            | **PASS**  | —               |
| T8  | Token de outro tenant                | 200 (sem filtro de tenant — risco)             | Sem dois tokens distintos + sem cliente persistível                                                                                                                  | BLOCKED   | BUG-009 / BUG-TENANT-CLI |
| T9  | `Cache-Control` em PII               | `no-store` ou `private`                        | Não foi possível obter resposta 200 — em 500 atual não há header de cache aplicável                                                                                  | BLOCKED   | BUG-009 / BUG-CACHE-PII |
| T10 | PII em claro                         | CPF/CNPJ em claro (gap LGPD)                   | Não foi possível obter 200 com body                                                                                                                                  | BLOCKED   | BUG-009 / BUG-LGPD-CLI |
| T11 | Performance < 300ms                  | < 0.3s                                         | `status=500 time=0.0047s` (rápido, mas erro)                                                                                                                         | BLOCKED   | BUG-009         |

## Observações de senioridade (QA)

1. **CA011 ainda não está cumprido para Clientes Read.** Sair de "Auth 500" para "Clientes 500" mantém o efeito prático: a suíte de homologação não consegue exercitar nenhum caso que dependa de dados. Antes de marcar a sprint como concluída, **o release deve aplicar migrations no setup do healthcheck** e o gate de CI precisa rodar `dotnet ef database update` ou equivalente.
2. **Sequência de pipeline validada.** T3 byid (404 por route constraint), T12/T13 listar (401 com `WWW-Authenticate`) e T6/T7 byid (401) confirmam que `UseRouting → UseAuthentication → UseAuthorization` segue íntegro pós-fix de Auth. Nada regredidu na borda HTTP.
3. **Pegadinha 404 vs 500.** Mesmo depois de aplicar BUG-009, é importante adicionar um teste regressão para Guid inexistente → 404 (BUG-010). Hoje o EF dispara a query antes do `null`-check; com schema correto, o `FirstOrDefaultAsync` retorna `null` e o controller responde 404. Mas qualquer mismatch futuro vai voltar a 500.
4. **PII / LGPD continua pendente** (T17 listar, T9 e T10 byid). Bloqueante para release em produção. Reescalonar com PO/PM e arquiteto — a decisão de mascarar CPF/CNPJ na listagem e exigir claim para vê-los completos precisa ser tomada antes do CA005.
5. **Cross-tenant leak (T8 byid)** segue inviabilizado de testar nesta rodada — sem cliente persistível e sem 2º usuário. Quando BUG-009 cair, executar com `USER_A` criando cliente e `USER_B` lendo o id, esperando filtro/403.
6. **Anti-flakiness:** todas as observações desta rodada são determinísticas (10/10 reprodução). Latência de 500 ficou < 10ms — Npgsql falha cedo, antes de qualquer I/O significativo. Nenhum jitter detectado.

## Próximos passos

1. **Acionar `dev-dotnet-carwash`** para aplicar a migration `20260517061810_RefatoraClienteEndereco` no banco de desenvolvimento e ajustar o pipeline (`dotnet ef database update` no entrypoint do container backend ou step explícito do compose).
2. **Após aplicar**, rebater novamente esta suíte (esperado: 22 BLOCKED → maioria PASS, restando FAIL nos gaps LGPD / tenant / clamp / unaccent / paginação).
3. **Adicionar teste automatizado de regressão** (xUnit + WebApplicationFactory + Testcontainers) cobrindo:
   - Migration aplicada com sucesso no setup do fixture (smoke).
   - Guid inexistente → 404 puro (BUG-010).
   - `?ativo=xyz` → 400 com `errors.ativo` (já passa hoje — congelar via teste).
   - `[Trait("CA","011")]` em cada um deles para entrar no gate de release.
4. **Não fechar a sprint** de Clientes Read antes da migration aplicada e da rebateria seguinte.
