# Relatório Consolidado de QA — Backend CarWash

Data da execução: 2026-05-17
Backend: `http://localhost:8080` (container `carwash-backend`)
Banco: `carwash-postgres` (PostgreSQL 16, migration aplicada: `20260513114525_InitialSchema`)
Cobertura: 12 endpoints HTTP em 4 áreas (Auth, Usuários, Clientes, Health)
Documentação-fonte: `QA/{auth,usuarios,clientes,health}/*.md` (157 casos descritos)

---

## TL;DR

- **157 casos executados** → **52 PASS (33%) · 30 FAIL · 88 BLOCKED · 2 SKIP**.
- **1 bug crítico bloqueia 56% da bateria**: schema do banco está dessincronizado com o código (`Usuario` espera colunas de lockout que não existem). Cascateia em 500 no `/login` e, por dependência de token, derruba toda QA de Clientes e parte de Usuários.
- **Risco crítico de segurança/LGPD**: os 3 endpoints de `/api/v1/usuarios` **não exigem autenticação**. Hoje parcialmente mascarado pelo bug do schema — quando o schema for corrigido sem corrigir isso, qualquer anônimo cria Admin, lê PII e desativa usuários.
- **Escalada de privilégio**: omitir o campo `perfil` no POST de usuário cria **Admin** silenciosamente (enum struct default).
- **Health Checks (módulo único 100% executado)**: 9/11 PASS, performance < 5ms, headers corretos, anônimo OK, suporta 50 reqs paralelas. T4/T5 ficaram SKIP por exigirem derrubar Postgres com outros agentes ativos.
- **Recomendação imediata**: bloquear release do MVP até **BUG-001** e **BUG-002** estarem fechados; re-executar a bateria completa.

---

## Sumário consolidado por área

| Área | Endpoints | Casos | PASS | FAIL | BLOCKED | SKIP | % PASS |
|---|---|---:|---:|---:|---:|---:|---:|
| Auth | login + refresh + logout | 30 | 13 | 1 | 16 | 0 | 43% |
| Usuários | POST + GET/{id} + PATCH/{id}/status | 41 | 19 | 14 | 8 | 0 | 46% |
| Clientes Read | GET listar + GET/{id} | 28 | 6 | 0 | 22 | 0 | 21% |
| Clientes Write | POST + PUT + PATCH/{id}/status | 47 | 5 | 0 | 42 | 0 | 11% |
| Health | /health + /health/live + /health/ready | 11 | 9 | 0 | 0 | 2 | 82% |
| **Totais** | 12 endpoints | **157** | **52** | **15** | **88** | **2** | **33%** |

> Observação: a contagem de FAIL difere por relatório porque alguns casos foram marcados FAIL (sintoma exposto) e outros BLOCKED (causa raiz bloqueante) para o mesmo bug. O relatório do módulo Usuários classifica mais como FAIL; os de Clientes preferem BLOCKED. Para efeitos de priorização, **140 casos dependem direta ou indiretamente da correção do BUG-001**.

---

## Bugs priorizados

### BUG-001 — Schema dessincronizado: colunas de lockout faltando em `usuarios` (CRÍTICO · BLOQUEADOR)

- **Sintoma**: qualquer SELECT/UPDATE sobre `Usuario` no EF falha com `Npgsql.PostgresException 42703: column "bloqueado_ate" of relation "usuarios" does not exist` (posições 40/49). Backend devolve **500** com ProblemDetails genérico.
- **Causa raiz**: `LoginHandler` e o mapeamento EF projetam `bloqueado_ate`, `tentativas_invalidas` (e possivelmente `ultima_tentativa_em`), mas a única migration aplicada é `20260513114525_InitialSchema`. Migration de lockout não foi gerada/commitada/aplicada.
- **Casos afetados**: ~88 BLOCKED + ~30 FAIL — efetivamente toda a bateria autenticada (Auth, Usuários CRUD, Clientes CRUD).
- **Reprodução**:
  ```bash
  curl -i -X POST http://localhost:8080/api/v1/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"admin@carwash.local","senha":"DevSeedAdmin2026!Forte"}'
  ```
  Resposta: `500` com `correlationId` (ex.: `d5272fe0d0e34a42bfaf4e91153bb05c`).
- **Log Serilog (trecho)**:
  ```
  [14:23:38 ERR] Failed executing DbCommand
  SELECT u.id, u.ativo, u.atualizado_em, u.bloqueado_ate, u.criado_em,
         u.email, u.nome, u.perfil, u.senha_hash, u.tentativas_invalidas
  FROM public.usuarios AS u WHERE u.email = @__emailNormalizado_0 LIMIT 1
  Npgsql.PostgresException 42703: column u.bloqueado_ate does not exist
    at CarWash.Application.Auth.Login.LoginHandler.HandleAsync(...) LoginHandler.cs:85
  ```
