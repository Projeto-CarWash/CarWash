# Relatório Consolidado de QA — Backend CarWash (v2 — pós-fix BUG-001/002)

Data: 2026-05-17T15:20Z
Backend: `http://localhost:8080`
Migrations aplicadas: `20260513114525_InitialSchema`, `20260517022432_AddUsuarioLockoutFields`.
Rodada anterior: `QA/_relatorios/v1-pre-fix/`.

---

## TL;DR

- **PASS subiu de 33% (52/157) para 64% (100/157)** após fechar BUG-001 (schema lockout) e BUG-002 (RequireAuthorization).
- **Novo bloqueador**: migration `RefatoraClienteEndereco` está no FS mas **não aplicada** — derruba 26 casos de Clientes (POST/PUT/PATCH) e ~20 de Read com 500 (`column c.data_nascimento does not exist`).
- **Novo bug crítico de segurança (CA011)**: ao detectar reuse de refresh token, o backend revoga só o token consumido — **o token rotacionado da chamada legítima continua válido**. Atacante que exfiltrou o cookie mantém sessão viva.
- **Lockout dispara em 2 falhas** (esperado: 3) e a resposta 403 **não emite header HTTP `Retry-After`** (só no body) — violação de RNF005.
- **Escalada de privilégio confirmada**: POST usuário sem `perfil` cria **Admin** silenciosamente (201, persiste no banco — antes mascarado pelo BUG-001).
- **Auto-desativação do admin do seed**: PATCH `{id}/status` aceita admin desativando a si próprio sem qualquer trava (último admin pode ficar inativo).

---

## Sumário consolidado v1 → v2

| Área | v1 PASS | v2 PASS | Δ | v2 FAIL | v2 BLOCKED | v2 SKIP |
|---|---:|---:|---:|---:|---:|---:|
| Auth | 13/30 | **28/30** | +15 | 2 | 0 | 0 |
| Usuários | 19/41 | **36/41** | +17 | 2 | 0 | 0 |
| Clientes Read | 6/28 | 6/28 | 0 | 2 | 20 | 0 |
| Clientes Write | 5/47 | **21/47** | +16 | 0 | 26 | 0 |
| Health | 9/11 | 9/11 | 0 | 0 | 0 | 2 |
| **Totais** | **52 (33%)** | **100 (64%)** | **+48** | **6** | **48** | **2** |

---

## Bugs FECHADOS pela última iteração de dev ✅

| ID | Título | Confirmação |
|---|---|---|
| BUG-001 | Schema de lockout dessincronizado em `usuarios` | `\d usuarios` mostra `bloqueado_ate`, `tentativas_invalidas` + check |
| BUG-002 | `/api/v1/usuarios/*` aceitava anônimo | 3 endpoints sem token → 401 + `WWW-Authenticate: Bearer` |
| BUG-CR002 | `[Authorize]` antes do binding em listar clientes | Com token válido, `?ativo=xyz` → 400 limpo |

---

## Bugs CRÍTICOS abertos (bloqueiam release)

### BUG-009 — Migration `RefatoraClienteEndereco` pendente

- **Severidade**: CRÍTICA · bloqueia 100% dos writes e reads de `clientes`.
- **Sintoma**: arquivo `20260517061810_RefatoraClienteEndereco.cs` está em `backend/src/CarWash.Infrastructure/Persistence/Migrations/` mas não em `__ef_migrations_history`. Schema atual de `clientes` tem `endereco varchar(255)` + `observacoes text` e SEM `data_nascimento`/`endereco_*`. Entidade `Cliente.cs` espera as novas colunas.
- **Resposta**: 500 + `correlationId` em GET/POST/PUT/PATCH com `Npgsql.PostgresException 42703: column c.data_nascimento does not exist`.
- **Casos afetados**: ~46 entre Clientes Read e Write.
- **Fix**: `dotnet ef database update --project backend/src/CarWash.Infrastructure --startup-project backend/src/CarWash.Api` (ou via `db.Database.Migrate()` no startup em Development/Testing).
- **Hardening**: bloquear startup quando `GetPendingMigrationsAsync()` não estiver vazia em Dev/Test; mapear `Npgsql 42703` para um 503 com hint `database-schema-drift` no `ExceptionHandlingMiddleware`.

### BUG-008 — Família de refresh NÃO é revogada após reuse detectado (CA011)

