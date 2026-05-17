# Relatório — Usuários (v4 pós terceira iteração de fix)

Data: 2026-05-17T18:55Z
Rodada anterior: ../v3-pos-fix2/usuarios.md
Bugs fechados nesta iteração: **BUG-U006**
Executor: QA Engineer Sênior — CarWash
Backend: http://localhost:8080 (carwash-backend UP, carwash-postgres UP healthy)
Token: `POST /api/v1/auth/login` com `admin@carwash.local` / `DevSeedAdmin2026!Forte`

Documentos-fonte:
- `/home/gbrogio/university/carwash/CarWash/QA/usuarios/POST_usuarios.md`
- `/home/gbrogio/university/carwash/CarWash/QA/usuarios/GET_usuario_por_id.md`
- `/home/gbrogio/university/carwash/CarWash/QA/usuarios/PATCH_usuario_status.md`

## Comparativo v3 → v4

| Endpoint | v3 PASS | v4 PASS | Δ |
|---|---:|---:|---:|
| POST (16) | 16 | 16 | 0 |
| GET /{id} (11) | 11 | 11 | 0 |
| PATCH status (14) | 14 | 14 | 0 |
| **Total** | **41** | **41** | **0** |

Diferença qualitativa: 6 casos que em v3 contabilizavam **PASS\*** (status correto + defeito cosmético rastreado em BUG-U006) agora são **PASS sem ressalva**. São eles: POST T7, POST T10, GET T3, GET T8, PATCH T5, PATCH T9.

## Sumário

