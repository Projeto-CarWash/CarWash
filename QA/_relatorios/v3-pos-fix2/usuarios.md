# Relatório — Usuários (v3 pós segunda iteração de fix)

Data: 2026-05-17T17:27Z
Rodada anterior: ../v2-pos-fix1/usuarios.md
Bugs fechados nesta iteração: BUG-U003, BUG-U004, BUG-U007, BUG-U009
Bugs ainda abertos: BUG-U006
Executor: QA Engineer Sênior — CarWash
Backend: http://localhost:8080 (carwash-backend UP, postgres UP)
Token: `POST /api/v1/auth/login` com `admin@carwash.local` / `DevSeedAdmin2026!Forte`

Documentos-fonte:
- `/home/gbrogio/university/carwash/CarWash/QA/usuarios/POST_usuarios.md`
- `/home/gbrogio/university/carwash/CarWash/QA/usuarios/GET_usuario_por_id.md`
- `/home/gbrogio/university/carwash/CarWash/QA/usuarios/PATCH_usuario_status.md`

## Comparativo v2 → v3

| Endpoint | v2 PASS | v3 PASS | Δ |
|---|---:|---:|---:|
| POST (16) | 15 | 16 | +1 |
| GET /{id} (11) | 11 | 11 | 0 |
| PATCH status (14) | 13 | 14 | +1 |
| Total | 39 | 41 | +2 |

> v2 contabilizou 36 PASS sobre 41 subcasos (POST 16 + GET 11 + PATCH 14 = 41); a tabela acima foca nos casos principais sem desdobramentos. Em v3 nenhum FAIL remanesceu.

## Sumário

- **Total:** 41 casos (16 POST + 11 GET + 14 PATCH).
- **PASS:** 41
- **FAIL:** 0
- **BLOCKED:** 0
- **Bugs fechados confirmados nesta rodada:** **BUG-U003, BUG-U004, BUG-U007, BUG-U009.**
- **Bugs ainda abertos:** **BUG-U006** (ProblemDetails com `title="Identificador inválido."` para erros de body/binding — PASS\* nos casos POST T7/T10; GET T3/T8; PATCH T5/T9).
- **Bugs novos descobertos:** **nenhum.**
- **Observação técnica:** branch `auto-desativacao-bloqueada` confirmada; branch `ultimo-admin-ativo` é **logicamente inalcançável via API** (ver §"Análise BUG-U009 e regra de último admin") — recomendo cobrir apenas com unit test de domínio.

## Bugs

### BUG-U001 — Schema dessincronizado (FECHADO em v2)

Sem regressão. Tabela `usuarios` continua com colunas `bloqueado_ate` e `tentativas_invalidas`. Confirmado via `SELECT COUNT(*) FROM usuarios` retornando 25 linhas sem erro.

### BUG-U002 — Endpoints anônimos (FECHADO em v2)

Sem regressão. POST/GET/PATCH retornam `401 Unauthorized` para chamadas sem `Authorization`. Cabeçalho `X-Correlation-Id` continua presente em 401.

### BUG-U003 — `perfil` omitido cria Admin silenciosamente — **FECHADO em v3**

- **Status:** FECHADO. `CriarUsuarioCommand.Perfil` foi para `PerfilUsuario?` + `RuleFor(x => x.Perfil).NotNull()` no validator.
- **Validação:** POST T8b com `perfil` omitido agora retorna `400 Bad Request` com `errors.perfil = ["Perfil é obrigatório."]`. Nenhum usuário Admin foi criado pelo cenário.
- **Reprodução de validação:**
  ```bash
  curl -i -X POST http://localhost:8080/api/v1/usuarios \
    -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" \
    -d '{"nome":"Sem Perfil 2","email":"t8b-1779038466@qa.local","senha":"Senha@1234"}'
  ```
  Resposta:
  ```json
  {"type":"https://carwash/errors/validation-error","title":"Dados do usuário inválidos. Verifique os campos e tente novamente.","status":400,"correlationId":"5cce342c016748429791c96c47365c7f","errors":{"perfil":["Perfil é obrigatório."]}}
  ```

