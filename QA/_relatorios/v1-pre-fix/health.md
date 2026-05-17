# Relatório — Health Checks (/health, /health/live, /health/ready)

Data: 2026-05-17T14:24:00Z
Backend: http://localhost:8080
Commit: cd16b6a462ce9c164f16e483ade1c99e68dfd76b
Executor: QA Engineer Sênior (agente automatizado)

## Sumário

- Total: 11 | PASS: 9 | FAIL: 0 | SKIP: 2 (T4 e T5 — exigem `docker compose stop postgres`, evitando impacto em testes paralelos)

## Ambiente

- Backend container `carwash-backend`: Up 7 hours
- Postgres container `carwash-postgres`: Up 11 hours (healthy)
- Outros agentes QA rodando em paralelo contra o mesmo backend/DB.

## Observações gerais

- Todas as três rotas estão anônimas, retornando `Content-Type: text/plain` e corpo `Healthy` quando o sistema está OK — conforme especificado em `backend/src/CarWash.Api/Program.cs` linhas 149-176.
- Headers de cache adequados: `Cache-Control: no-store, no-cache`, `Expires: Thu, 01 Jan 1970 00:00:00 GMT`, `Pragma: no-cache`. Health checks não são cacheados — comportamento correto.
- Cada resposta inclui `X-Correlation-Id` único — observabilidade adequada.
- Latência muito abaixo dos thresholds (≈1-3ms para todas as rotas, incluindo `/health/ready` que toca o DB).
- Logs do backend não registram entradas de erro/warn relacionadas aos endpoints de health durante a execução. O único `Microsoft.EntityFrameworkCore.DbUpdateException` capturado nos logs é de outra suíte rodando em paralelo (não pertence à rota de health).
- 50 requisições paralelas a `/health/ready` retornaram 100% 200 — sem exhaust do pool Npgsql.

## Bugs

Nenhum bug identificado nos 9 casos executados.

T4 (DB down → 503 em /health/ready) e T5 (DB down → 200 em /health) não foram executados nesta janela para não impactar os outros 4 agentes que dependem do Postgres. Devem ser executados em janela isolada, com `docker compose stop postgres && curl -i /health/ready && curl -i /health && docker compose start postgres`. A abordagem alternativa de `pg_terminate_backend` foi descartada porque o pool do Npgsql/ASP.NET Core reabriria conexão imediatamente — não simula readiness probe falhando de fato (o check `AddNpgSql` somente falharia se o socket TCP estivesse indisponível, não com sessões terminadas).

## Casos (11)

| ID  | Rota                | Descrição                                                         | Esperado                                | Obtido                                                                                                       | Resultado | Bug |
|-----|---------------------|-------------------------------------------------------------------|-----------------------------------------|--------------------------------------------------------------------------------------------------------------|-----------|-----|
| T1  | `GET /health`       | Backend + Postgres up                                             | 200 / `Healthy` / `text/plain`          | 200 / `Healthy` / `text/plain` / Cache-Control no-store,no-cache                                              | PASS      | —   |
| T2  | `GET /health/live`  | Backend up                                                        | 200 / `Healthy`                         | 200 / `Healthy` / `text/plain`                                                                                | PASS      | —   |
| T3  | `GET /health/ready` | Backend + Postgres up                                             | 200 / `Healthy`                         | 200 / `Healthy` / `text/plain` (latência ≈2.6ms — DB tocado normalmente)                                      | PASS      | —   |
| T4  | `GET /health/ready` | Postgres parado                                                   | 503 / `Unhealthy` com detalhe `postgres`| Não executado — exige derrubar Postgres com outros agentes ativos                                            | SKIP      | —   |
| T5  | `GET /health`       | Postgres parado                                                   | 200 / `Healthy`                         | Não executado — exige derrubar Postgres com outros agentes ativos                                            | SKIP      | —   |
| T6  | `/health` e `/live` | Latência                                                          | < 50ms                                  | `/health` 1.39/1.59/1.43ms; `/health/live` 1.19/1.30/1.24ms — todos << 50ms                                  | PASS      | —   |
| T7  | `/health/ready`     | Latência com DB                                                   | < 500ms                                 | 3.02/2.65/2.66ms — << 500ms                                                                                   | PASS      | —   |
| T8  | Todas               | `Authorization:` vazio (anônimo)                                  | 200 nas 3                               | `/health` 200, `/health/live` 200, `/health/ready` 200                                                        | PASS      | —   |
| T9  | Todas               | `Content-Type`                                                    | `text/plain`                            | `text/plain` nas 3 rotas (sem charset adicional)                                                              | PASS      | —   |
| T10 | Todas               | `Cache-Control`                                                   | Sem caching (`no-store`)                | `Cache-Control: no-store, no-cache` + `Expires: 1970-01-01` + `Pragma: no-cache` em todas                    | PASS      | —   |
| T11 | `/health/ready`     | 50 requisições paralelas                                          | 50× 200, sem esgotar pool               | 50× 200; `/health` continua 200 após a carga; sem erros nos logs                                              | PASS      | —   |

## Conclusão

As três rotas de health check estão funcionalmente corretas, com latência excelente, sem exposição de dados sensíveis nos headers/body, sem cache indevido, anônimas como esperado e resistentes a carga concorrente (50× paralelo). A configuração `Predicate = _ => false` em `/health` e `/health/live` precisa ainda ser validada por T5 (em janela isolada). T4 é o caso crítico restante (validação do contrato de readiness com orquestradores) e deve ser executado isoladamente após o término das outras suítes paralelas.

Recomendação: agendar T4 + T5 como mini-suíte separada, idealmente em pipeline de CI noturno onde nenhum outro teste depende do Postgres, ou em uma janela manual de 30 segundos com `docker compose stop postgres && sleep 5 && curl ... && docker compose start postgres`.
