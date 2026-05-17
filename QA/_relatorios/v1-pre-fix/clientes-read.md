# Relatório — Clientes Read (GET listar, GET /{id})

Data: 2026-05-17T14:24:00Z
Backend: http://localhost:8080
Pré-condição: **NÃO obteve token** — login `POST /api/v1/auth/login` retorna `500` por causa do bug pré-conhecido `column "bloqueado_ate" of relation "usuarios" does not exist`. Dependência bloqueante herdada do módulo Auth. Sem token, todos os casos que exigem usuário autenticado válido foram marcados como **BLOCKED**.

Tabela `clientes` populada: `SELECT COUNT(*) FROM clientes` = `0`. Não foi possível popular via `POST /api/v1/clientes` (também exige `[Authorize]`), e a regra operacional proíbe `INSERT` direto neste cenário (preferir endpoint POST). Sem dados não rodam T1–T5, T6, T7, T11, T14–T17 do listar e T1, T2, T4, T5, T8, T9, T10, T11 do por_id.

## Sumário

- Total: 28 | PASS: 6 | FAIL: 0 | BLOCKED: 22
- Bugs novos: 0 (todos os achados deste lote são reiteração/confirmação de bugs já conhecidos)

## Bugs

### BUG-CR001 — Login admin retorna 500 (`column "bloqueado_ate" does not exist`) [HERDADO DE AUTH]

- Severidade: **bloqueante** para QA de Clientes (impede obter token)
- Sintoma: `POST /api/v1/auth/login` com credenciais válidas devolve `500` + `application/problem+json` com `correlationId`. Em log: `Npgsql.PostgresException 42703: column "bloqueado_ate" of relation "usuarios" does not exist`. Schema atual de `usuarios` não tem a coluna que o EF Core esquema do código tenta `UPDATE` ao registrar tentativa de login.
- Casos afetados: praticamente toda a suíte (qualquer um que dependa de `$TOKEN`).
- Reprodução:
  ```bash
  curl -s -X POST "http://localhost:8080/api/v1/auth/login" \
    -H "Content-Type: application/json" \
    -d '{"email":"admin@carwash.local","senha":"DevSeedAdmin2026!Forte"}' -i
  ```
- Log: `column "bloqueado_ate" of relation "usuarios" does not exist` em `ExceptionHandlingMiddleware.cs:line 37`.
- Sugestão: criar migration adicionando `bloqueado_ate timestamptz NULL` em `usuarios`, ou remover o `UPDATE` da entidade até a migration existir.

### BUG-CR002 — `[Authorize]` é avaliado antes do model binding de query string (impacta T8)

- Severidade: baixa (semântica)
- Sintoma: `GET /api/v1/clientes?ativo=xyz` sem header → `401` (esperado `401`, OK). Com header `Authorization: Bearer fake` → também `401` com `invalid_token` (correto). Não foi possível confirmar se com token válido o `ativo=xyz` retorna `400` de binding — depende de destravar Auth (BUG-CR001).
- Casos afetados: T8 listar (BLOCKED).
- Observação: comportamento atual é seguro; pode mascarar gap de validação caso o ASP.NET resolva `ativo=xyz` como `null` silenciosamente. Reavaliar após corrigir BUG-CR001.

## GET /api/v1/clientes (17 casos)

| ID  | Descrição                                  | Esperado                                   | Obtido                                                                                                                                   | Resultado | Bug                |
| --- | ------------------------------------------ | ------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------- | --------- | ------------------ |
| T1  | Golden path sem filtros                    | 200 + lista paginada                       | Sem token (BUG-CR001) e sem dados                                                                                                        | BLOCKED   | BUG-CR001          |
| T2  | Paginação válida                           | 200 + 5 itens                              | Sem token e sem dados                                                                                                                    | BLOCKED   | BUG-CR001          |
| T3  | Busca por termo simples                    | 200 + itens com `joao`                     | Sem token e sem dados                                                                                                                    | BLOCKED   | BUG-CR001          |
| T4  | Busca vazia                                | 200 equivalente a sem busca                | Sem token                                                                                                                                | BLOCKED   | BUG-CR001          |
| T5  | SQL injection em busca                     | 200, sem 500                               | Sem token (não dá pra validar parametrização end-to-end)                                                                                 | BLOCKED   | BUG-CR001          |
| T6  | Filtro ativos                              | 200, só ativos                             | Sem token                                                                                                                                | BLOCKED   | BUG-CR001          |
| T7  | Filtro inativos                            | 200, só inativos                           | Sem token                                                                                                                                | BLOCKED   | BUG-CR001          |
| T8  | `ativo=xyz` inválido                       | 400 binding (ou gap de validação)          | `401` (Authorize precede o binding); com header fake também `401 invalid_token`. Não foi possível observar o binding error.              | BLOCKED   | BUG-CR001/BUG-CR002 |
| T9  | Página zero/negativa                       | 200 normalizado (gap)                      | Sem token (não confirmou normalização)                                                                                                   | BLOCKED   | BUG-CR001          |
| T10 | TamanhoPagina inválido (0/-5/10000)        | Clamp para 100, JSON inconsistente         | Sem token                                                                                                                                | BLOCKED   | BUG-CR001          |
| T11 | Página além do total                       | 200 + `Itens=[]`                           | Sem token                                                                                                                                | BLOCKED   | BUG-CR001          |
| T12 | Sem Authorization                          | 401                                        | `HTTP/1.1 401 Unauthorized`, `WWW-Authenticate: Bearer`, body vazio, `X-Correlation-Id` presente                                          | PASS      | —                  |
| T13 | Token expirado/inválido                    | 401                                        | `HTTP/1.1 401 Unauthorized`, `WWW-Authenticate: Bearer error="invalid_token"`, body vazio                                                | PASS      | —                  |
| T14 | Combinação completa de filtros             | 200                                        | Sem token                                                                                                                                | BLOCKED   | BUG-CR001          |
| T15 | Busca com acento (gap unaccent)            | 200, não casa                              | Sem token (não confirmou)                                                                                                                | BLOCKED   | BUG-CR001          |
| T16 | Performance com volume                     | < 500ms                                    | Sem token e sem volume                                                                                                                   | BLOCKED   | BUG-CR001          |
| T17 | PII em claro                               | CPF/CNPJ no body                           | Sem token (não há JSON para inspecionar)                                                                                                 | BLOCKED   | BUG-CR001          |

