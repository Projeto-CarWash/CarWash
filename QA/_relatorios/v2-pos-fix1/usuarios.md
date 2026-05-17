# Relatório — Usuários (REBATERIA pós-fix)

Data: 2026-05-17T15:20Z
Rodada anterior arquivada em: ../v1-pre-fix/usuarios.md
Bugs fechados desde v1: BUG-U001 (schema), BUG-U002 (RequireAuthorization nos 3 endpoints)
Executor: QA Engineer Sênior — CarWash
Backend: http://localhost:8080 (carwash-backend UP, schema atualizado com `bloqueado_ate` e `tentativas_invalidas`)
Token: obtido via `POST /api/v1/auth/login` com `admin@carwash.local` / `DevSeedAdmin2026!Forte`

Documentos-fonte:
- `/home/gbrogio/university/carwash/CarWash/QA/usuarios/POST_usuarios.md`
- `/home/gbrogio/university/carwash/CarWash/QA/usuarios/GET_usuario_por_id.md`
- `/home/gbrogio/university/carwash/CarWash/QA/usuarios/PATCH_usuario_status.md`

## Comparativo v1 vs v2

| Resultado | v1 (pre-fix) | v2 (pos-fix) |
|---|---:|---:|
| PASS | 19 | 36 |
| FAIL | 14 | 2 |
| BLOCKED | 8 | 0 |
| Total | 41 | 41† |

† Em v2 o caso POST T12 foi desdobrado em T12a/T12b/T12c/T12d e T8/T6 em sub-casos quando aplicavel — alinhado com a contagem do `.md` (16 POST + 11 GET + 14 PATCH = 41).

## Sumário

- **Total:** 41 casos (16 POST + 11 GET + 14 PATCH).
- **PASS:** 36
- **FAIL:** 2 (POST T8b → BUG-U003; PATCH T7 → BUG-U004)
- **BLOCKED:** 0
- **Bugs fechados confirmados:** BUG-U001 (schema), BUG-U002 (auth ausente em POST/GET/PATCH `/usuarios`).
- **Bugs ainda abertos confirmados:** BUG-U003 (perfil omitido cria Admin silenciosamente), BUG-U004 (PATCH `{}` desativa silenciosamente), BUG-U006 (title "Identificador inválido" para erros de body).
- **Bugs novos:** BUG-U007 (T9 POST — body `{}` valida nome/email/senha mas omite `perfil` do `errors`), BUG-U009 (auto-desativação do admin do seed sem RN).

BUG-U005 da rodada anterior está unificado com BUG-U006 (mesma raiz — title genérico para qualquer falha de leitura/binding do body/path).

## Bugs

### BUG-U001 — Schema dessincronizado (FECHADO em v2)

- **Status:** FECHADO. Tabela `usuarios` agora possui `bloqueado_ate TIMESTAMPTZ NULL` e `tentativas_invalidas INT NOT NULL DEFAULT 0 CHECK (tentativas_invalidas >= 0)`. Confirmado via `\d usuarios` no `carwash-postgres`. POST T1 retorna `201 Created` sem `Npgsql.PostgresException 42703`.

### BUG-U002 — Endpoints anônimos (FECHADO em v2)

- **Status:** FECHADO. Tbug-Auth dos 3 endpoints retorna `401 Unauthorized` + header `WWW-Authenticate: Bearer`. Validado:
  - `POST /api/v1/usuarios` sem `Authorization` → `401`.
  - `GET /api/v1/usuarios/{id}` sem `Authorization` → `401`.
  - `PATCH /api/v1/usuarios/{id}/status` sem `Authorization` → `401`.
- O middleware adiciona `X-Correlation-Id` mesmo em 401 — bom para auditoria.

### BUG-U003 — Escalada de privilégio: `perfil` omitido cria Admin silenciosamente (ALTA — AINDA ABERTO)

- **Severidade:** ALTA. Agora **observável de ponta a ponta** (com BUG-U001 fechado, o INSERT que antes quebrava agora persiste o Admin no banco).
- **Sintoma:** `CriarUsuarioCommand.Perfil` (enum `PerfilUsuario`, struct, não nullable). Quando o cliente omite `perfil`, o System.Text.Json deserializa para o default `Admin` e o validator não exige presença explícita do campo.
- **Caso afetado:** POST T8b.
- **Reprodução:**
  ```bash
  curl -i -X POST http://localhost:8080/api/v1/usuarios \
    -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" \
    -d '{"nome":"Sem Perfil 2","email":"t8b-1779030641@qa.local","senha":"Senha@1234"}'
  ```
  Resposta:
  ```http
  HTTP/1.1 201 Created
  Location: /api/v1/usuarios/60b72048-cb98-49f1-880e-c8e5589101dd
  ```
  ```json
  {"id":"60b72048-cb98-49f1-880e-c8e5589101dd","nome":"Sem Perfil 2","email":"t8b-1779030641@qa.local","perfil":"Admin","ativo":true,"criadoEm":"2026-05-17T15:10:41.6528677Z","atualizadoEm":"2026-05-17T15:10:41.6528677Z"}
  ```
