# Relatório — Health (REBATERIA pós-fix)

Data: 2026-05-17T14:58:00Z
Backend: http://localhost:8080
Commit: cd16b6a462ce9c164f16e483ade1c99e68dfd76b
Executor: QA Engineer Sênior (agente automatizado, rebateria v2)
Rodada anterior em: ../v1-pre-fix/health.md

## Comparativo v1 vs v2

| Métrica | v1 | v2 |
|---|---:|---:|
| PASS | 9 | 9 |
| FAIL | 0 | 0 |
| SKIP | 2 | 2 |
| Total | 11 | 11 |

Sem regressão. T4 e T5 permanecem SKIP nesta janela porque os outros 4 agentes paralelos (auth, usuarios, clientes-read, clientes-write) ainda não publicaram seus relatórios em `_relatorios/` — derrubar Postgres impactaria todos eles. A política da v1 (não impactar paralelos) foi mantida deliberadamente.

## Sumário

- Total: 11 | PASS: 9 | FAIL: 0 | SKIP: 2 (T4 e T5)

## Ambiente

- `carwash-backend`: Up 12 minutes (após rebuild pós-fix)
- `carwash-postgres`: Up 11 hours (healthy)
- `carwash-frontend`: Up 8 hours
- Outros agentes QA rodando em paralelo contra o mesmo backend/DB (nenhum relatório `auth.md`/`usuarios.md`/`clientes-*.md` publicado em `_relatorios/` no momento da execução — apenas `v1-pre-fix/`).

## Observações gerais

- Comportamento idêntico ao v1: três rotas anônimas, `Content-Type: text/plain`, body `Healthy`, headers `Cache-Control: no-store, no-cache`, `Expires: 1970-01-01`, `Pragma: no-cache`, `X-Correlation-Id` único por requisição.
- Latência permanece excelente: `/health` ≈1.7ms, `/health/live` ≈1.4ms, `/health/ready` ≈2.1ms — ordens de grandeza abaixo dos thresholds (50ms / 500ms).
- 50 requisições paralelas a `/health/ready`: 100% 200, pool Npgsql não esgotou; `/health` responde 200 imediatamente após a carga.
- Logs `docker logs carwash-backend --since 60s` não contêm erros associados aos endpoints de health. Há `DbUpdateException` com `errorMissingColumn` em `ExceptionHandlingMiddleware.cs:37` provenientes de outra suíte paralela (não pertencem a estas rotas).

## Bugs

Nenhum bug identificado nos 9 casos executados nesta rebateria. Conforme esperado para a rota de health (a correção foi em `/auth/login`, não em health checks). T4/T5 não executados nesta janela por convivência com agentes paralelos.

## Casos (11)

| ID  | Rota                | Descrição                                  | Esperado                                   | Obtido                                                                                                       | Resultado | Bug |
|-----|---------------------|--------------------------------------------|--------------------------------------------|--------------------------------------------------------------------------------------------------------------|-----------|-----|
| T1  | `GET /health`       | Backend + Postgres up                      | 200 / `Healthy` / `text/plain`             | 200 / `Healthy` / `text/plain` / `no-store, no-cache` / `X-Correlation-Id` presente                          | PASS      | —   |
| T2  | `GET /health/live`  | Backend up                                 | 200 / `Healthy`                            | 200 / `Healthy` / `text/plain` (idêntico a T1)                                                                | PASS      | —   |
| T3  | `GET /health/ready` | Backend + Postgres up                      | 200 / `Healthy`                            | 200 / `Healthy` / `text/plain` (latência ≈2.1ms — DB tocado normalmente)                                      | PASS      | —   |
| T4  | `GET /health/ready` | Postgres parado                            | 503 / `Unhealthy` com detalhe `postgres`   | Não executado — outros 4 agentes paralelos dependem do DB ativo                                              | SKIP      | —   |
| T5  | `GET /health`       | Postgres parado                            | 200 / `Healthy` (independe do DB)          | Não executado — outros 4 agentes paralelos dependem do DB ativo                                              | SKIP      | —   |
| T6  | `/health` e `/live` | Latência                                   | < 50ms                                     | `/health` 2.12/1.70/1.72ms; `/health/live` 1.48/1.27/1.59ms — todos << 50ms                                  | PASS      | —   |
| T7  | `/health/ready`     | Latência com DB                            | < 500ms                                    | 2.11/2.06/2.29ms — << 500ms                                                                                   | PASS      | —   |
| T8  | Todas               | `Authorization:` vazio (anônimo)           | 200 nas 3                                  | `/health` 200, `/health/live` 200, `/health/ready` 200                                                        | PASS      | —   |
| T9  | Todas               | `Content-Type`                             | `text/plain`                               | `text/plain` nas 3 rotas (sem charset)                                                                        | PASS      | —   |
| T10 | Todas               | `Cache-Control`                            | Sem caching (`no-store`)                   | `Cache-Control: no-store, no-cache` + `Expires: 1970-01-01` + `Pragma: no-cache` em todas                    | PASS      | —   |
| T11 | `/health/ready`     | 50 requisições paralelas                   | 50× 200, sem esgotar pool                  | 50× 200; `/health` 200 após carga; sem erros relacionados nos logs                                            | PASS      | —   |

## Conclusão

Rebateria pós-fix confirma a estabilidade das três rotas de health check. Nenhuma regressão observada após `cd16b6a`. T4 e T5 permanecem como dívida de QA até janela isolada (sugestão: CI noturno ou mini-suíte dedicada de 30s com `docker compose stop postgres && curl -i /health /health/ready && docker compose start postgres`). A configuração `Predicate = _ => false` em `/health` e `/health/live` (independência do DB) continua não verificada empiricamente — apenas inspeção estática de `backend/src/CarWash.Api/Program.cs` linhas 149-176.