## GET /api/v1/clientes/{id} (11 casos)

| ID  | Descrição                          | Esperado                                          | Obtido                                                                                                                  | Resultado | Bug       |
| --- | ---------------------------------- | ------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------- | --------- | --------- |
| T1  | Golden path                        | 200 + ClienteResponse                             | Sem token + sem cliente                                                                                                 | BLOCKED   | BUG-CR001 |
| T2  | Guid válido inexistente            | 404 sem body                                      | Sem token                                                                                                               | BLOCKED   | BUG-CR001 |
| T3  | Id não-Guid (`abc`)                | 404 (route constraint, antes do controller)       | `HTTP/1.1 404 Not Found`, `Content-Length: 0`, sem `WWW-Authenticate` (rota nem casa, então Authorize não roda)         | PASS      | —         |
| T4  | Guid zero `0000...0000`            | 404 sem body                                      | Sem token                                                                                                               | BLOCKED   | BUG-CR001 |
| T5  | Guid em maiúsculas                 | 200                                               | Sem token + sem cliente                                                                                                 | BLOCKED   | BUG-CR001 |
| T6  | Sem `Authorization`                | 401                                               | `HTTP/1.1 401 Unauthorized`, `WWW-Authenticate: Bearer`, body vazio                                                     | PASS      | —         |
| T7  | Token expirado/inválido            | 401                                               | `HTTP/1.1 401 Unauthorized`, `WWW-Authenticate: Bearer error="invalid_token"`, body vazio                               | PASS      | —         |
| T8  | Token de outro usuário             | 200 (sem filtro de tenant — risco)                | Sem dois tokens                                                                                                         | BLOCKED   | BUG-CR001 |
| T9  | `Cache-Control` na resposta de PII | `no-store` ou `private`                           | Sem token (não foi possível obter resposta 200 com headers)                                                             | BLOCKED   | BUG-CR001 |
| T10 | PII em claro no body               | CPF/CNPJ etc                                      | Sem token                                                                                                               | BLOCKED   | BUG-CR001 |
| T11 | Performance < 300ms                | < 0.3s                                            | Sem token                                                                                                               | BLOCKED   | BUG-CR001 |

## Observações de senioridade (QA)

1. **CA011 NÃO está cumprido para Clientes Read** enquanto Auth estiver quebrado. Sem token, qualquer suíte automatizada de homologação que dependa de Bearer falha — e o test fixture típico (xUnit + WebApplicationFactory) só consegue blindar isso com a coluna `bloqueado_ate` presente.
2. **Achados validáveis sem token** estão todos PASS: roteamento e pipeline de autenticação estão consistentes (`401` com header correto, `404` por route constraint para `id` não-Guid antes do Authorize).
3. **Sequência correta do pipeline** observada (UseRouting → UseAuthentication → UseAuthorization → MapController): nas rotas `/{id:guid}`, o roteamento rejeita o id malformado antes do Authorize correr — por isso T3 do byid não exige token e é PASS.
4. **Pendência para reexecução:** assim que BUG-CR001 for corrigido (migration adicionando `bloqueado_ate timestamptz NULL`), refazer T1–T11 (listar) e T1, T2, T4, T5, T8–T11 (byid). É recomendável que a suíte CA011 inclua um *health-check* de login no setup para falhar cedo em qualquer regressão de schema.
5. **Gaps já documentados** (não revalidados aqui por bloqueio):
   - T9 listar: `pagina <= 0` normalização silenciosa (deveria 400).
   - T10 listar: clamp de `tamanhoPagina` reflete valor original no JSON (inconsistência de contrato).
   - T15 listar: ausência de `unaccent` na busca.
   - T17 listar / T10 byid: PII em claro (gap LGPD bloqueador para produção).
   - T8 byid: ausência de filtro de tenant/dono (cross-tenant leak).
   - T9 byid: ausência de `Cache-Control: no-store` para PII.

## Próximos passos

1. **Acionar `dev-dotnet-carwash`** para entregar a migration de `bloqueado_ate` em `usuarios` — sem isso, Clientes Read fica em BLOCKED indefinido.
2. **Após desbloqueio,** rodar a suíte de novo e reportar — esperar mover de 22 BLOCKED para majoritariamente PASS, com FAIL apenas onde os gaps documentados existirem (T9, T10, T15, T17 listar; T8, T9, T10 byid).
3. **Não fechar a sprint** de Clientes Read antes de revalidar — CA011 exige cobertura em homologação que hoje não é possível executar.