- **Severidade**: CRÍTICA · segurança de sessão.
- **Sintoma**: ao reusar um cookie já consumido (R-T4 do refresh), o backend retorna 401 corretamente para o reuse, mas o **cookie rotacionado emitido na 1ª chamada continua válido** — uma 3ª chamada com esse cookie retorna 200.
- **Reprodução**:
  ```bash
  # login -> cookieA
  # /refresh com cookieA -> 200, recebe cookieB
  # /refresh com cookieA novamente -> 401 (reuse detectado, CORRETO)
  # /refresh com cookieB -> ESPERADO 401 (família revogada); OBTIDO 200
  ```
- **Impacto**: atacante que exfiltra um cookie mantém a sessão viva mesmo após detecção do reuse.
- **Fix**: no `RefreshHandler`, ao detectar reuse, marcar `revogado_em = NOW()` em **todas** as sessões da mesma família (`familia_id` ou cadeia `usuario_id` + `rotated_from`). Adicionar teste `[Trait("CA","011")]` exercitando 200/401/401.

### BUG-U009 — Auto-desativação do admin sem RN

- **Severidade**: ALTA · risco operacional.
- **Sintoma**: `PATCH /api/v1/usuarios/{adminId}/status` com `{"ativo":false}` desativa o próprio admin logado, mesmo sendo o único admin ativo. Retorna 200 OK. Token corrente segue válido até expirar.
- **Reprodução**:
  ```bash
  TOKEN=<admin@carwash.local>
  curl -X PATCH http://localhost:8080/api/v1/usuarios/00000000-0000-0000-0000-000000000001/status \
    -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
    --data '{"ativo":false}'
  # 200 OK; admin agora ativo=false; risco de perder acesso administrativo
  ```
- **Fix**: rejeitar com 409/422 quando:
  - `userIdAlvo == JwtClaims.sub` (auto-desativação) **OU**
  - `userIdAlvo` é o último admin com `ativo=true` (`SELECT COUNT(*) FROM usuarios WHERE perfil='ADMIN' AND ativo=true = 1`).
- **Question para PO**: confirmar a RN antes de implementar (impacto operacional).

---

## Bugs ALTOS abertos

### BUG-U003 — Escalada: `perfil` omitido cria Admin silenciosamente

- **Severidade**: ALTA · violação de menor privilégio.
- **Sintoma**: `CriarUsuarioCommand.Perfil` é `PerfilUsuario` (enum struct, não-nullable). Omitir o campo deserializa para `PerfilUsuario.Admin` (índice 0). Validator não exige presença. Retorna 201 com `perfil=Admin`.
- **Reprodução**:
  ```bash
  curl -X POST http://localhost:8080/api/v1/usuarios -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d '{"nome":"x","email":"y@z.w","senha":"Senha@1234"}'
  # 201; resposta tem "perfil":"Admin"
  ```
- **Fix**: trocar para `PerfilUsuario? Perfil` e `RuleFor(x => x.Perfil).NotNull().IsInEnum()`.

### BUG-U004 — PATCH `/usuarios/{id}/status` com body `{}` desativa silenciosamente

- **Severidade**: ALTA · perda de auditoria + risco de desativação acidental.
- **Sintoma**: `AlterarStatusUsuarioRequest.Ativo` é `bool` não-anulável; `{}` deserializa para `Ativo=false` e o handler executa UPDATE.
- **Fix**: trocar para `bool? Ativo` + `RuleFor(x => x.Ativo).NotNull()`.

### BUG-AUTH-Lockout — Lockout em 2 falhas + sem header `Retry-After`

- **Severidade**: ALTA · CA011/RNF005.
- **Sintoma A**: doc QA exige 401×3 → 403 na 4ª; observado 401×2 → 403 na 3ª. Limite efetivo de 2.
- **Sintoma B**: resposta 403 traz `retryAfterSeconds: 900` no body mas **não** emite o header HTTP `Retry-After: 900`. Doc exige header padrão.
- **Fix**: corrigir constante `LimiteTentativasInvalidas` no `LoginHandler` (deveria ser 3 — provável `>= 2` virou `>= 3` ou similar). Adicionar `Response.Headers.Append("Retry-After", retryAfterSeconds.ToString())` no caminho de `UsuarioBloqueadoException` (provavelmente no `ExceptionHandlingMiddleware`).

