# Health Checks — GET /health, /health/live, /health/ready

## Resumo

Documento de QA para as três rotas de health check expostas pela API CarWash:

- `GET /health` — liveness puro. `Predicate = _ => false`, ou seja, nenhuma checagem é executada. Retorna 200 sempre que o processo ASP.NET estiver respondendo.
- `GET /health/live` — liveness explícito. Idêntico a `/health` (`Predicate = _ => false`). Existe para orquestradores que esperam o sufixo `/live`.
- `GET /health/ready` — readiness. `Predicate = check => check.Tags.Contains("ready")`, executa o check `AddNpgSql` (nome `postgres`, tag `ready`). Falha quando o banco não está acessível.

Diferença essencial: `live` responde se a aplicação está viva (deve retornar 200 mesmo com o DB caído); `ready` responde se a aplicação está pronta para servir tráfego (depende do Postgres).

Configuração: `backend/src/CarWash.Api/Program.cs` linhas 149-176. Rotas anônimas (sem `Authorize`). Resposta padrão `text/plain` com corpo `Healthy`, `Degraded` ou `Unhealthy`.

## Pré-requisitos

- Backend up em `http://localhost:8080`.
- Container `carwash-postgres` controlável via docker compose:
  - Parar: `docker compose stop postgres`
  - Subir: `docker compose start postgres`
- `curl` instalado.
- Acesso aos logs Serilog do backend (console ou `logs/carwash-*.log` se habilitado).

Lembrete: sempre que parar o Postgres para um teste, subir de volta antes do próximo caso que dependa do banco.

## Tabela resumo

| ID  | Rota                | Pré-condição                       | Esperado                                       |
|-----|---------------------|------------------------------------|------------------------------------------------|
| T1  | `GET /health`       | Backend + Postgres up              | 200 / body `Healthy`                            |
| T2  | `GET /health/live`  | Backend up                         | 200 / body `Healthy`                            |
| T3  | `GET /health/ready` | Backend + Postgres up              | 200 / body `Healthy`                            |
| T4  | `GET /health/ready` | Postgres parado                    | 503 / body `Unhealthy` com detalhe `postgres`   |
| T5  | `GET /health`       | Postgres parado                    | 200 / body `Healthy` (independe do DB)          |
| T6  | `/health` e `/live` | Backend up                         | Latência < 50ms                                 |
| T7  | `/health/ready`     | Backend + Postgres up              | Latência < 500ms                                |
| T8  | Qualquer das 3      | Sem header `Authorization`         | 200 (rotas anônimas)                            |
| T9  | Qualquer das 3      | Backend up                         | `Content-Type: text/plain`                      |
| T10 | Qualquer das 3      | Backend up                         | Documentar `Cache-Control` retornado            |
| T11 | `/health/ready`     | 50 requisições paralelas           | Todas 200, sem esgotar pool                     |

## Detalhamento dos casos

### T1 — GET /health com backend funcional

```bash
curl -i http://localhost:8080/health
```

Esperado:

- Status `HTTP/1.1 200 OK`.
- `Content-Type: text/plain`.
- Body exatamente `Healthy`.

Logs: nenhuma entrada de erro Serilog. Não deve haver query SQL associada (rota não toca DB).

### T2 — GET /health/live

```bash
curl -i http://localhost:8080/health/live
```

Esperado:

- Status `200 OK`.
- Body `Healthy`.
- Comportamento idêntico a T1.

Logs: idem T1, sem acesso ao DB.

### T3 — GET /health/ready com Postgres up

```bash
curl -i http://localhost:8080/health/ready
```

Esperado:

- Status `200 OK`.
- Body `Healthy`.

Logs: pode aparecer log de execução do check `postgres` em nivel Debug/Information. Nenhum erro.

### T4 — GET /health/ready com Postgres down

```bash
docker compose stop postgres
curl -i http://localhost:8080/health/ready
docker compose start postgres
```

Esperado:

- Status `HTTP/1.1 503 Service Unavailable`.
- Body `Unhealthy` (ou contendo o nome `postgres` no detalhamento, dependendo do formato configurado).
- O body NÃO deve conter connection string, senha, host ou porta em texto plano.

Logs: Serilog deve registrar falha no check `postgres`, com a exception do Npgsql (timeout/conexão recusada). Verificar que a mensagem de log é informativa para o operador.

Importante: subir o Postgres de volta antes de prosseguir com os casos T5+ que pedem DB up.

### T5 — GET /health com Postgres down

```bash
docker compose stop postgres
curl -i http://localhost:8080/health
docker compose start postgres
```

Esperado:

- Status `200 OK`.
- Body `Healthy`.
- Liveness não depende do banco — confirma que `Predicate = _ => false` está aplicado.

