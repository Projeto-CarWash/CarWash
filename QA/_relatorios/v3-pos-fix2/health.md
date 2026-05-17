# Relatório — Health (v3)

Data: 2026-05-17T16:09:00Z
Backend: http://localhost:8080
Commit: cd16b6a462ce9c164f16e483ade1c99e68dfd76b
Executor: QA Engineer Sênior (agente automatizado, rebateria v3)
Rodada anterior: ../v2-pos-fix1/health.md

## Comparativo v2 → v3

| Métrica | v2 | v3 |
|---|---:|---:|
| PASS | 9 | 9 |
| FAIL | 0 | 0 |
| SKIP | 2 | 2 |
| Total | 11 | 11 |

Sem regressão. T4/T5 permanecem SKIP nesta janela: no momento da execução, nenhum dos relatórios `auth.md`, `usuarios.md`, `clientes-read.md`, `clientes-write.md` está publicado em `_relatorios/` (apenas `v1-pre-fix/` e `v2-pos-fix1/` contêm artefatos). Portanto os 4 agentes paralelos seguem ativos contra o mesmo Postgres — derrubá-lo provocaria falsos negativos cruzados. Política da v1/v2 mantida.

## Sumário

- Total: 11 | PASS: 9 | FAIL: 0 | SKIP: 2 (T4 e T5)

## Ambiente

- `carwash-backend`: respondendo `200` em `/health` no momento do início da suíte.
- `carwash-postgres`: `Up 12 hours (healthy)`.
- Outros 4 agentes QA rodando em paralelo contra o mesmo backend/DB — nenhum relatório `auth.md`/`usuarios.md`/`clientes-*.md` publicado em `_relatorios/`.

## Observações gerais

- Comportamento idêntico ao v1/v2: três rotas anônimas, `Content-Type: text/plain`, body `Healthy`, headers `Cache-Control: no-store, no-cache` + `Expires: 1970-01-01` + `Pragma: no-cache`, `X-Correlation-Id` único por requisição.
- Latência permanece excelente, levemente melhor que v2:
  - `/health`: 1.113/1.128/1.145 ms (v2: 1.70–2.12 ms)
  - `/health/live`: 1.164/1.274/1.085 ms (v2: 1.27–1.59 ms)
  - `/health/ready`: 1.680/1.467/1.553 ms (v2: 2.06–2.29 ms)
  - Todos ordens de grandeza abaixo dos thresholds (50ms / 500ms).
- T11: 50 requisições paralelas a `/health/ready` retornaram 100% `200`; `/health` responde `200` imediatamente após a carga. Pool Npgsql sem sinais de exaustão.
- `X-Correlation-Id` distinto por requisição confirmado em T1/T2/T3 (sample: `793062..`, `1c5bac..`, `81cc14..`).

## Bugs

Nenhum bug identificado nos 9 casos executados nesta rebateria. T4/T5 ainda não verificados empiricamente.

## Casos (11)

| ID  | Rota                | Descrição                                  | Esperado                                   | Obtido                                                                                                       | Resultado | Bug |
|-----|---------------------|--------------------------------------------|--------------------------------------------|--------------------------------------------------------------------------------------------------------------|-----------|-----|
| T1  | `GET /health`       | Backend + Postgres up                      | 200 / `Healthy` / `text/plain`             | 200 / `Healthy` / `text/plain` / `Cache-Control: no-store, no-cache` / `X-Correlation-Id` presente            | PASS      | —   |
| T2  | `GET /health/live`  | Backend up                                 | 200 / `Healthy`                            | 200 / `Healthy` / `text/plain` (idêntico a T1, headers iguais)                                                | PASS      | —   |
| T3  | `GET /health/ready` | Backend + Postgres up                      | 200 / `Healthy`                            | 200 / `Healthy` / `text/plain` (DB tocado, latência ≈1.55 ms)                                                 | PASS      | —   |
| T4  | `GET /health/ready` | Postgres parado                            | 503 / `Unhealthy` com detalhe `postgres`   | Não executado — 4 agentes paralelos ainda dependem do DB ativo (sem relatórios publicados em `_relatorios/`) | SKIP      | —   |
| T5  | `GET /health`       | Postgres parado                            | 200 / `Healthy` (independe do DB)          | Não executado — 4 agentes paralelos ainda dependem do DB ativo                                                | SKIP      | —   |
| T6  | `/health` e `/live` | Latência                                   | < 50ms                                     | `/health` 1.113/1.128/1.145 ms; `/health/live` 1.164/1.274/1.085 ms — todos << 50 ms                          | PASS      | —   |
| T7  | `/health/ready`     | Latência com DB                            | < 500ms                                    | 1.680/1.467/1.553 ms — << 500 ms                                                                              | PASS      | —   |
| T8  | Todas               | `Authorization:` vazio (anônimo)           | 200 nas 3                                  | `/health` 200, `/health/live` 200, `/health/ready` 200                                                        | PASS      | —   |
| T9  | Todas               | `Content-Type`                             | `text/plain`                               | `text/plain` nas 3 rotas (sem charset)                                                                        | PASS      | —   |
| T10 | Todas               | `Cache-Control`                            | Sem caching (`no-store`)                   | `Cache-Control: no-store, no-cache` + `Expires: 1970-01-01` + `Pragma: no-cache` em todas                    | PASS      | —   |
| T11 | `/health/ready`     | 50 requisições paralelas                   | 50× 200, sem esgotar pool                  | `50 200` exatos; `/health` 200 logo após a carga; sem erros nos logs                                          | PASS      | —   |

## Conclusão

Terceira rebateria confirma estabilidade total das três rotas de health check após o commit `cd16b6a`. Nenhuma regressão em relação a v1/v2; latências marginalmente melhores. T4 e T5 seguem como dívida de QA aguardando janela isolada (sugestão recorrente: rodada noturna em CI ou mini-suíte dedicada com `docker compose stop postgres && curl -i /health /health/ready && docker compose start postgres`). A independência de `/health` e `/health/live` em relação ao DB continua validada apenas por inspeção estática em `backend/src/CarWash.Api/Program.cs` linhas 149-176.