### BUG-010 — `GET /clientes/{id}` inexistente retorna 500 em vez de 404

- **Severidade**: ALTA · revelada pelo BUG-009, mas indica fragilidade do design.
- **Fix esperado**: pode sumir junto com BUG-009 (após migration, `FirstOrDefaultAsync` retorna null e controller responde 404). Validar pós-migration.

---

## Bugs MÉDIOS/BAIXOS abertos

### BUG-005 — `/auth/refresh` 401 sem `Cache-Control: no-store` (MÉDIO)

- 401 do `/refresh` não emite `no-store`. 200 emite corretamente. Logout emite corretamente.
- **Fix**: mover `Response.Headers.CacheControl = "no-store"` para antes do `throw` no endpoint, ou middleware específico `/api/v1/auth/*`.

### BUG-007 — Log `Logout efetuado. UsuarioId=null` mesmo com cookie válido (MÉDIO · regressão de observabilidade)

- Todos os 6 logs `Logout efetuado` capturados aparecem com `UsuarioId=null`, inclusive em casos com cookie válido e sessão sendo revogada (CA011 L-T5 confirma que o backend sabe correlacionar a sessão para revogar — mas o log não usa).
- **Fix**: mover o log para após `sessao.UsuarioId` ser resolvido no `LogoutHandler`. Condicionar a emissão à existência de sessão.

### BUG-006 / BUG-U006 — ProblemDetails `title: "Identificador inválido"` para erros de body (BAIXO · UX)

- Body com enum desconhecido, null em campo não-nullable, JSON malformado, ou Guid inválido no path retornam `title: "Identificador inválido."` com `errors.request` vazando nome do parâmetro C# (`"CriarUsuarioCommand command"`).
- **Fix**: customizar handler para diferenciar body vs path; usar `errors.body` ou `errors.<campo>`; sem nome de parâmetro C#.

### BUG-U007 — POST body `{}` omite `perfil` em `errors` (BAIXO)

- Mesma raiz que BUG-U003. Fix conjunto.

---

## Gaps catalogados ainda BLOCKED (Clientes — dependem do BUG-009)

| ID | Origem | Risco |
|---|---|---|
| GAP-CW-CLI-EMAIL-1 | POST T11 | `ClienteService.CriarAsync` não valida unicidade de email |
| GAP-CW-CLI-PUT-CPF | PUT T8 | `UpdateClienteRequest` ignora `cpf`/`cnpj` silenciosamente |
| GAP-CW-CLI-PUT-EML | PUT T9 | `AtualizarAsync` não valida unicidade de email |
| GAP-CW-CLI-AUDIT | PUT T12, PATCH T13 | `_ = usuarioId;` confirmado em código; entidade sem `AtualizadoPorUsuarioId` |
| GAP-CW-CLI-STA-EMP | PATCH T10 | body `{}` desativa silenciosamente (mesma causa do BUG-U004) |
| GAP-CW-CLI-STA-AGD | PATCH T12 | sem RN bloqueando desativação com agendamentos abertos |
| BUG-LGPD-CLI | GET listar T17, GET/{id} T10 | CPF/CNPJ em claro em response — LGPD |
| BUG-TENANT-CLI | GET/{id} T8 | sem filtro de tenant |
| BUG-CACHE-PII | GET/{id} T9 | sem `Cache-Control: no-store` para PII |
| GAP-PAG-0 | GET listar T9 | `pagina<=0` normaliza silencioso |
| GAP-CLAMP | GET listar T10 | `tamanhoPagina` clampa mas JSON reflete original |
| GAP-UNACCENT | GET listar T15 | sem `unaccent` na busca |
| Sub-issue UF | POST T17 | validator aceita `uf:"sp"` (deveria exigir maiúscula) |

---

## Detalhe compacto por endpoint

### Auth (PASS=28, FAIL=2, BLOCKED=0)

| Endpoint | v1 → v2 PASS | Bugs ativos |
|---|---:|---|
| POST /login (13) | 4 → 12 | T9 FAIL: BUG-Auth-Lockout |
| POST /refresh (10) | 3 → 9 | T2 FAIL: BUG-005; mais BUG-008 em T4 |
| POST /logout (7) | 6 → 7 | BUG-007 em todos |