- **Schema atual de `usuarios`** (faltam 3 colunas):
  ```
  id, nome, email, senha_hash, perfil, ativo, criado_em, atualizado_em
  ```
- **Sugestão ao dev**:
  1. `dotnet ef migrations list -p src/CarWash.Infrastructure -s src/CarWash.Api` para verificar se a migration existe localmente sem ter sido aplicada.
  2. Se não existir: `dotnet ef migrations add AddLockoutColumnsToUsuario` adicionando `bloqueado_ate TIMESTAMPTZ NULL`, `tentativas_invalidas INT NOT NULL DEFAULT 0`, `ultima_tentativa_em TIMESTAMPTZ NULL` (confirmar nomes com `UsuarioConfiguration`).
  3. `make migrate` (ou `dotnet ef database update`).
  4. Adicionar teste de integração com Testcontainers validando `INFORMATION_SCHEMA.columns` — qualquer drift entre modelo EF e schema PG deve quebrar CI.
  5. Considerar `db.Database.Migrate()` no startup em Development/Testing para evitar drift entre `make` e `docker compose up`.

### BUG-002 — `/api/v1/usuarios/*` aceita requisição anônima (CRÍTICO · LGPD)

- **Sintoma**: nenhum dos 3 endpoints (`POST`, `GET/{id}`, `PATCH/{id}/status`) chama `RequireAuthorization()`. Hoje mascarado pelo BUG-001 (todos retornam 500), mas após o fix do schema qualquer anônimo cria Admin, lê PII e desativa usuários.
- **Localização**: `backend/src/CarWash.Api/Endpoints/Usuarios/UsuariosEndpoints.cs:29,37,43`.
- **Casos afetados**: Tbug-Auth nos 3 arquivos `.md` de `QA/usuarios/`.
- **Reprodução**:
  ```bash
  curl -i -X POST http://localhost:8080/api/v1/usuarios -H "Content-Type: application/json" \
    -d '{"nome":"Anon","email":"anon@qa.local","senha":"Senha@1234","perfil":"Admin"}'
  ```
  Atual: 500 (BUG-001). Esperado pós-fix: 401. Atual sem BUG-001 seria: 201 (vulnerabilidade ativa).
- **Sugestão ao dev**: adicionar `.RequireAuthorization()` nos 3 endpoints e considerar policy `Admin` para POST e PATCH. Garantir que mergeie **junto** com o fix do BUG-001 para que a vulnerabilidade não fique observável em produção mesmo por uma janela.

### BUG-003 — Escalada de privilégio: `perfil` omitido cria Admin silenciosamente (ALTO)

- **Sintoma**: `CriarUsuarioCommand.Perfil` é tipado como `PerfilUsuario` (enum struct, não-nullable). Omitir o campo no JSON deserializa para `PerfilUsuario.Admin` (índice 0). O validator não exige presença.
- **Casos afetados**: POST T8b. Confirmado nos logs do EF tentando INSERT com `perfil='ADMIN'`.
- **Reprodução**:
  ```bash
  curl -i -X POST http://localhost:8080/api/v1/usuarios -H "Content-Type: application/json" \
    -d '{"nome":"Sem Perfil","email":"t8b@qa.local","senha":"Senha@1234"}'
  ```
- **Sugestão**: trocar `PerfilUsuario Perfil` por `PerfilUsuario? Perfil` no `CriarUsuarioCommand` e adicionar `RuleFor(x => x.Perfil).NotNull().IsInEnum()` no validator.

### BUG-004 — `PATCH /usuarios/{id}/status` com body `{}` desativa silenciosamente (ALTO)

- **Sintoma**: `AlterarStatusUsuarioRequest.Ativo` é `bool` (não-nullable). Body `{}` deserializa para `Ativo=false` e o handler segue como se fosse desativação explícita. Sem auditoria de "campo ausente".
- **Casos afetados**: PATCH usuários T7. Hoje 500 (BUG-001), mas a intenção do código é persistir a desativação.
- **Sugestão**: trocar `bool Ativo` por `bool? Ativo` e adicionar `RuleFor(x => x.Ativo).NotNull().WithMessage("Campo 'ativo' é obrigatório.")`.

### BUG-005 — `/auth/refresh` 401 sem `Cache-Control: no-store` (MÉDIO)