- **Total:** 41 casos (16 POST + 11 GET + 14 PATCH).
- **PASS:** 41
- **FAIL:** 0
- **BLOCKED:** 0
- **Bugs fechados confirmados nesta rodada:** **BUG-U006** (ProblemDetails diferenciando body vs. path, sem vazar nome de parâmetro C#).
- **Bugs ainda abertos:** **nenhum**.
- **Bugs novos descobertos:** **nenhum**.
- **Estado do BUG-U006 antes/depois:** v3 retornava `title="Identificador inválido."` para erros de body com chave genérica `errors.request=["Failed to read parameter ..."]`. v4 retorna `title="Corpo da requisição inválido. Verifique o JSON e tente novamente."` com `errors.<campo>` ou `errors.body` em PT-BR, sem expor nome do parâmetro C#. Para erros de path (Guid inválido), o título `"Identificador inválido."` é preservado, agora com chave `errors.id` (não `errors.request`).

## Bugs

### BUG-U001 — Schema dessincronizado (FECHADO em v2)

Sem regressão. Tabela `usuarios` continua estável; 30 linhas no banco após a v4 (acumulando QA de v1–v4), sem erro de schema em nenhum dos 41 casos.

### BUG-U002 — Endpoints anônimos (FECHADO em v2)

Sem regressão. POST/GET/PATCH retornam `401 Unauthorized` para chamadas sem `Authorization`. Confirmado em Tbug-Auth dos três endpoints.

### BUG-U003 — `perfil` omitido cria Admin silenciosamente (FECHADO em v3)

Sem regressão. POST T8b → `400` + `errors.perfil=["Perfil é obrigatório."]`. Nenhuma criação de Admin silenciosa.

### BUG-U004 — PATCH `/status` com body `{}` desativa silenciosamente (FECHADO em v3)

Sem regressão. PATCH T7 → `400` + `errors.ativo=["Campo 'ativo' é obrigatório."]`.

### BUG-U006 — ProblemDetails para erros de body/binding — **FECHADO em v4**

- **Status:** **FECHADO**. ProblemDetails passa a diferenciar semanticamente erros de body (deserialização) vs. erros de path (route binding), com mensagens em PT-BR e sem vazar nome de parâmetro C# (`CriarUsuarioCommand command`, `Guid id`, `AlterarStatusUsuarioRequest request`).

- **Validação por caso (antes v3 → depois v4):**

  | Caso | Cenário | v3 (defeito) | v4 (corrigido) |
  |---|---|---|---|
  | **POST T7** | `perfil:"Gerente"` | `title="Identificador inválido."` + `errors.request=["Failed to read parameter \"CriarUsuarioCommand command\"..."]` | `title="Corpo da requisição inválido. Verifique o JSON e tente novamente."` + `errors.perfil=["Valor inválido para o campo informado."]` |
  | **POST T10** | JSON malformado | `title="Identificador inválido."` + `errors.request=["Failed to read parameter \"CriarUsuarioCommand command\"..."]` | `title="Corpo da requisição inválido. Verifique o JSON e tente novamente."` + `errors.body=["Corpo da requisição inválido. Verifique o JSON e tente novamente."]` |
  | **GET T3** | path `/abc` | `title="Identificador inválido."` + `errors.request=["Failed to bind parameter \"Guid id\" from \"abc\"."]` | `title="Identificador inválido."` + `errors.id=["Identificador deve ser um GUID válido."]` |
  | **GET T8** | path `'%20OR%201=1` | `title="Identificador inválido."` + `errors.request=["Failed to bind parameter \"Guid id\"..."]` | `title="Identificador inválido."` + `errors.id=["Identificador deve ser um GUID válido."]` |
  | **PATCH T5** | path `/123abc` | `title="Identificador inválido."` + `errors.request` | `title="Identificador inválido."` + `errors.id=["Identificador deve ser um GUID válido."]` |
  | **PATCH T9** | `{"ativo":"sim"}` | `title="Identificador inválido."` + `errors.request=["Failed to read parameter \"AlterarStatusUsuarioRequest request\"..."]` | `title="Corpo da requisição inválido. Verifique o JSON e tente novamente."` + `errors.ativo=["Valor inválido para o campo informado."]` |

- **Comportamento agregado consolidado:**
  - **Body inválido (deserialização):** `type=https://carwash/errors/invalid-request`, `title="Corpo da requisição inválido. Verifique o JSON e tente novamente."`, `errors.<campo>` (quando o JsonException expõe `Path`) ou `errors.body` (quando não há campo identificável).
  - **Path inválido (route binding de Guid):** `type=https://carwash/errors/invalid-request`, `title="Identificador inválido."`, `errors.id=["Identificador deve ser um GUID válido."]`.
  - **Validation rule do FluentValidation:** `type=https://carwash/errors/validation-error`, `title="Dados do usuário inválidos. Verifique os campos e tente novamente."`, `errors.<campo>` em PT-BR.

### BUG-U007 — POST body `{}` não inclui `perfil` em `errors` (FECHADO em v3)

Sem regressão. POST T9 → `400` + `errors` com `nome`, `email`, `senha`, `perfil`.

### BUG-U009 — Auto-desativação do admin sem RN (FECHADO em v3)

Sem regressão. PATCH T11 → `409` + `type=auto-desativacao-bloqueada` + `title="Você não pode desativar a própria conta de usuário."`. Banco confirma `admin@carwash.local | ativo=t` após o cenário.

---

## POST /api/v1/usuarios (16 casos)

| ID | Descrição | Esperado | Obtido | Resultado |
|---|---|---|---|---|
| Tbug-Auth | POST sem `Authorization` | 401 | `401` | PASS |
| T1 | Golden path — Funcionario válido | 201 + Location + body sem senha | `201` + `Location: /api/v1/usuarios/53f93bd7-...` + body com 7 keys públicas | PASS |
| T2 | Email duplicado | 409 `email-already-exists` | `409` + `type=https://carwash/errors/email-already-exists` + `title="Já existe usuário cadastrado com este e-mail."` | PASS |
| T3 | Nome vazio (`"   "`) | 400 `errors.nome` | `400` + `errors.nome=["Nome é obrigatório."]` | PASS |
| T4 | Email malformado | 400 `errors.email` | `400` + `errors.email=["E-mail inválido."]` | PASS |
| T5 | Senha curta `Ab1xyz` | 400 `errors.senha` | `400` + `errors.senha=["Senha não atende aos requisitos mínimos."]` | PASS |
| T6a | Senha sem dígito `SenhaForte` | 400 | `400` + `errors.senha` | PASS |
| T6b | Senha sem letra `12345678` | 400 | `400` + `errors.senha` | PASS |
| **T7** | **Perfil `"Gerente"`** | **400 `errors.perfil` + title de corpo inválido** | **`400` + `title="Corpo da requisição inválido. Verifique o JSON e tente novamente."` + `errors.perfil=["Valor inválido para o campo informado."]`** | **PASS (U006 FECHADO)** |
| T8a | `perfil: null` | 400 `errors.perfil` | `400` + `errors.perfil=["Perfil é obrigatório."]` | PASS |
| T8b | `perfil` omitido | 400 `errors.perfil` | `400` + `errors.perfil=["Perfil é obrigatório."]` | PASS |
| T9 | Body `{}` | 400 com 4 chaves | `400` + `errors` com `nome`, `email`, `senha`, `perfil` | PASS |
| **T10** | **JSON malformado** | **400 sem stacktrace + `errors.body`** | **`400` + `title="Corpo da requisição inválido..."` + `errors.body=["Corpo da requisição inválido..."]`** | **PASS (U006 FECHADO)** |
| T11 | Normalização email `  NORM-${TS}@MAIL.COM  ` | 201 + email normalizado | `201` + `email=norm-1779043654@mail.com` (trim + lower) | PASS |
| T12a | Nome 120 chars | 201 | `201` | PASS |
| T12b | Nome 121 chars | 400 | `400` + `errors.nome=["Nome excede 120 caracteres."]` | PASS |
| T12c | Email 150 chars (exato) | 201 | `201` (verificado boundary com `wc -c` → 150) | PASS |
| T12d | Email 151 chars (exato) | 400 | `400` + `errors.email=["E-mail excede 150 caracteres."]` (`wc -c` → 151) | PASS |
| T13 | Unicode (`José da Silva ação`) | 201 preservado | `201` + nome preservado byte a byte | PASS |
| T14 | Race condition — 2 POSTs simultâneos mesmo email | 1×201 + 1×409; 1 linha no DB | `201` + `409` (paralelo com `&`+`wait`); `SELECT COUNT(*) WHERE email='race-...'` = `1` | PASS |
| T15 | Response sem senha/hash | 7 keys públicas | `keys=['ativo','atualizadoEm','criadoEm','email','id','nome','perfil']` | PASS |

**Subtotal POST:** 16 casos | PASS=16 | FAIL=0 | BLOCKED=0.

---

## GET /api/v1/usuarios/{id} (11 casos)

USER_ID dedicado: `70fe47ca-85e9-4567-8dcd-1fd6d0dab2d0` (QA Get Target, criado no setup do GET).

| ID | Descrição | Esperado | Obtido | Resultado |
|---|---|---|---|---|
| Tbug-Auth | GET sem `Authorization` | 401 | `401` | PASS |
| T1 | Golden path (id válido) | 200 + UsuarioResponse | `200` + body com 7 keys públicas | PASS |
| T2 | Guid bem formado inexistente | 404 ProblemDetails | `404` + `type=https://carwash/errors/not-found` + `title="Usuário não encontrado."` | PASS |
| **T3** | **Path não-Guid `abc`** | **400 + `errors.id`** | **`400` + `title="Identificador inválido."` + `errors.id=["Identificador deve ser um GUID válido."]`** | **PASS (U006 FECHADO)** |
| T4 | `Guid.Empty` | 404 | `404` + `type=not-found` + `title="Usuário não encontrado."` | PASS |
| T5 | Guid uppercase | 200 | `200` (Guid case-insensitive) | PASS |
| T6 | `Accept: application/xml` | 200 JSON | `200` + `Content-Type: application/json; charset=utf-8` | PASS |
| T7 | Query params irrelevantes | 200 | `200` (binding ignora `?foo=bar&drop=table`) | PASS |
| **T8** | **SQL injection no path (`%27%20OR%201=1`)** | **400 + `errors.id`** | **`400` + `title="Identificador inválido."` + `errors.id=["Identificador deve ser um GUID válido."]`** | **PASS (U006 FECHADO)** |
| T9 | Response sem campos sensíveis | 7 keys públicas | `keys=['ativo','atualizadoEm','criadoEm','email','id','nome','perfil']` | PASS |
| T10 | Performance (mediana < 300ms) | mediana < 0.3s | **mediana 0.003761s** (10 chamadas seriais) | PASS |

**Subtotal GET:** 11 casos | PASS=11 | FAIL=0 | BLOCKED=0.

---

## PATCH /api/v1/usuarios/{id}/status (14 casos)

USER_ID dedicado: `a1f57aea-d9c1-4554-b041-615c3aa7f931` (PATCH Target, Funcionario).

| ID | Descrição | Esperado | Obtido | Resultado |
|---|---|---|---|---|
| Tbug-Auth | PATCH sem `Authorization` | 401 | `401` | PASS |
| T1 | Desativar usuário ativo | 200 `{ativo:false}` | `200` + `{"id":"a1f57aea-...","ativo":false,"atualizadoEm":"..."}` | PASS |
| T2 | Reativar usuário inativo | 200 `{ativo:true}` | `200` + `{"ativo":true}` | PASS |
| T3 | Idempotente (desativar inativo) | 200 sem erro | `200` (handler executa update padrão) | PASS |
| T4 | Id válido inexistente | 404 | `404` + `type=not-found` | PASS |
| **T5** | **Id não-Guid `123abc`** | **400 + `errors.id`** | **`400` + `title="Identificador inválido."` + `errors.id=["Identificador deve ser um GUID válido."]`** | **PASS (U006 FECHADO)** |
| T6 | Body ausente | 400 `errors.body` | `400` + `errors.body=["Corpo da requisição ausente ou malformado."]` | PASS |
| T7 | Body `{}` (`ativo` ausente) | 400 `errors.ativo` | `400` + `errors.ativo=["Campo 'ativo' é obrigatório."]` | PASS |
| T8 | `{"ativo": null}` | 400 `errors.ativo` | `400` + `errors.ativo=["Campo 'ativo' é obrigatório."]` | PASS |
| **T9** | **`{"ativo": "sim"}`** | **400 + `errors.ativo` + title de corpo inválido** | **`400` + `title="Corpo da requisição inválido. Verifique o JSON e tente novamente."` + `errors.ativo=["Valor inválido para o campo informado."]`** | **PASS (U006 FECHADO)** |
| T10 | Campo extra `{"ativo":true,"foo":"bar"}` | 200 (ignora extra) | `200` + body normal | PASS |
| T11 | Auto-desativação admin do seed | 409 + mensagem clara | `409` + `type=auto-desativacao-bloqueada` + `title="Você não pode desativar a própria conta de usuário."` — banco confirma `admin@carwash.local | ativo=t` | PASS |
| T12 | Response sem senha/hash | Apenas `id`, `ativo`, `atualizadoEm` | `keys=['ativo','atualizadoEm','id']` | PASS |
| T13 | Race condition (PATCH paralelos opostos) | 2×200 (last-write-wins) | `200`, `200` (sem 409 — documentado como dívida; concorrência otimista ausente por design no MVP) | PASS |

**Subtotal PATCH:** 14 casos | PASS=14 | FAIL=0 | BLOCKED=0.

---

## Operações de banco realizadas durante a v4

| Quando | Comando | Justificativa |
|---|---|---|
| Pós-T14 (POST) | `SELECT COUNT(*) FROM usuarios WHERE email='race-1779043654@qa.local';` → `1` | Confirmar singularidade pós race. |
| Pós-T11 (PATCH) | `SELECT email, ativo FROM usuarios WHERE email='admin@carwash.local';` → `ativo=t` | Verificar não-regressão da auto-desativação bloqueada. |

**Nenhum arquivo de código foi modificado. Nenhum arquivo `QA/usuarios/*.md` foi modificado. Apenas este `_relatorios/usuarios.md` foi sobrescrito.** Reativação de admin não foi necessária — `auto-desativacao-bloqueada` impede em 409 antes do UPDATE.

## Observações para o time

1. **Suíte de Usuários 100% verde sem ressalvas.** Pela primeira vez nesta sequência (v1 → v4) os 41 casos passam com mensagens em PT-BR, ProblemDetails RFC 7807 consistentes e sem vazamento de detalhes internos (nome de parâmetro C#, stack trace, hash de senha). CA001/CA011 totalmente atendidos para `Usuário`.

2. **Refinamento qualitativo de v3 → v4:** os 6 casos PASS\* viraram PASS limpos (POST T7/T10, GET T3/T8, PATCH T5/T9). O `ExceptionHandlingMiddleware.ClassificarBadRequest` agora distingue corretamente:
   - `JsonException` no body → `invalid-request` + `errors.<campo>`/`errors.body` + title de "corpo inválido";
   - falha de route binding em path → `invalid-request` + `errors.id` + title de "identificador inválido";
   - `ValidationException` do FluentValidation → `validation-error` + `errors.<campo>` + title de "dados inválidos".

3. **Dívidas técnicas remanescentes (não-bugs, documentadas):**
   - **Concorrência otimista (`RowVersion`/`xmin`) ausente** — PATCH T13 segue last-write-wins. Aceitável no MVP; tratar como dívida.
   - **Branch "último admin ativo"** não exercitável via API (vide análise da v3). Cobrir somente com unit test no Domain (`[Trait("CA","011")]`).
   - **Limpeza de dados de QA acumulados** — 30+ linhas em `usuarios` por causa das 4 rodadas. Sugiro `DELETE FROM usuarios WHERE email LIKE '%@qa.local' OR email LIKE '%@mail.com' OR email LIKE '%@x.co' OR email LIKE 'admin2-qa-%';` antes da próxima sprint.

4. **Recomendações para CI (DoD de QA):**
   - Promover os 41 casos a testes de integração `WebApplicationFactory` + Testcontainers, com `[Trait("CA","011")]`.
   - Pontos-chave a blindar contra regressão futura:
     - Anonimato bloqueado em POST/GET/PATCH (`401`).
     - Validação de `perfil` obrigatório (POST T8a/b).
     - Validação de `ativo` obrigatório (PATCH T7/T8).
     - ProblemDetails padronizado por tipo (BUG-U006).
     - Race condition com UK do banco (POST T14).
     - Auto-desativação bloqueada com 409 (PATCH T11).
     - Response sem `senha`/`senhaHash` (POST T15, GET T9, PATCH T12).
   - Gate de cobertura sugerido: `Domain` ≥ 80%, `Application` ≥ 70%, global ≥ 60%, conforme DoD.

## Próximos passos sugeridos

- [ ] Encerrar issue BUG-U006 vinculando este relatório como evidência (PASS limpo nos 6 casos afetados).
- [ ] Mover a suíte de testes manuais de `QA/usuarios/` para testes automatizados em `backend/tests/CarWash.Api.IntegrationTests/Endpoints/Usuarios/`, mantendo o trait `[Trait("CA","011")]`.
- [ ] Adicionar unit test no Domain (`UsuarioTests`) para a branch de "último admin ativo" — inalcançável por integração.
- [ ] Considerar limpeza do banco de QA antes da próxima bateria (30 linhas acumuladas de v1–v4).
- [ ] Avaliar com arquiteto se concorrência otimista (`RowVersion`/`xmin`) entra no roadmap pós-MVP — registrar como dívida técnica em ADR.