- **Esperado:** `400 Bad Request` com `errors.perfil = ["Perfil é obrigatório."]`.
- **Sugestão ao dev:** trocar `PerfilUsuario Perfil` por `PerfilUsuario? Perfil` no command + `RuleFor(x => x.Perfil).NotNull().IsInEnum()`. Cobrir com unit test `[Trait("CA","011")]`.

### BUG-U004 — PATCH `/status` com body `{}` desativa silenciosamente (ALTA — AINDA ABERTO)

- **Severidade:** ALTA. Agora observável de ponta a ponta.
- **Sintoma:** `AlterarStatusUsuarioRequest.Ativo` é `bool` não nullable; body `{}` deserializa para `Ativo=false` e o handler executa UPDATE (ou no-op se já estava `false`). O cliente não pediu desativação; mesmo assim a intenção do código é desativar.
- **Caso afetado:** PATCH T7.
- **Reprodução:**
  ```bash
  curl -i -X PATCH http://localhost:8080/api/v1/usuarios/<id>/status \
    -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" \
    --data '{}'
  ```
  Resposta: `200 OK` com `{"id":"<id>","ativo":false,"atualizadoEm":"..."}`. Log Serilog: `Solicitação de alteração de status. UsuarioId=..., AtivoSolicitado=False` — não distingue omissão de pedido explícito.
- **Esperado:** `400 Bad Request` com `errors.ativo = ["Campo 'ativo' é obrigatório."]`.
- **Sugestão ao dev:** trocar `bool Ativo` por `bool? Ativo` + `RuleFor(x => x.Ativo).NotNull()`.

### BUG-U006 — ProblemDetails com title "Identificador inválido" para erros de body/binding (MÉDIA — AINDA ABERTO)

- **Severidade:** MÉDIA. Mesma raiz de BUG-U005 da v1 (unifiquei).
- **Sintoma:** Para body com enum desconhecido, `null` em campo não nullable, JSON malformado, ou Guid mal formado no path, o pipeline retorna `400` com `type=https://carwash/errors/invalid-request` e **`title="Identificador inválido."`** — mesmo `title` usado quando o problema é o Guid no path. Mensagem em `errors.request` vaza nome de parâmetro C# (`"CriarUsuarioCommand command"`, `"AlterarStatusUsuarioRequest request"`, `"Guid id"`).
- **Casos afetados:** POST T7, T8a, T10; GET T3, T8; PATCH T5, T8, T9.
- **Reprodução:**
  ```bash
  curl -s -X POST http://localhost:8080/api/v1/usuarios -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" \
    -d '{"nome":"x","email":"x@y.z","senha":"Senha@1234","perfil":"Gerente"}'
  ```
  Resposta:
  ```json
  {"type":"https://carwash/errors/invalid-request","title":"Identificador inválido.","status":400,"correlationId":"...","errors":{"request":["Failed to read parameter \"CriarUsuarioCommand command\" from the request body as JSON."]}}
  ```