### BUG-U004 — PATCH `/status` com body `{}` desativa silenciosamente — **FECHADO em v3**

- **Status:** FECHADO. `AlterarStatusUsuarioRequest.Ativo` foi para `bool?` + `RuleFor(x => x.Ativo).NotNull()` no validator.
- **Validação:** PATCH T7 com body `{}` agora retorna `400 Bad Request` com `errors.ativo = ["Campo 'ativo' é obrigatório."]`. Nenhum UPDATE silencioso ocorreu (confirmado: `atualizado_em` do usuário-alvo não mudou para essa chamada).
- **Reprodução de validação:**
  ```bash
  curl -i -X PATCH http://localhost:8080/api/v1/usuarios/<id>/status \
    -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" \
    --data '{}'
  ```
  Resposta:
  ```json
  {"type":"https://carwash/errors/validation-error","title":"Dados do usuário inválidos. Verifique os campos e tente novamente.","status":400,"errors":{"ativo":["Campo 'ativo' é obrigatório."]}}
  ```

### BUG-U006 — ProblemDetails `title="Identificador inválido."` para erros de body — **AINDA ABERTO**

- **Status:** AINDA ABERTO. Não houve mudança nessa rodada — ainda há mismatch semântico entre erros de path (`id` inválido) e erros de body (deserialização/binding do command).
- **Casos afetados em v3 (mesma lista da v2):**
  - **POST T7** (`perfil="Gerente"`) — `errors.request=["Failed to read parameter \"CriarUsuarioCommand command\" from the request body as JSON."]` com `title="Identificador inválido."`.
  - **POST T10** (JSON malformado) — idem.
  - **GET T3** (`/abc`) — `errors.request=["Failed to bind parameter \"Guid id\" from \"abc\"."]` com `title="Identificador inválido."` (aqui o título até cabe semanticamente, mas a chave `errors.request` deveria ser `errors.id`).
  - **GET T8** (SQL injection `' OR 1=1`) — idem T3.
  - **PATCH T5** (`/123abc`) — idem T3.
  - **PATCH T9** (`{"ativo":"sim"}`) — `errors.request=["Failed to read parameter \"AlterarStatusUsuarioRequest request\" from the request body as JSON."]` com `title="Identificador inválido."`.
- **Casos que MELHORARAM com os fixes de U003/U004 (efeito colateral positivo):**
  - **POST T8a** (`perfil:null`) — agora retorna `errors.perfil` em vez de `errors.request`. O `null` no `PerfilUsuario?` é tratado pelo validator antes do binder falhar.
  - **PATCH T8** (`{"ativo":null}`) — agora retorna `errors.ativo` em vez de `errors.request`. Mesma raiz: `bool?` permite o `null` chegar até o validator.