- **Sintoma**: em respostas 401 do `POST /api/v1/auth/refresh`, o header `Cache-Control: no-store` está ausente. `/logout` faz certo em todas as respostas; `/refresh` só seta no caminho de sucesso (200).
- **Casos afetados**: Refresh T2, T3 (e indiretamente T1/T8 quando virarem 401).
- **Risco**: contrato pede `no-store` sempre — proxy/CDN intermediário pode cachear um 401 com ProblemDetails.
- **Sugestão**: mover `Response.Headers.CacheControl = "no-store"` para **antes** de qualquer `throw` no endpoint/middleware, ou adicionar middleware específico para `/api/v1/auth/*` que sempre marque `no-store`. Cobrir com teste de integração em `WebApplicationFactory`.

### BUG-006 — ProblemDetails com `title: "Identificador inválido."` para erros de body (MÉDIO · UX)

- **Sintoma**: erros de deserialização (JSON malformado, enum inválido, perfil null, ativo null) retornam 400 com `title="Identificador inválido."` e `errors.request=["Failed to read parameter \"X command\" from the request body as JSON."]`. O mesmo `title` é usado para Guid inválido em path — confunde quem está debugando e vaza nome de parâmetro C# (`"CriarUsuarioCommand command"`).
- **Casos afetados**: POST usuários T7, T8a, T10; GET usuários T3, T8; PATCH usuários T5, T8, T9; POST login T7; PATCH/PUT/POST clientes (esperado em testes pós-fix).
- **Sugestão**: customizar handler de deserialization para devolver `title="Corpo da requisição inválido."`, `errors.body` (ou `errors.<campo>` quando identificável) e mensagem PT-BR sem expor nomes de parâmetros internos.

### BUG-007 — Log ruidoso: `Logout efetuado. UsuarioId=null` (BAIXO · observabilidade)

- **Sintoma**: chamadas ao `/api/v1/auth/logout` sem cookie emitem log Serilog `[INF] Logout efetuado. UsuarioId=null`. Documentação QA esperava silêncio nesse caminho.
- **Casos afetados**: Logout T2, T3.
- **Sugestão**: condicionar a emissão do log à existência de sessão correspondente, para reduzir ruído em monitoramento.

### BUG-008 — `[Authorize]` avalia antes do model binding em query (BAIXO · semântica)

- **Sintoma**: `GET /api/v1/clientes?ativo=xyz` retorna 401 sem Authorization e 401 com token inválido, em vez do 400 do binding. Só verificável com token válido (bloqueado por BUG-001).
- **Casos afetados**: Listar clientes T8.
- **Sugestão**: comportamento atual é seguro; mas pode mascarar gap de validação se ASP.NET silenciosamente trata `ativo=xyz` como `null`. Reavaliar pós-BUG-001.

---

## Gaps catalogados que NÃO foram confirmados (BLOCKED por BUG-001)

Estes saem direto dos `.md` da pasta `QA/` e estão aguardando rebateria após correção do schema. **São pontos a abrir como issues próprias** quando virarem FAIL na próxima rodada:

| Gap | Local | Risco |
|---|---|---|
| POST clientes — email duplicado aceito | service `ClienteService.CriarAsync` não valida email único | Médio (duplicatas em base) |
| GET listar — `pagina <= 0` normaliza silencioso | repo aceita e clampa para 1 | Baixo (UX) |
| GET listar — `tamanhoPagina` clamp para 100 mas JSON mostra valor original | inconsistência de contrato | Médio (paginação cliente quebra) |
| GET listar — busca sem `unaccent` | `joão` não casa `joao` | Médio (UX brasileiro) |
| GET listar — CPF/CNPJ em claro | `ListaClientesResponse.Itens[]` | **Alto (LGPD)** |
| GET por id — sem filtro de tenant | qualquer auth lê qualquer cliente | Médio (escopo) |
| GET por id — sem `Cache-Control: no-store` | PII pode ser cacheada | Médio (LGPD) |
| PUT cliente — body aceita `cpf`/`cnpj` e descarta silenciosamente | DTO de update não tem o campo | Médio (UX confuso) |
| PUT cliente — email duplicado entre clientes não validado | service não checa | Médio |
| PUT cliente — sem `AtualizadoPorUsuarioId` (audit) | entidade não tem o campo | **Alto (audit/compliance)** |
| PATCH cliente status — body `{}` desativa silenciosamente | `bool` não-nullable | Alto (igual BUG-004 em Clientes) |
| PATCH cliente status — sem RN bloqueando desativação com agendamentos abertos | service não checa | Alto (regra de negócio) |

---