### Usuários (PASS=36, FAIL=2)

| Endpoint | v1 → v2 PASS | Bugs ativos |
|---|---:|---|
| POST (16) | 10 → 15 | T8b FAIL: BUG-U003 |
| GET /{id} (11) | 2 → 11 | BUG-U006 em T3/T8 |
| PATCH status (14) | 4 → 13 | T7 FAIL: BUG-U004; T11 risco: BUG-U009 |

### Clientes Read (PASS=6, FAIL=2, BLOCKED=20)

| Endpoint | v1 → v2 PASS | Bugs ativos |
|---|---:|---|
| GET listar (17) | 3 → 3 (T8 novo PASS) | BUG-009 bloqueia 14 |
| GET /{id} (11) | 3 → 3 | BUG-009 + BUG-010 (T2, T4) |

### Clientes Write (PASS=21, FAIL=0, BLOCKED=26)

| Endpoint | v1 → v2 PASS | Bugs ativos |
|---|---:|---|
| POST (20) | 1 → 12 | BUG-009 bloqueia 8 |
| PUT (13) | 2 → 5 | BUG-009 bloqueia 8 |
| PATCH status (14) | 2 → 5 | BUG-009 bloqueia 9 |

### Health (PASS=9, SKIP=2)

| Endpoint | v1 → v2 | Notas |
|---|---|---|
| /health, /live, /ready (11) | 9 → 9 | T4/T5 SKIP (exigem DB down) |

---

## Próximos passos priorizados

### Prioridade 1 — destravar release (~1h)
1. **BUG-009**: aplicar `20260517061810_RefatoraClienteEndereco`. Re-rodar Clientes (esperado: 46 BLOCKED → maioria PASS, restando os gaps catalogados como FAIL).
2. **BUG-008**: revogar família refresh em reuse (CA011 crítico).
3. **BUG-U009**: bloquear auto-desativação e último admin (impedir lockout administrativo).

### Prioridade 2 — segurança/validação (2-4h)
4. **BUG-U003**: `Perfil` nullable + validator `NotNull`.
5. **BUG-U004**: `Ativo` nullable + validator `NotNull` (usuário status; e estender ao status de cliente após migration).
6. **BUG-Auth-Lockout**: limite `>= 3` (não 2) + header HTTP `Retry-After`.
7. **BUG-005**: `Cache-Control: no-store` em todos os caminhos do `/refresh`.

### Prioridade 3 — polimentos (1-2h)
8. **BUG-006**: customizar handler de deserialization (`title` + `errors` mais informativos).
9. **BUG-007**: corrigir log de logout para usar `UsuarioId` da sessão e condicionar emissão.
10. **Sub-issue UF**: normalizar para uppercase no validator.

### Prioridade 4 — gaps de Clientes (sprint seguinte, após BUG-009 fechar)
11. Abrir issues separadas para os 12 gaps catalogados (LGPD, audit, tenant, cache PII, paginação, unaccent, validações).

### Prioridade 5 — defesa em profundidade
12. Adicionar `SchemaConsistencyTests` que falha o CI quando `GetPendingMigrationsAsync()` não está vazia.
13. Cobrir todos os bugs corrigidos com testes `[Trait("CA","011")]` em `WebApplicationFactory` + Testcontainers.
14. Executar T4/T5 de Health em janela isolada (não nesta bateria paralela).

---

## Apêndice — Schema atual de `clientes` (drift confirmado)

```
nome, cpf, cnpj, telefone, celular (NULL — deveria NOT NULL),
email, endereco varchar(255) (deveria DROPPED),
observacoes text (deveria DROPPED),
ativo, criado_em, atualizado_em
```
Faltam: `data_nascimento date NOT NULL`, `endereco_cep`, `endereco_logradouro`, `endereco_numero`, `endereco_complemento`, `endereco_bairro`, `endereco_cidade`, `endereco_uf`.

## Apêndice — Migrations aplicadas (`__ef_migrations_history`)
```
20260513114525_InitialSchema
20260517022432_AddUsuarioLockoutFields
```
Pendente: `20260517061810_RefatoraClienteEndereco`.

---

**Status final v2**: release ainda **bloqueado**. Próxima ação owner: `dev-dotnet-carwash` (P1+P2). Em paralelo: `po-pm-carwash` para confirmar RN de BUG-U009.