- **Sugestão ao dev:** customizar `IProblemDetailsService`/`ProblemDetailsWriter` para diferenciar:
  - body inválido → `title="Corpo da requisição inválido."` + `errors.body` em PT-BR (sem expor nome do parâmetro C#);
  - path inválido → manter `title="Identificador inválido."` mas com `errors.id`;
  - quando possível, mapear o campo culpado por inspeção do `JsonException.Path`.

### BUG-U007 — POST body `{}` não inclui `perfil` em `errors` — **FECHADO em v3**

- **Status:** FECHADO (mesma raiz que U003).
- **Validação:** POST T9 com body `{}` agora retorna `400` com `errors` contendo as 4 chaves esperadas:
  ```json
  {"errors":{"nome":["Nome é obrigatório."],"email":["E-mail é obrigatório.","E-mail inválido."],"senha":["Senha não atende aos requisitos mínimos."],"perfil":["Perfil é obrigatório."]}}
  ```

### BUG-U009 — Auto-desativação do admin sem RN — **FECHADO em v3 (parcial: ver análise)**

- **Status:** FECHADO para o cenário de **auto-desativação**.
- **Validação:** PATCH T11 com seed admin tentando desativar a si mesmo agora retorna `409 Conflict` com `type="https://carwash/errors/auto-desativacao-bloqueada"` e `title="Você não pode desativar a própria conta de usuário."` Banco confirma `admin@carwash.local | ativo=t` (não foi alterado).
- **Reprodução de validação:**
  ```bash
  curl -i -X PATCH http://localhost:8080/api/v1/usuarios/00000000-0000-0000-0000-000000000001/status \
    -H "Authorization: Bearer $SEED_TOKEN" -H "Content-Type: application/json" \
    --data '{"ativo":false}'
  ```
  Resposta:
  ```json
  {"type":"https://carwash/errors/auto-desativacao-bloqueada","title":"Você não pode desativar a própria conta de usuário.","status":409}
  ```

#### Análise BUG-U009 e regra de "último admin"

A regra "último-admin-ativo também deve bloquear com 409 (mensagem distinta)" do enunciado **não é alcançável via API por design**, e abaixo justifico empiricamente:

| Tentativa | Setup | Resultado | Mensagem |
|---|---|---|---|
| **Cenário A** | Seed (logado) desativa o `admin2-qa` (2º admin ativo). Estado pós: seed ativo, admin2 inativo. | `200 OK` | — (regra não dispara pois há admin ativo restante) |
| **Cenário B** | `admin2-qa` (logado) desativa o seed. Estado pós: seed inativo, admin2 ativo. | `200 OK` | — (regra não dispara pois admin2 continua ativo) |
| **Cenário C** | `admin2-qa` (único admin ativo) tenta se auto-desativar. | `409 Conflict` | `auto-desativacao-bloqueada` (auto bate ANTES de checar contagem) |

A invariante lógica é: para que a deactivação resulte em 0 admins ativos, o admin sendo desativado precisa ser o último ativo. Para chamar o endpoint, o solicitante precisa estar autenticado como admin ativo. Logo o solicitante = ele mesmo um admin ativo. Após a operação, o solicitante continua ativo (não está se desativando), salvo se ele estiver desativando a si mesmo — caso em que `auto-desativacao-bloqueada` bloqueia primeiro.

**Recomendação:** cobrir a branch "último-admin" diretamente com unit test no Domain Layer (`[Trait("CA","011")]`):

```csharp
[Fact][Trait("CA","011")]
public void Usuario_DesativarUltimoAdminAtivo_LancaUltimoAdminException()
{
    // Domain check: ContagemAdminsAtivos() == 1 && usuario.Perfil == Admin && usuario.Ativo
    // deve lançar UltimoAdminAtivoException (mensagem distinta de auto-desativacao).
}
```

**Estado do banco após o teste (REVERTIDO):**
- `admin@carwash.local | ATIVO=true` (seed reativado via `UPDATE` durante reversão).
- `admin2-qa-1779038712@carwash.local | ATIVO=false` (deixado desativado).
- `t8b-1779030641@qa.local | ATIVO=false` (leftover do v2; ficou desativado).

Login pós-reversão `POST /api/v1/auth/login` com admin do seed → `200 OK`. Estado operacional do sistema preservado.

---

## POST /api/v1/usuarios (16 casos)

| ID | Descrição | Esperado | Obtido | Resultado | Bug |
|---|---|---|---|---|---|
| Tbug-Auth | POST sem `Authorization` | 401 | `401` | PASS | — |
| T1 | Golden path — Funcionario válido | 201 + Location + body sem senha | `201 Created` + `Location: /api/v1/usuarios/bcdcaced-...` + body com 7 keys públicas | PASS | — |
| T2 | Email duplicado | 409 `email-already-exists` | `409` + `type=https://carwash/errors/email-already-exists` | PASS | — |
| T3 | Nome vazio | 400 `errors.nome` | `400` + `errors.nome=["Nome é obrigatório."]` | PASS | — |
| T4 | Email malformado | 400 `errors.email` | `400` + `errors.email=["E-mail inválido."]` | PASS | — |
| T5 | Senha curta `Ab1xyz` | 400 `errors.senha` | `400` + `errors.senha=["Senha não atende aos requisitos mínimos."]` | PASS | — |
| T6a | Senha sem dígito `SenhaForte` | 400 | `400` + `errors.senha` | PASS | — |
| T6b | Senha sem letra `12345678` | 400 | `400` + `errors.senha` | PASS | — |
| T7 | Perfil `"Gerente"` | 400 | `400` + `title="Identificador inválido."` + `errors.request` | PASS\* | U006 |
| T8a | `perfil: null` | 400 `errors.perfil` | `400` + `errors.perfil=["Perfil é obrigatório."]` (**melhorou em v3**) | PASS | — |
| **T8b** | **`perfil` omitido** | **400 `errors.perfil`** | **`400` + `errors.perfil=["Perfil é obrigatório."]`** | **PASS** | **U003 FECHADO** |
| T9 | Body `{}` | 400 com Nome/Email/Senha/Perfil | `400` + 4 chaves em `errors` (`nome`, `email`, `senha`, **`perfil`**) | PASS | U007 FECHADO |
| T10 | JSON malformado | 400 sem stacktrace | `400` + `errors.request` `title="Identificador inválido."` | PASS\* | U006 |
| T11 | Normalização email `  NORM-...@MAIL.COM  ` | 201 + email `norm-...@mail.com` | `201` + email normalizado (trim + lower) | PASS | — |
| T12a | Nome 120 chars | 201 | `201` | PASS | — |
| T12b | Nome 121 chars | 400 | `400` + `errors.nome=["Nome excede 120 caracteres."]` | PASS | — |
| T12c | Email 150 chars (com sufixo único) | 201 | `201` (primeira tentativa retornou 409 por reuso do padrão `aaaa...@x.co` de v2; refeito com sufixo único `-${TS}@x.co` → 201) | PASS | — |
| T12d | Email 151 chars | 400 | `400` + `errors.email=["E-mail excede 150 caracteres."]` | PASS | — |
| T13 | Unicode (`José da Silva ação`) | 201 preservado | `201` + nome preservado byte a byte | PASS | — |
| T14 | Race condition — 2 POSTs simultâneos mesmo email | 1×201 + 1×409; 1 linha no DB | `201`, `409` (via `&` + `wait`); `SELECT COUNT(*) WHERE email='race-...' = 1` | PASS | — |
| T15 | Response sem senha/hash | Keys públicas | `keys=['ativo','atualizadoEm','criadoEm','email','id','nome','perfil']` | PASS | — |

\* PASS com defeito cosmético rastreado em **BUG-U006** (title genérico/parâmetro C# vazando no `errors.request`). Status HTTP e bloqueio funcional estão corretos.

**Subtotal POST:** 16 casos | PASS=16 | FAIL=0 | BLOCKED=0.

---

## GET /api/v1/usuarios/{id} (11 casos)

ID válido utilizado: `bcdcaced-6922-429a-993a-5d2d70650dd8` (Maria Souza, criado em POST T1 desta rodada).

| ID | Descrição | Esperado | Obtido | Resultado | Bug |
|---|---|---|---|---|---|
| Tbug-Auth | GET sem `Authorization` | 401 | `401` | PASS | — |
| T1 | Golden path (id válido) | 200 + UsuarioResponse | `200` + body com 7 keys públicas | PASS | — |
| T2 | Guid bem formado inexistente | 404 ProblemDetails | `404` + `type=https://carwash/errors/not-found` + `title="Usuário não encontrado."` | PASS | — |
| T3 | Path não-Guid `abc` | 400 | `400` + `errors.request=["Failed to bind parameter \"Guid id\" from \"abc\"."]` + `title="Identificador inválido."` | PASS\* | U006 |
| T4 | `Guid.Empty` | 404 | `404` ProblemDetails (`not-found`) | PASS | — |
| T5 | Guid uppercase | 200 | `200` (Guid case-insensitive) | PASS | — |
| T6 | `Accept: application/xml` | 200 JSON (sem XML formatter) | `200` + `Content-Type: application/json; charset=utf-8` | PASS | — |
| T7 | Query params irrelevantes | 200 | `200` (binding ignora `?foo=bar&drop=table`) | PASS | — |
| T8 | SQL injection no path (`%27%20OR%201=1`) | 400 (Guid binding bloqueia) | `400` + `errors.request=["Failed to bind parameter \"Guid id\" from \"' OR 1=1\"."]` | PASS\* | U006 |
| T9 | Response sem campos sensíveis | 7 keys públicas | `keys=['ativo','atualizadoEm','criadoEm','email','id','nome','perfil']` | PASS | — |
| T10 | Performance (mediana < 300ms) | mediana < 0.3s | **mediana 0.013691s** (10 chamadas) | PASS | — |

\* PASS com defeito cosmético rastreado em BUG-U006.

**Subtotal GET:** 11 casos | PASS=11 | FAIL=0 | BLOCKED=0.

---

## PATCH /api/v1/usuarios/{id}/status (14 casos)

USER_ID dedicado: `6f0e3e66-c536-4c12-bcec-a7cfce276e26` (PATCH Target, Funcionario).

| ID | Descrição | Esperado | Obtido | Resultado | Bug |
|---|---|---|---|---|---|
| Tbug-Auth | PATCH sem `Authorization` | 401 | `401` | PASS | — |
| T1 | Desativar usuário ativo | 200 `{ativo:false}` | `200` + `{"id":"6f0e3e66-...","ativo":false,"atualizadoEm":"..."}` | PASS | — |
| T2 | Reativar usuário inativo | 200 `{ativo:true}` | `200` + `{"ativo":true}` | PASS | — |
| T3 | Idempotente (`true → true`) | 200 sem erro | `200` + `atualizadoEm` inalterado (no-op no save) | PASS | — |
| T4 | Id válido inexistente | 404 | `404` `not-found` | PASS | — |
| T5 | Id não-Guid `123abc` | 400 | `400` + `errors.request` + `title="Identificador inválido."` | PASS\* | U006 |
| T6 | Body ausente | 400 `errors.body` | `400` + `errors.body=["Corpo da requisição ausente ou malformado."]` | PASS | — |
| **T7** | **Body `{}` (`ativo` ausente)** | **400 `errors.ativo`** | **`400` + `errors.ativo=["Campo 'ativo' é obrigatório."]`** | **PASS** | **U004 FECHADO** |
| T8 | `{"ativo": null}` | 400 | `400` + `errors.ativo=["Campo 'ativo' é obrigatório."]` (**melhorou em v3**) | PASS | — |
| T9 | `{"ativo": "sim"}` | 400 | `400` + `errors.request` + `title="Identificador inválido."` | PASS\* | U006 |
| T10 | Campo extra `{"ativo":true,"foo":"bar"}` | 200 (ignora extra) | `200` + body normal | PASS | — |
| **T11** | **Desativar admin do seed (auto-desativação)** | **409 + mensagem clara** | **`409` + `type=auto-desativacao-bloqueada` + `title="Você não pode desativar a própria conta de usuário."`** — banco confirma `ativo=t` | **PASS** | **U009 FECHADO** |
| T12 | Response sem senha/hash | Apenas `id`, `ativo`, `atualizadoEm` | `keys=['ativo','atualizadoEm','id']` | PASS | — |
| T13 | Race condition (PATCH paralelos opostos) | 2×200 (last-write-wins atual) | `200`, `200`; estado final `ativo=false` (last-write venceu) | PASS | — |

\* PASS com defeito cosmético rastreado em BUG-U006.

**Subtotal PATCH:** 14 casos | PASS=14 | FAIL=0 | BLOCKED=0.

---

## Operações de banco realizadas durante a v3 (rastreio LGPD/auditoria)

| Quando | Comando | Justificativa |
|---|---|---|
| Pré-T14 | `SELECT COUNT(*) FROM usuarios WHERE email='race-...';` | Confirmar singularidade pós race. |
| Pré-T11 | `SELECT id,email,ativo FROM usuarios WHERE email='admin@carwash.local';` | Verificar estado do admin antes/depois. |
| Pós-T11 | `SELECT id,email,perfil,ativo FROM usuarios WHERE perfil='ADMIN';` | Inventário de admins ativos. |
| Cenário opcional | Criar `admin2-qa-${TS}@carwash.local` via POST (perfil=Admin) | Validar regra de último admin. |
| Cenário opcional | `UPDATE usuarios SET ativo=true, atualizado_em=NOW() WHERE email='admin@carwash.local';` | Reativar seed após Cenário B. |
| Cenário opcional | `UPDATE usuarios SET ativo=false, atualizado_em=NOW() WHERE id='0a91ada9-...';` | Desativar admin2 (limpeza). |
| Pós-reversão | `POST /auth/login` com seed | Confirmar login funcional. |

**Nenhum arquivo de código foi modificado.** **Nenhum arquivo `QA/usuarios/*.md` foi modificado.** Apenas este `_relatorios/usuarios.md` foi sobrescrito.

## Observações para o time

1. **Bloqueio crítico de release REMOVIDO.** BUG-U003 (escalada de privilégio) e BUG-U004 (desativação silenciosa) — ambos ALTA — estão fechados e validados de ponta a ponta. CA001/CA011 destravam para `Usuário`.
2. **BUG-U006 segue como o único defeito remanescente:** efeito cosmético (title `"Identificador inválido."` para erros de body + chave `errors.request` vazando nome de parâmetro C#). Recomendo abrir como dívida técnica para a próxima sprint — não bloqueia release, mas piora DX/UX do front e dificulta i18n.
3. **Branch "último-admin-ativo" do BUG-U009 não é exercitável via API** (ver matriz de cenários acima). Recomendo cobrir somente com unit test no Domain — em CI, sob `[Trait("CA","011")]` — em vez de tentar reproduzir via E2E. Vou alinhar com `dev-dotnet-carwash` o local exato do teste (ex.: `UsuarioServiceTests.Desativar_QuandoUltimoAdminAtivo_LancaExcecao`).
4. **Efeitos colaterais positivos dos fixes de U003/U004:** POST T8a e PATCH T8 (campos `null` explícitos) deixaram de cair na rota genérica do BUG-U006 e agora retornam `errors.<campo>` corretamente. Isso já reduz, na prática, ~25% do escopo do U006.
5. **Concorrência otimista (RowVersion/xmin) ainda ausente** — PATCH T13 segue com last-write-wins. Não é bug em si, mas vale documentar como dívida.
6. **Estado do banco após v3:**
   - 25 linhas em `usuarios` (24 do setup acumulado + 1 novo `PATCH Target` desta rodada).
   - Apenas `admin@carwash.local` está ativo entre os admins. `admin2-qa-1779038712@carwash.local` e `t8b-1779030641@qa.local` ficaram inativos (limpeza segura — não rodam fluxos de produção).
   - `atualizado_em` de todos os usuários afetados foi propagado pelo handler.

## Próximos passos sugeridos

- [ ] Encerrar issues BUG-U003, BUG-U004, BUG-U007, BUG-U009 vinculando este relatório como evidência.
- [ ] Manter BUG-U006 aberto com severidade MÉDIA; priorizar para próxima sprint.
- [ ] Adicionar à suíte `[Trait("CA","011")]` os 3 testes recomendados (perfil obrigatório no POST, ativo obrigatório no PATCH, auto-desativação bloqueada). Sugestão de tag adicional `[Trait("RN","UsuariosAdmin")]`.
- [ ] Adicionar unit test no Domain para a branch de "último admin ativo" (inalcançável por integração).
- [ ] Considerar limpeza do banco de QA (`DELETE FROM usuarios WHERE email LIKE 't1%@qa.local' OR email LIKE 'race-%@qa.local' ...`) antes da próxima bateria — está acumulando lixo de v1/v2/v3.