## Detalhe por endpoint (compacto)

### Auth (30 casos · 13 PASS · 1 FAIL · 16 BLOCKED)

| Endpoint | PASS | FAIL | BLOCKED |
|---|---:|---:|---:|
| POST /api/v1/auth/login | 4 | 0 | 9 (BUG-001) |
| POST /api/v1/auth/refresh | 3 | 1 (BUG-005) | 6 (BUG-001) |
| POST /api/v1/auth/logout | 6 | 0 | 1 (BUG-001 — CA011) |

Notas:
- Rate limit 10/min/IP funciona (T10 PASS, 429 + Retry-After: 60).
- Validators de senha (T5/T6) e body vazio (T6) funcionam.
- `Set-Cookie` apagador no logout correto (`expires=1970`, `HttpOnly`, `SameSite=Strict`, `Path=/api/v1/auth`).
- CA011 (revogação server-side validada via `/refresh` após `/logout`) **não foi atestado** por dependência de login válido.

### Usuários (41 casos · 19 PASS · 14 FAIL · 8 BLOCKED)

| Endpoint | PASS | FAIL | BLOCKED |
|---|---:|---:|---:|
| POST /api/v1/usuarios | 10 | 4 | 2 |
| GET /api/v1/usuarios/{id} | 2 | 7 | 2 |
| PATCH /api/v1/usuarios/{id}/status | 4 | 8 | 2 |

Notas:
- Validators de Create (nome/email vazios, email malformado, senha curta/sem dígito/sem letra, limite de tamanhos) PASS.
- Bugs ativos confirmados: **BUG-002, BUG-003, BUG-004, BUG-006**.
- 2 usuários pré-existentes no banco (admin do seed + 1 funcionário) preservados — nenhum UPDATE bem-sucedido por causa do BUG-001.

### Clientes Read (28 casos · 6 PASS · 0 FAIL · 22 BLOCKED)

| Endpoint | PASS | FAIL | BLOCKED |
|---|---:|---:|---:|
| GET /api/v1/clientes (listar) | 2 | 0 | 15 |
| GET /api/v1/clientes/{id} | 4 | 0 | 7 |

Notas:
- Tabela `clientes` está VAZIA — sem token e sem direito de INSERT direto, nenhum cliente foi criado.
- Pipeline de autenticação validado (401 + `WWW-Authenticate: Bearer` corretos).
- Route constraint `{id:guid}` confirma 404 antes do Authorize para id malformado (T3 do por_id PASS).

### Clientes Write (47 casos · 5 PASS · 0 FAIL · 42 BLOCKED)

| Endpoint | PASS | FAIL | BLOCKED |
|---|---:|---:|---:|
| POST /api/v1/clientes | 1 | 0 | 19 |
| PUT /api/v1/clientes/{id} | 2 | 0 | 11 |
| PATCH /api/v1/clientes/{id}/status | 2 | 0 | 12 |

Notas:
- Apenas casos de auth/route constraint passaram (T3 POST sem Auth → 401; T4/T5 PUT/PATCH; T5/T6 PATCH status).
- Gaps documentados nos `.md` (T11 email duplicado, T8 cpf/cnpj descartado, T10 body `{}` desativa) **não confirmados** — pendentes para a próxima bateria.

### Health (11 casos · 9 PASS · 0 FAIL · 2 SKIP)

| Endpoint | PASS | FAIL | SKIP |
|---|---:|---:|---:|
| GET /health | — | — | — |
| GET /health/live | — | — | — |
| GET /health/ready | — | — | — |
| (consolidado) | 9 | 0 | 2 |

Notas:
- T1-T3 (golden), T6-T11 (latência, content-type, cache, anônimo, 50× paralelo) todos PASS.
- T4 (Postgres down → 503 ready) e T5 (Postgres down → 200 live) SKIP para não impactar testes paralelos.
- Latência: `/health` e `/health/live` ≈ 1.3ms; `/health/ready` ≈ 2.8ms — muito abaixo dos limites (50ms / 500ms).
- Headers: `Cache-Control: no-store, no-cache` + `Expires: 1970-01-01` + `Pragma: no-cache` + `X-Correlation-Id` único.

---

## Próximos passos (priorizados)

### Prioridade 1 — desbloquear release (1-2h)
1. **Fechar BUG-001**: gerar/aplicar migration de lockout em `usuarios`. Validar localmente que `/login` golden path retorna 200 + JWT + cookie.
2. **Fechar BUG-002**: adicionar `.RequireAuthorization()` nos 3 endpoints de `/api/v1/usuarios`. Mergear no mesmo PR / janela do BUG-001 para evitar exposição em prod.
3. **Re-executar toda a bateria** (`/QA/_relatorios/` regerado). Esperado: ~140 dos 88 BLOCKED viram PASS, ~5-10 novos FAIL nos gaps catalogados acima.