- **Sugestão ao dev:** customizar `IProblemDetailsService` para diferenciar:
  - body inválido → `title="Corpo da requisição inválido."` + `errors.body` em PT-BR (sem nome do parâmetro C#);
  - path inválido → manter `title="Identificador inválido."` mas com `errors.id`;
  - quando possível, identificar campo culpado (`errors.perfil` em vez de `errors.request`).

### BUG-U007 — POST body `{}` não inclui `perfil` em `errors` (BAIXA — NOVO em v2)

- **Severidade:** BAIXA — UX/cosmético, mas confunde o front e está ligado a BUG-U003.
- **Sintoma:** Para `POST /api/v1/usuarios` com body `{}`, a resposta `400` lista `errors.nome`, `errors.email`, `errors.senha`, mas **omite** `errors.perfil`. O default do enum mascara a obrigatoriedade.
- **Caso afetado:** POST T9.
- **Reprodução:**
  ```bash
  curl -s -X POST http://localhost:8080/api/v1/usuarios -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" -d '{}'
  ```
  Resposta:
  ```json
  {"type":"https://carwash/errors/validation-error","title":"Dados do usuário inválidos. Verifique os campos e tente novamente.","status":400,"errors":{"nome":["Nome é obrigatório."],"email":["E-mail é obrigatório.","E-mail inválido."],"senha":["Senha não atende aos requisitos mínimos."]}}
  ```
- **Esperado:** `errors.perfil = ["Perfil é obrigatório."]` deve aparecer também. Mesmo fix de BUG-U003 resolve.

### BUG-U009 — Auto-desativação do admin do seed sem RN (ALTA — NOVO em v2)

- **Severidade:** ALTA — risco operacional. Sistema pode ficar sem nenhum administrador.
- **Sintoma:** `PATCH /api/v1/usuarios/{adminId}/status` com `{"ativo":false}` desativa o próprio admin logado (e o único admin do seed) sem trava. Retorna `200 OK` e persiste `ativo=false` no banco.
- **Caso afetado:** PATCH T11.
- **Reprodução:**
  ```bash
  TOKEN=<token de admin@carwash.local>
  curl -i -X PATCH http://localhost:8080/api/v1/usuarios/00000000-0000-0000-0000-000000000001/status \
    -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" \
    --data '{"ativo":false}'
  ```
  Resposta: `200 OK` + `{"id":"...0001","ativo":false}`. Banco confirma `ativo=f`. O token atual continua válido até expirar (acessTokens stateless).
- **Esperado:** `409 Conflict` ou `422 Unprocessable Entity` impedindo:
  - auto-desativação (`userId == JwtClaims.sub`);
  - desativação do último admin ativo (`SELECT COUNT(*) FROM usuarios WHERE perfil='ADMIN' AND ativo=true` = 1).
- **Ação imediata em produção:** revogar refresh tokens do admin desativado e reativar via SQL (`UPDATE usuarios SET ativo=true WHERE email='admin@carwash.local'`).
- **Question para PO/PM (`po-pm-carwash`):** confirmar RN explícita — "não é permitido desativar o último admin ativo nem o próprio usuário logado".

---

## POST /api/v1/usuarios (16 casos)

| ID | Descrição | Esperado | Obtido | Resultado | Bug |
|---|---|---|---|---|---|
| Tbug-Auth | POST sem `Authorization` | 401 | 401 + `WWW-Authenticate: Bearer` | PASS | — |
| T1 | Golden path — Funcionario válido | 201 + Location + body sem senha | 201 + `Location: /api/v1/usuarios/178f8572-...`, body com keys públicas | PASS | — |
| T2 | Email duplicado (2º POST) | 409 `email-already-exists` | 409 + `type=https://carwash/errors/email-already-exists` | PASS | — |
| T3 | Nome vazio/espaços | 400 `errors.nome` | 400 `errors.nome=["Nome é obrigatório."]` | PASS | — |
| T4 | Email malformado | 400 `errors.email` | 400 `errors.email=["E-mail inválido."]` | PASS | — |
| T5 | Senha curta `Ab1xyz` | 400 `errors.senha` | 400 `errors.senha=["Senha não atende aos requisitos mínimos."]` | PASS | — |
| T6a | Senha sem dígito `SenhaForte` | 400 | 400 `errors.senha` | PASS | — |
| T6b | Senha sem letra `12345678` | 400 | 400 `errors.senha` | PASS | — |
| T7 | Perfil `"Gerente"` | 400 com mensagem de enum | 400 `title="Identificador inválido"` / `errors.request` | PASS\* | U006 |
| T8a | `perfil: null` | 400 | 400 `errors.request` | PASS\* | U006 |
| T8b | `perfil` omitido | 400 `errors.perfil` | **201 Created — usuário persistido como Admin** | **FAIL** | **U003** |
| T9 | Body `{}` | 400 com Nome/Email/Senha/Perfil | 400 com `nome`, `email`, `senha` (omite `perfil`) | PASS\* | U007 |
| T10 | JSON malformado | 400 sem stacktrace | 400 `errors.request` `title="Identificador inválido"` | PASS\* | U006 |
| T11 | Normalização email `  USER@MAIL.COM  ` | 201 + email `user@mail.com` | 201 + `email: "norm-1779030641@mail.com"` (trim+lower aplicados) | PASS | — |
| T12a | Nome 120 chars | 201 | 201 | PASS | — |
| T12b | Nome 121 chars | 400 | 400 `errors.nome=["Nome excede 120 caracteres."]` | PASS | — |
| T12c | Email 150 chars (145 local + `@x.co`) | 201 | 201 | PASS | — |
| T12d | Email 151 chars (146 local + `@x.co`) | 400 | 400 `errors.email=["E-mail excede 150 caracteres."]` | PASS | — |
| T13 | Unicode/acento (`José da Silva ação`) | 201 preservado | 201 + nome preservado byte a byte | PASS | — |
| T14 | Race condition — 2 POSTs simultâneos mesmo email | 1×201 + 1×409; 1 linha no DB | `201`, `409` (xargs -P2); `SELECT COUNT(*) = 1` | PASS | — |
| T15 | Response sem senha/hash | Keys públicas | `keys=['ativo','atualizadoEm','criadoEm','email','id','nome','perfil']` | PASS | — |

\* PASS com defeito cosmético rastreado em BUG-U006 (title genérico/parâmetro C# vazando) e BUG-U007 (campo ausente no `errors`). Status final correto.

**Subtotal POST:** 16 casos | PASS=15 | FAIL=1 | BLOCKED=0.

---

## GET /api/v1/usuarios/{id} (11 casos)

ID válido reutilizado: `178f8572-35e2-4c8f-9cc5-808008ed98c4` (Maria Souza, criado em T1).

| ID | Descrição | Esperado | Obtido | Resultado | Bug |
|---|---|---|---|---|---|
| Tbug-Auth | GET sem `Authorization` | 401 | 401 + `WWW-Authenticate: Bearer` | PASS | — |
| T1 | Golden path (id válido) | 200 + UsuarioResponse | 200 + body com 7 keys públicas | PASS | — |
| T2 | Guid bem formado inexistente | 404 ProblemDetails | 404 `type=https://carwash/errors/not-found` `title="Usuário não encontrado."` | PASS | — |
| T3 | Path não-Guid `abc` | 400 | 400 `errors.request=["Failed to bind parameter \"Guid id\" from \"abc\"."]` `title="Identificador inválido"` | PASS\* | U006 |
| T4 | `Guid.Empty` | 404 | 404 ProblemDetails | PASS | — |
| T5 | Guid uppercase | 200 | 200 (Guid case-insensitive) | PASS | — |
| T6 | `Accept: application/xml` | 200 JSON (sem XML formatter) | 200 + `Content-Type: application/json` | PASS | — |
| T7 | Query params irrelevantes | 200 | 200 (binding ignora) | PASS | — |
| T8 | SQL injection no path (`%27%20OR%201=1`) | 400 (Guid binding bloqueia) | 400 `errors.request=["Failed to bind parameter \"Guid id\" from \"' OR 1=1\"."]` | PASS\* | U006 |
| T9 | Response sem campos sensíveis | 7 keys públicas | `keys=['ativo','atualizadoEm','criadoEm','email','id','nome','perfil']` | PASS | — |
| T10 | Performance (mediana < 300ms) | mediana < 0.3s | **mediana 0.002576s** (10 chamadas) | PASS | — |

\* PASS com defeito cosmético rastreado em BUG-U006.

**Subtotal GET:** 11 casos | PASS=11 | FAIL=0 | BLOCKED=0.

---

## PATCH /api/v1/usuarios/{id}/status (14 casos)

USER_ID dedicado: `f3aebe49-0d00-48e5-82c8-6b072fd07ac7` (PATCH Target, Funcionario).

| ID | Descrição | Esperado | Obtido | Resultado | Bug |
|---|---|---|---|---|---|
| Tbug-Auth | PATCH sem `Authorization` | 401 | 401 + `WWW-Authenticate: Bearer` | PASS | — |
| T1 | Desativar usuário ativo | 200 `{ativo:false}` | 200 + `{"id":"...","ativo":false,"atualizadoEm":"..."}` | PASS | — |
| T2 | Reativar usuário inativo | 200 `{ativo:true}` | 200 + `{"id":"...","ativo":true}` | PASS | — |
| T3 | Idempotente (desativar já inativo) | 200 sem erro | 200 + log `Status já é False ... no-op` (sem save) | PASS | — |
| T4 | Id válido inexistente | 404 | 404 `not-found` | PASS | — |
| T5 | Id não-Guid `123abc` | 400 | 400 `errors.request` `title="Identificador inválido"` | PASS\* | U006 |
| T6 | Body ausente (sem `--data`) | 400 `errors.body` | 400 `errors.body=["Corpo da requisição ausente ou malformado."]` | PASS | — |
| T7 | Body `{}` (campo `ativo` ausente) | 400 | **200 OK — desativação silenciosa** | **FAIL** | **U004** |
| T8 | `{"ativo": null}` | 400 | 400 `errors.request=["Failed to read parameter \"AlterarStatusUsuarioRequest request\"..."]` | PASS\* | U006 |
| T9 | `{"ativo": "sim"}` | 400 | 400 `errors.request` | PASS\* | U006 |
| T10 | Campo extra `{"ativo":true,"foo":"bar"}` | 200 (ignora extra) | 200 + body normal | PASS | — |
| T11 | Desativar admin do seed (auto-desativação) | 200 atual (sem RN); admin fica `ativo=false` | 200; banco confirmou `ativo=f`; **reativado em seguida via SQL** | PASS-com-risco | U009 |
| T12 | Response sem senha/hash | Apenas `id`, `ativo`, `atualizadoEm` | `keys=['ativo','atualizadoEm','id']` | PASS | — |
| T13 | Race condition (PATCH paralelos opostos) | 2×200 (last-write-wins atual) | `200`, `200`; estado final `ativo=t` (last-write vence) | PASS | — |

\* PASS com defeito cosmético rastreado em BUG-U006.

**Subtotal PATCH:** 14 casos | PASS=13 | FAIL=1 | BLOCKED=0.

---

## Observações para o time

1. **BUG-U001 e BUG-U002 fechados** — bloqueio crítico de release removido. POST/GET/PATCH operam end-to-end com autenticação. CA001 destravado para a feature de usuários.
2. **Auditoria operacional reforçada:** logs Serilog agora trazem `Solicitação de alteração de status. UsuarioId=..., AtivoSolicitado=...`, separando intenção de execução; idempotência reconhecida (`Status já é False ... no-op (sem save, sem audit)`). Bom para LGPD.
3. **Header `X-Correlation-Id`** presente em todas as respostas (sucesso e erro) — facilita troubleshooting.
4. **T11 (admin do seed) executado com cuidado:** admin foi desativado durante o teste e **REATIVADO imediatamente** via `UPDATE usuarios SET ativo=true WHERE email='admin@carwash.local'`. Re-login pós-fix bem-sucedido. Estado final no banco: `admin@carwash.local | t`.
5. **BUG-U003 e BUG-U004 são pequenos no diff mas grandes no impacto:** trocar `bool`/`enum` por `bool?`/`enum?` + regras `NotNull()` no validator resolve ambos. **Recomendo abrir issues separadas e bloquear release até fix.**
6. **BUG-U009 (auto-desativação do admin) é regressão de governança:** mesmo com auth corrigida, o admin pode se desativar sozinho. Em produção isso permite ataque de negação de acesso administrativo. Discutir RN com PO antes de implementar.
7. **CA011 — pendente:** falta cobertura `[Trait("CA","011")]` automatizada para BUG-U003, BUG-U004 e BUG-U009. Sugestões de testes:
   ```csharp
   [Fact][Trait("CA","011")]
   public async Task PostUsuario_SemPerfil_Retorna400() { /* assert errors.perfil */ }

   [Fact][Trait("CA","011")]
   public async Task PatchStatus_BodyVazio_Retorna400() { /* assert errors.ativo */ }

   [Fact][Trait("CA","011")]
   public async Task PatchStatus_DesativarUltimoAdmin_Retorna409() { /* RN nova */ }
   ```
8. **Não modifiquei nenhum arquivo de código nem de QA/usuarios/** durante a execução. SQL UPDATE usado **apenas** para reativar o admin do seed em T11.

## Próximos passos sugeridos

- [ ] Abrir issue BUG-U003 (escalada — perfil omitido cria Admin) — severidade ALTA, bloqueia release.
- [ ] Abrir issue BUG-U004 (PATCH `{}` desativa silenciosamente) — severidade ALTA, bloqueia release.
- [ ] Abrir issue BUG-U006 (ProblemDetails inconsistente para body vs path) — severidade MÉDIA.
- [ ] Abrir issue BUG-U007 (T9 omite `perfil` em `errors`) — severidade BAIXA, mesma raiz que U003.
- [ ] Abrir issue BUG-U009 (auto-desativação do admin) — severidade ALTA, requer RN do PO.
- [ ] Após fix de U003/U004, rodar terceira bateria (v3) com foco em T8b POST e T7 PATCH + adicionar testes `[Trait("CA","011")]` no CI.
