# Relatório — Health (v4)

Data: 2026-05-17T18:40:36Z
Backend: http://localhost:8080
Commit: cd16b6a462ce9c164f16e483ade1c99e68dfd76b
Executor: QA Engineer Sênior (agente automatizado, rebateria v4)
Rodada anterior: ../v3-pos-fix2/health.md

## Comparativo v3 → v4

| Métrica | v3 | v4 |
|---|---:|---:|
| PASS | 9 | 9 |
| FAIL | 0 | 0 |
| SKIP | 2 | 2 |
| Total | 11 | 11 |

Sem regressão. T4/T5 permanecem SKIP nesta janela: no momento da execução, `ls QA/_relatorios/*.md` (raiz) está vazio — nenhum dos 4 outros agentes paralelos publicou seu relatório v4 (`auth.md`, `usuarios.md`, `clientes-read.md`, `clientes-write.md`) na raiz. Derrubar o Postgres provocaria falsos negativos cruzados. Política das rodadas anteriores mantida.

## Sumário

- Total: 11 | PASS: 9 | FAIL: 0 | SKIP: 2 (T4 e T5)

## Ambiente

- `carwash-backend`: respondendo `200 OK` em `/health` no início da suíte.
- `carwash-postgres`: `Up 15 hours (healthy)`.
- Outros 4 agentes QA ainda rodando em paralelo (raiz de `_relatorios/` sem arquivos `.md`, apenas subpastas `v1-pre-fix/`, `v2-pos-fix1/`, `v3-pos-fix2/`, `v4-pos-fix3/`).

## Observações gerais

- Comportamento idêntico ao v1/v2/v3: três rotas anônimas, `Content-Type: text/plain`, body `Healthy`, headers `Cache-Control: no-store, no-cache` + `Expires: Thu, 01 Jan 1970 00:00:00 GMT` + `Pragma: no-cache`, `X-Correlation-Id` único por requisição.
- Latência permanece excelente, equivalente a v3:
  - `/health`: 1.585 / 1.719 / 5.854 ms (v3: 1.113–1.145 ms) — pico de 5.854 ms ainda muito abaixo do limite de 50 ms.
  - `/health/live`: 5.345 / 1.793 / 7.043 ms (v3: 1.085–1.274 ms) — variação dentro do esperado em ambiente compartilhado com 4 outros agentes.
  - `/health/ready`: 2.439 / 4.933 / 2.085 ms (v3: 1.467–1.680 ms) — todos << 500 ms.
- T11: 50 requisições paralelas a `/health/ready` retornaram 100% `200` (saída `50 200`); `/health` respondeu `200` imediatamente após a carga. Pool Npgsql sem sinais de exaustão.
- `X-Correlation-Id` distinto por requisição confirmado em T1/T2/T3 (sample: `3dbb7efe..`, `829b4048..`, `d685d6d5..`).

## Bugs

Nenhum bug identificado nos 9 casos executados nesta rebateria. T4/T5 ainda não verificados empiricamente.

## Casos (11)

| ID  | Rota                | Descrição                                  | Esperado                                   | Obtido                                                                                                       | Resultado | Bug |
|-----|---------------------|--------------------------------------------|--------------------------------------------|--------------------------------------------------------------------------------------------------------------|-----------|-----|
| T1  | `GET /health`       | Backend + Postgres up                      | 200 / `Healthy` / `text/plain`             | 200 / `Healthy` / `text/plain` / `Cache-Control: no-store, no-cache` / `X-Correlation-Id: 3dbb7efe..`         | PASS      | —   |
| T2  | `GET /health/live`  | Backend up                                 | 200 / `Healthy`                            | 200 / `Healthy` / `text/plain` (idêntico a T1, headers iguais) / `X-Correlation-Id: 829b4048..`               | PASS      | —   |
| T3  | `GET /health/ready` | Backend + Postgres up                      | 200 / `Healthy`                            | 200 / `Healthy` / `text/plain` / `X-Correlation-Id: d685d6d5..` (DB tocado, latência ≈2.44 ms)               | PASS      | —   |
| T4  | `GET /health/ready` | Postgres parado                            | 503 / `Unhealthy` com detalhe `postgres`   | Não executado — 4 agentes paralelos ainda dependem do DB ativo (sem relatórios v4 publicados na raiz)        | SKIP      | —   |
| T5  | `GET /health`       | Postgres parado                            | 200 / `Healthy` (independe do DB)          | Não executado — 4 agentes paralelos ainda dependem do DB ativo                                                | SKIP      | —   |
| T6  | `/health` e `/live` | Latência                                   | < 50ms                                     | `/health` 1.585/1.719/5.854 ms; `/health/live` 5.345/1.793/7.043 ms — todos << 50 ms                          | PASS      | —   |
| T7  | `/health/ready`     | Latência com DB                            | < 500ms                                    | 2.439/4.933/2.085 ms — << 500 ms                                                                              | PASS      | —   |
| T8  | Todas               | `Authorization:` vazio (anônimo)           | 200 nas 3                                  | `/health` 200, `/health/live` 200, `/health/ready` 200                                                        | PASS      | —   |
| T9  | Todas               | `Content-Type`                             | `text/plain`                               | `text/plain` nas 3 rotas (sem charset)                                                                        | PASS      | —   |
| T10 | Todas               | `Cache-Control`                            | Sem caching (`no-store`)                   | `Cache-Control: no-store, no-cache` + `Expires: Thu, 01 Jan 1970 00:00:00 GMT` + `Pragma: no-cache` em todas | PASS      | —   |
| T11 | `/health/ready`     | 50 requisições paralelas                   | 50× 200, sem esgotar pool                  | `50 200` exatos; `/health` 200 logo após a carga; sem erros nos logs                                          | PASS      | —   |

## Conclusão

Quarta rebateria confirma estabilidade total das três rotas de health check após o commit `cd16b6a`. Nenhuma regressão em relação a v1/v2/v3; latências marginalmente maiores que v3 (faixa 1.5–7 ms vs 1.0–1.7 ms), atribuíveis ao ambiente compartilhado com 4 outros agentes QA em execução paralela — ainda assim ordens de grandeza abaixo dos limites (50ms / 500ms). T4 e T5 seguem como dívida de QA aguardando janela isolada (sugestão recorrente: rodada noturna em CI ou mini-suíte dedicada com `docker compose stop postgres && curl -i /health /health/ready && docker compose start postgres`). A independência de `/health` e `/health/live` em relação ao DB continua validada apenas por inspeção estática em `backend/src/CarWash.Api/Program.cs` linhas 149-176.