Logs: não deve haver tentativa de conexão com Postgres por causa desta requisição.

### T6 — Latência de /health e /health/live

```bash
curl -o /dev/null -s -w "health: %{time_total}s\n" http://localhost:8080/health
curl -o /dev/null -s -w "live:   %{time_total}s\n" http://localhost:8080/health/live
```

Esperado: cada chamada `< 0.050s` (50ms) em ambiente local quente. Se latência consistentemente acima, suspeitar de middleware bloqueante ou logging síncrono.

### T7 — Latência de /health/ready

```bash
curl -o /dev/null -s -w "ready: %{time_total}s\n" http://localhost:8080/health/ready
```

Esperado: `< 0.500s` (500ms) com DB local. Latência muito acima pode indicar:

- Timeout configurado alto no `AddNpgSql`.
- Pool de conexões saturado.
- Query do check excessivamente custosa.

### T8 — Sem header Authorization

```bash
curl -i -H "Authorization:" http://localhost:8080/health
curl -i -H "Authorization:" http://localhost:8080/health/live
curl -i -H "Authorization:" http://localhost:8080/health/ready
```

Esperado: todas retornam 200 (rotas anônimas, sem `RequireAuthorization`).

### T9 — Content-Type

```bash
curl -sI http://localhost:8080/health | grep -i content-type
curl -sI http://localhost:8080/health/live | grep -i content-type
curl -sI http://localhost:8080/health/ready | grep -i content-type
```

Esperado: `Content-Type: text/plain` (charset pode aparecer). Caso retorne `application/json`, foi configurado um `ResponseWriter` customizado — atualizar este documento.

### T10 — Cache-Control

```bash
curl -sI http://localhost:8080/health | grep -i cache-control
curl -sI http://localhost:8080/health/ready | grep -i cache-control
```

Documentar exatamente o valor retornado (esperado pelo middleware default: `no-store, no-cache` ou ausente). Health checks NÃO devem ser cacheáveis — qualquer `max-age > 0` é bug.

### T11 — Carga: 50 requisições paralelas a /health/ready

```bash
seq 50 | xargs -P50 -I{} curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8080/health/ready | sort | uniq -c
```

Esperado:

- 50 linhas `200`.
- Nenhuma `500`, `503`, `504` ou timeout.
- Backend continua respondendo `/health` normalmente logo após.
- Pool do Npgsql não deve esgotar (sem exceção `The connection pool has been exhausted` nos logs).

Se houver falhas, capturar:

- Quantas e quais HTTP codes ocorreram.
- Stack trace do Serilog no momento.
- Métricas de pool (`Npgsql` expõe via `EventCounters`).

## Bugs e crashes a observar

- `/health/ready` retornar 200 com Postgres down: bug crítico. Quebra contrato com orquestradores (Kubernetes, Docker Swarm, load balancers) que usariam readiness para tirar o pod da rotação.
- `/health` ou `/health/live` retornar 500: pipeline ASP.NET com falha grave; provavelmente middleware antes do endpoint quebrando.
- `/health/live` checando o DB: configuração `Predicate` errada. Validar inspecionando o código em `Program.cs` linhas 149-176.
- Pool de conexões esgotado em T11: configuração de pool insuficiente ou check do Npgsql segurando conexão. Investigar `MaxPoolSize` da connection string e o timeout do `AddNpgSql`.
- Body expondo connection string, senha, host real ou nome de usuário do DB: vazamento de configuração sensível. Aplicar `ResponseWriter` que sanitize.
- Status 200 com body `Unhealthy` (ou inverso 503 com `Healthy`): inconsistência grave entre status code e payload.
- Latência crescente nas chamadas consecutivas: possível leak de conexão.

## Como reportar para o dev

Ao identificar bug, abrir issue contendo:

1. ID do caso (T1–T11) e título da rota.
2. Comando `curl` exato executado (copiar do documento).
3. Resposta completa: status line, headers, body (sanitizar segredos antes de colar).
4. Trecho do log Serilog correspondente, com timestamp.
5. Estado do container `carwash-postgres` no momento (`docker compose ps postgres`).
6. Versão do backend (commit hash de `git rev-parse HEAD`).
7. Severidade sugerida:
   - Crítico: T4 com 200, vazamento de credencial, crash em T1/T2.
   - Alto: T11 com falhas, latência > 10x do esperado.
   - Médio: `Cache-Control` permissivo, `Content-Type` divergente.
8. Referência ao arquivo `backend/src/CarWash.Api/Program.cs` linhas 149-176 quando o bug for de configuração de health check.

Anexar saída completa de T11 (output do `sort | uniq -c`) sempre que o bug for relacionado a carga.