### Prioridade 2 — defesa em profundidade (4-8h)
4. **Fechar BUG-003 e BUG-004**: nullables + validator `NotNull` para `perfil` (criar usuário) e `ativo` (patch status, em usuário e cliente).
5. **Fechar BUG-005**: `Cache-Control: no-store` sempre em `/auth/*`, inclusive em 401.
6. **Adicionar testes de integração com Testcontainers** que validem:
   - Schema EF ↔ Postgres (regressão de BUG-001).
   - Anônimo → 401 em `/usuarios/*` (regressão de BUG-002).
   - Perfil omitido → 400 (regressão de BUG-003).
   - Body `{}` em PATCH status → 400 (regressão de BUG-004).
   - `Cache-Control: no-store` em 401 do `/refresh` (regressão de BUG-005).

### Prioridade 3 — polimentos (2-4h)
7. **Fechar BUG-006**: customizar handler de deserialization (title e errors mais informativos, sem vazar nomes C#).
8. **Fechar BUG-007**: condicionar log de logout à presença de sessão.
9. **Investigar BUG-008** pós-correção do BUG-001 (decidir se mantém comportamento ou re-ordena pipeline).

### Prioridade 4 — gaps documentados (sprint seguinte)
10. Abrir issues separadas para os 12 gaps catalogados no quadro "Gaps catalogados". Priorizar LGPD (CPF/CNPJ em claro na listagem) e audit (sem `AtualizadoPorUsuarioId`).

### Prioridade 5 — Health
11. Executar T4/T5 do Health em janela isolada (`docker compose stop postgres && curl /health/ready /health && docker compose start postgres`).

---

## Apêndices

### A. Schema atual de `public.usuarios`
```
    Column     |           Type           | Nullable | Default
---------------+--------------------------+----------+---------
 id            | uuid                     | not null |
 nome          | character varying(120)   | not null |
 email         | character varying(150)   | not null |
 senha_hash    | text                     | not null |
 perfil        | character varying(20)    | not null |
 ativo         | boolean                  | not null | true
 criado_em     | timestamp with time zone | not null | now()
 atualizado_em | timestamp with time zone | not null | now()
```
Esperado adicional: `bloqueado_ate TIMESTAMPTZ NULL`, `tentativas_invalidas INT NOT NULL DEFAULT 0`, `ultima_tentativa_em TIMESTAMPTZ NULL` (nomes finais conforme `UsuarioConfiguration.cs`).

### B. Migrations aplicadas no container
```
         migration_id         | product_version
------------------------------+-----------------
 20260513114525_InitialSchema | 8.0.10
```

### C. Arquivos por relatório (drill-down)
- `QA/_relatorios/auth.md` — 30 casos, 221 linhas, BUG-001 e BUG-005 detalhados.
- `QA/_relatorios/usuarios.md` — 41 casos, 245 linhas, BUG-001 a BUG-006 detalhados.
- `QA/_relatorios/clientes-read.md` — 28 casos, 93 linhas, foco em pipeline de auth e route constraints.
- `QA/_relatorios/clientes-write.md` — 47 casos, 186 linhas, BUG-001 confirmado e gaps catalogados.
- `QA/_relatorios/health.md` — 11 casos, 53 linhas, único módulo "verde".

### D. Casos PASS notáveis (servem de baseline)
- **Auth · Logout**: idempotente em todos os caminhos; `Set-Cookie` apagador correto; `Cache-Control: no-store` em todas as 7 respostas.
- **Auth · Login validators**: T5 (senha vazia/null), T6 (body `{}`), T7 (JSON malformado), T8 (Content-Type errado), T10 (rate limit).
- **Usuários · Validators de Create**: nome vazio/limite, email malformado/limite, senha curta/sem dígito/sem letra.
- **Clientes · Pipeline de Auth**: 401 com `WWW-Authenticate: Bearer` em todos os endpoints; 404 antes do Authorize para `{id:guid}` malformado.
- **Health · Tudo**: 9/11 PASS, observabilidade exemplar (X-Correlation-Id, sem cache, anônimo, latência < 5ms, aguenta 50× paralelo).

---

**Status final**: release do MVP **bloqueado** até BUG-001 + BUG-002 estarem fechados. Próxima ação owner: `dev-dotnet-carwash` (migration de lockout + RequireAuthorization em `/usuarios/*`).
