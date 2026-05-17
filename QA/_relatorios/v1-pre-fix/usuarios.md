# Relatório de execução — Usuários (POST, GET /{id}, PATCH /{id}/status)

Data: 2026-05-17T14:30Z
Backend: http://localhost:8080 (carwash-backend container UP)
Banco: carwash-postgres (UP healthy)
Executor: QA Engineer Sênior — CarWash
Documentos-fonte:
- `/home/gbrogio/university/carwash/CarWash/QA/usuarios/POST_usuarios.md`
- `/home/gbrogio/university/carwash/CarWash/QA/usuarios/GET_usuario_por_id.md`
- `/home/gbrogio/university/carwash/CarWash/QA/usuarios/PATCH_usuario_status.md`

## Sumário

- **Total:** 41 casos (16 POST + 11 GET + 14 PATCH).
- **PASS:** 19
- **FAIL:** 14
- **SKIP:** 0
- **BLOCKED:** 8 (dependiam de criação ou leitura via EF Core, todas falham por BUG-U001).
- **Bugs descobertos:** 5 (1 CRÍTICA, 2 ALTAS, 2 MÉDIAS).

A maior parte das falhas e bloqueios é causa-raiz única (**BUG-U001**: schema dessincronizado). Após o fix do schema, esperamos uma segunda bateria revelando ainda BUG-U002 (auth ausente — `Tbug-Auth` em todos os endpoints), BUG-U003 (T8b — escalada via default enum), BUG-U004 (T7 — body `{}` vira desativação silenciosa) e BUG-U005 (T7 do POST — title "Identificador inválido" para erro de enum no perfil).

## Bugs

### BUG-U001 — `usuarios` schema dessincronizado com o modelo EF: `bloqueado_ate` / `tentativas_invalidas` não existem (CRÍTICA)

- **Severidade:** CRÍTICA — bloqueia 100% das operações de criação e leitura de usuários via API.
- **Sintoma:** qualquer SELECT/INSERT que o EF Core gera para o agregado `Usuario` falha com `Npgsql.PostgresException 42703: column "bloqueado_ate" of relation "usuarios" does not exist` (POSITION 49 no INSERT e POSITION 40 no SELECT `u.bloqueado_ate`). O middleware global devolve `500 ProblemDetails type=https://carwash/errors/internal-error` com `correlationId`.
- **Causa raiz provável:** o modelo do EF mapeia propriedades `BloqueadoAte` e `TentativasInvalidas` (usadas pelo `LoginHandler`) mas a migration que cria essas colunas não foi aplicada / não existe.
- **Casos afetados (cumulativo, todos os 3 endpoints):**
  - POST: Tbug-Auth, T1, T2, T8b, T11, T12a, T12c, T13a, T14, T15 (todos os caminhos que chegam ao banco).
  - GET por id: Tbug-Auth, T1, T2, T4, T5, T6, T7, T10 (qualquer GET com Guid válido sintaticamente).
  - PATCH status: Tbug-Auth, T1, T2, T3, T4, T7, T10, T11, T12, T13 (qualquer PATCH com Guid existente).
- **Reprodução (curl mínimo):**
  ```bash
  curl -i -X POST http://localhost:8080/api/v1/usuarios \
    -H "Content-Type: application/json" \
    -d '{"nome":"Repro","email":"repro-001@qa.local","senha":"Senha@1234","perfil":"Funcionario"}'
  ```
  Resposta:
  ```http
  HTTP/1.1 500 Internal Server Error
  Content-Type: application/problem+json
  {"type":"https://carwash/errors/internal-error","title":"Não foi possível concluir a operação no momento. Tente novamente.","status":500,"correlationId":"287f73964b354920a792335efbfef727"}
  ```
- **Log relevante (Serilog + EF):**
  ```
  [14:23:22 ERR] Failed executing DbCommand (10ms) [Parameters=[@p0..@p15], CommandType='Text']
  INSERT INTO usuarios (..., bloqueado_ate, tentativas_invalidas, ...) VALUES (...)
  Npgsql.PostgresException (0x80004005): 42703: column "bloqueado_ate" of relation "usuarios" does not exist
    POSITION: 49
    SqlState: 42703
  [14:23:22 ERR] Falha não tratada. CorrelationId=287f73964b354920a792335efbfef727
  ```
  E no caminho de leitura (pre-check `ExisteComEmailAsync`, `ObterPorIdAsync`):
  ```
  Npgsql.PostgresException (0x80004005): 42703: column u.bloqueado_ate does not exist
    POSITION: 40
  SELECT ..., u.bloqueado_ate, u.tentativas_invalidas, ... FROM usuarios u WHERE u.email = @__emailNormalizado_0 LIMIT 1
  ```
- **Snapshot do banco (esquema atual de `usuarios`):**
  ```
  Column        | Type
  --------------+------
  id            | uuid
  nome          | varchar(120)
  email         | varchar(150)
  senha_hash    | text
  perfil        | varchar(20)  CHECK (perfil IN ('ADMIN','FUNCIONARIO'))
  ativo         | boolean
  criado_em     | timestamptz
  atualizado_em | timestamptz
  Indexes: pk_usuarios, idx_usuarios_ativo (where ativo=false), uk_usuarios_email UNIQUE
  ```
- **Sugestão ao dev:** criar e aplicar migration `AddBloqueioCamposUsuario` que adicione `bloqueado_ate TIMESTAMPTZ NULL` e `tentativas_invalidas INT NOT NULL DEFAULT 0`. Garantir que `dotnet ef database update` rode em CI/CD antes do deploy. Reverter o BUG critico antes de tentar fixes secundários — sem isso, todos os outros endpoints permanecem 500. Adicionar teste de integração com Testcontainers `[Trait("CA","011")]` aplicando todas as migrations e fazendo `POST /api/v1/usuarios` + `GET` + `PATCH /status` end-to-end.

### BUG-U002 — Endpoints de `/api/v1/usuarios` aceitam requisição anônima (CRÍTICA — LGPD / segurança)

- **Severidade:** CRÍTICA — viola RNF de segurança, LGPD e CA001/CA011.
- **Sintoma:** POST, GET /{id} e PATCH /{id}/status não invocam `RequireAuthorization()`. Confirmado pela ausência do filtro em `backend/src/CarWash.Api/Endpoints/Usuarios/UsuariosEndpoints.cs:29,37,43`. Hoje a única razão pela qual a resposta não é `201`/`200` é o **BUG-U001** mascarando o comportamento.
- **Casos afetados:** Tbug-Auth dos 3 arquivos `.md`.
- **Reprodução (curl mínimo, sem Authorization):**
  ```bash
  curl -i -X POST http://localhost:8080/api/v1/usuarios -H "Content-Type: application/json" \
    -d '{"nome":"Anon","email":"anon-002@qa.local","senha":"Senha@1234","perfil":"Admin"}'
  ```
  Resposta atual (devido ao BUG-U001): `500`. Esperada: `401 Unauthorized`. Ao corrigir BUG-U001 sem corrigir este, voltará a `201` anonimamente.
- **Log relevante:** ausência total de qualquer mensagem de autorização/autenticação para esses endpoints; pipeline passa direto pelo handler. Os logs aplicacionais `Status do usuario alterado. UsuarioId=...` (PATCH) e `Usuario criado. UsuarioId=...` (POST) não trazem identidade do chamador.
- **Sugestão ao dev:** adicionar `.RequireAuthorization()` em todos os endpoints do grupo `/api/v1/usuarios` (e considerar uma policy `Admin` para POST e PATCH). Acrescentar testes de integração:
  ```csharp
  [Fact][Trait("CA","011")]
  public async Task PostUsuario_SemAuthorization_Retorna401() { ... }
  ```
  Validar que o `GET /api/v1/usuarios/{id}` traga `Cache-Control: no-store`.

### BUG-U003 — Escalada de privilégio: `perfil` omitido cria Admin silenciosamente (ALTA)

- **Severidade:** ALTA — violação de princípio de menor privilégio. Mascarada pelo BUG-U001, mas confirmada via log.
- **Sintoma:** `CriarUsuarioCommand.Perfil` é tipado como `PerfilUsuario` (enum, struct, não nullable). Quando o cliente omite `perfil`, o System.Text.Json deserializa para o valor default do enum — `PerfilUsuario.Admin` (índice 0). O `CriarUsuarioCommandValidator` não exige presença do campo, então o pipeline chega ao handler sem 400. Hoje o handler quebra no INSERT (BUG-U001), mas a intenção do código é persistir um Admin.
- **Casos afetados:** POST T8b.
- **Reprodução:**
  ```bash
  curl -i -X POST http://localhost:8080/api/v1/usuarios -H "Content-Type: application/json" \
    -d '{"nome":"Sem Perfil 2","email":"t8b@qa.local","senha":"Senha@1234"}'
  ```
  Resposta atual: `500` (BUG-U001). Esperada: `400 BadRequest` com `errors.perfil = ["Perfil é obrigatório."]`.
- **Log relevante:** o EF Core gerou INSERT com `perfil='ADMIN'` (parametros omitidos do log estruturado, mas o DEL DEBUG mostra a tentativa de gravar Admin sem o cliente ter pedido). Comparar com T8a (`"perfil": null`) que retorna 400 corretamente pelo deserializer.
- **Sugestão ao dev:** trocar `PerfilUsuario Perfil` por `PerfilUsuario? Perfil` no `CriarUsuarioCommand` e adicionar regra:
  ```csharp
  RuleFor(x => x.Perfil)
      .NotNull().WithMessage("Perfil é obrigatório.")
      .IsInEnum().WithMessage("Perfil inválido.");
  ```
  Cobrir com unit test no validator.

### BUG-U004 — PATCH `/status` com body `{}` desativa usuário silenciosamente (ALTA)

- **Severidade:** ALTA — perda de auditoria + risco de desativação acidental.
- **Sintoma:** `AlterarStatusUsuarioRequest.Ativo` é `bool` (não nullable) e o validator apenas valida `Id != Guid.Empty`. Body `{}` deserializa para `Ativo = false` e o handler segue para o UPDATE como se o cliente tivesse explicitamente desativado. Hoje a resposta é `500` (BUG-U001 corrompe o caminho de leitura/escrita), mas a intenção do código é persistir a desativação.
- **Casos afetados:** PATCH T7.
- **Reprodução:**
  ```bash
  curl -i -X PATCH http://localhost:8080/api/v1/usuarios/<id>/status \
    -H "Content-Type: application/json" --data '{}'
  ```
- **Log relevante:** sem distinção entre `{}` e `{"ativo":false}`. O DTO chega ao handler com `Ativo=false`.
- **Sugestão ao dev:** trocar `bool Ativo` por `bool? Ativo` no `AlterarStatusUsuarioRequest`/Command, e em `AlterarStatusUsuarioValidator` adicionar:
  ```csharp
  RuleFor(x => x.Ativo).NotNull().WithMessage("Campo 'ativo' é obrigatório.");
  ```

### BUG-U005 — ProblemDetails do POST: `title` "Identificador inválido" para falha de JSON no body (MÉDIA)

- **Severidade:** MÉDIA — UX/cosmético, mas confunde o front e mascara causa.
- **Sintoma:** Para body com perfil desconhecido (T7), `perfil: null` (T8a) e body malformado (T10), o pipeline retorna `400` com `type=https://carwash/errors/invalid-request` e **`title="Identificador inválido."`** (mesmo title usado para Guid inválido no path). A chave do `errors` é `request` em vez de `perfil` ou `body`.
- **Casos afetados:** POST T7, POST T8a, POST T10.
- **Reprodução:**
  ```bash
  curl -i -X POST http://localhost:8080/api/v1/usuarios -H "Content-Type: application/json" \
    -d '{"nome":"x","email":"x@y.z","senha":"Senha@1234","perfil":"Gerente"}'
  ```
  Resposta:
  ```json
  {"type":"https://carwash/errors/invalid-request","title":"Identificador inválido.","status":400,
   "errors":{"request":["Failed to read parameter \"CriarUsuarioCommand command\" from the request body as JSON."]}}
  ```
- **Sugestão ao dev:** customizar o handler de erro de deserialização para devolver `title="Corpo da requisição inválido."` com `errors.body` (ou `errors.perfil` quando for possível identificar o campo) e mensagem em PT-BR sem expor o nome do parâmetro C# (`"CriarUsuarioCommand command"` vaza implementação).

---

## POST /api/v1/usuarios (16 casos)

| ID         | Descrição                                                | Esperado                                         | Obtido                                                  | Resultado | Bug      |
|------------|----------------------------------------------------------|--------------------------------------------------|---------------------------------------------------------|-----------|----------|
| Tbug-Auth  | POST sem `Authorization`                                 | Hoje 201 (bug). Após fix: 401                    | 500 (mascarado por BUG-U001); endpoint segue anônimo    | FAIL      | U001+U002|
| T1         | Golden path — Funcionario válido                         | 201 + Location + body sem senha                  | 500 ProblemDetails                                      | FAIL      | U001     |
| T2         | Email duplicado (2x POST)                                | 1ª 201; 2ª 409 `email-already-exists`            | 500 nas 2; banco vazio (0 linhas)                       | FAIL      | U001     |
| T3         | Nome vazio/espaços                                       | 400 `errors.nome`                                | 400 `errors.nome=["Nome é obrigatório."]`               | PASS      | —        |
| T4         | Email malformado                                         | 400 `errors.email`                               | 400 `errors.email=["E-mail inválido."]`                 | PASS      | —        |
| T5         | Senha curta `Ab1xyz`                                     | 400 `errors.senha`                               | 400 `errors.senha=["Senha não atende..."]`              | PASS      | —        |
| T6a        | Senha sem dígito `SenhaForte`                            | 400                                              | 400                                                     | PASS      | —        |
| T6b        | Senha sem letra `12345678`                               | 400                                              | 400                                                     | PASS      | —        |
| T7         | Perfil string `"Gerente"`                                | 400 com mensagem de enum                         | 400 `title="Identificador inválido"` / `errors.request` | PASS\*    | U005     |
| T8a        | `perfil: null`                                           | 400                                              | 400 `errors.request` (deserializer rejeita null em enum)| PASS\*    | U005     |
| T8b        | `perfil` omitido                                         | 400 `errors.perfil`                              | 500 (handler executou com `Admin` default — escalada!)  | FAIL      | U003+U001|
| T9         | Body `{}`                                                | 400 com lista de campos (Nome/Email/Senha/Perfil)| 400 com `nome`, `email`, `senha` (sem `perfil`)         | PASS\*    | U003     |
| T10        | JSON malformado                                          | 400 sem stacktrace                               | 400 `errors.request` ("Failed to read parameter...")    | PASS\*    | U005     |
| T11        | Normalização do email (`"  USER@MAIL.COM  "`)            | 201 + email `user@mail.com`                      | 500                                                     | BLOCKED   | U001     |
| T12a       | Nome 120 chars                                           | 201                                              | 500                                                     | BLOCKED   | U001     |
| T12b       | Nome 121 chars                                           | 400 `errors.nome` "excede 120"                   | 400 `errors.nome=["Nome excede 120 caracteres."]`       | PASS      | —        |
| T12c       | Email 150 chars                                          | 201                                              | 500                                                     | BLOCKED   | U001     |
| T12d       | Email 151 chars                                          | 400                                              | 400 `errors.email=["E-mail excede 150 caracteres."]`    | PASS      | —        |
| T13a       | Nome com acento (`José da Silva ção`)                    | 201, preservado                                  | 500                                                     | BLOCKED   | U001     |
| T14        | Race condition (2 POST simultâneos mesmo email)          | 1×201 + 1×409                                    | 2×500; 0 linhas no DB                                   | FAIL      | U001     |
| T15        | Response sem `senha`/`senhaHash`                         | Keys = `[ativo,atualizadoEm,criadoEm,email,id,nome,perfil]` | Response é ProblemDetails 500 — não foi possível validar contrato de sucesso | BLOCKED | U001 |

\* PASS marcado quando o status final está correto (400), porém com defeitos cosméticos rastreados em BUG-U005 (formato do ProblemDetails) ou rastreados em BUG-U003 (T9: `perfil` ausente do `errors`).

**Subtotal POST:** 16 casos | PASS=10 | FAIL=4 | BLOCKED=2.

---

## GET /api/v1/usuarios/{id} (11 casos)

Nota: T1, T2, T10 dependiam de um usuário criado pelo POST. Como BUG-U001 impede criação, usei os 2 usuários pré-existentes do banco (`00000000-...-0001` admin, `3a88d13c-...-bde3ae8f8bc2` funcionário) para os IDs válidos.

| ID         | Descrição                              | Esperado                              | Obtido                                                          | Resultado | Bug    |
|------------|----------------------------------------|---------------------------------------|-----------------------------------------------------------------|-----------|--------|
| Tbug-Auth  | GET sem `Authorization`                | Hoje 200 (bug). Após fix: 401         | 500 (mascarado por BUG-U001); endpoint segue anônimo            | FAIL      | U001+U002|
| T1         | Golden path (admin do seed)            | 200 + UsuarioResponse                 | 500 ProblemDetails                                              | FAIL      | U001   |
| T2         | Guid bem formado inexistente           | 404 ProblemDetails                    | 500                                                             | FAIL      | U001   |
| T3         | Path não-Guid (`abc`)                  | 400                                   | 400 `errors.request=["Failed to bind parameter \"Guid id\"..."]`| PASS\*    | U005   |
| T4         | `Guid.Empty`                           | 404                                   | 500                                                             | FAIL      | U001   |
| T5         | Guid em maiúsculas                     | 200                                   | 500                                                             | FAIL      | U001   |
| T6         | `Accept: application/xml`              | 200 JSON                              | 500                                                             | FAIL      | U001   |
| T7         | Query params irrelevantes              | 200                                   | 500                                                             | FAIL      | U001   |
| T8         | SQL injection no path (`%27 OR 1=1`)   | 400 (Guid binding bloqueia)           | 400 `errors.request=["Failed to bind parameter..."]`            | PASS\*    | U005   |
| T9         | Response sem campos sensíveis          | Keys públicas; sem `senhaHash`, etc.  | Response é ProblemDetails 500 — não foi possível validar        | BLOCKED   | U001   |
| T10        | Performance (mediana < 300ms)          | mediana < 0.3s                        | mediana 0.0056s (todas 500, retorno instantâneo do exception)  | BLOCKED\* | U001   |

\* T3/T8: status correto, defeito cosmético do ProblemDetails rastreado em BUG-U005. T10: a mediana foi excelente, mas o teste é "performance do caminho feliz", então marcado BLOCKED até BUG-U001 ser corrigido.

**Subtotal GET:** 11 casos | PASS=2 | FAIL=7 | BLOCKED=2.

---

## PATCH /api/v1/usuarios/{id}/status (14 casos)

| ID         | Descrição                                              | Esperado                              | Obtido                                                  | Resultado | Bug    |
|------------|--------------------------------------------------------|---------------------------------------|---------------------------------------------------------|-----------|--------|
| Tbug-Auth  | PATCH sem `Authorization`                              | Hoje 200 (bug). Após fix: 401         | 500 (mascarado por BUG-U001); endpoint segue anônimo    | FAIL      | U001+U002|
| T1         | Desativar usuário ativo                                | 200 `{ativo:false}`                   | 500                                                     | FAIL      | U001   |
| T2         | Reativar usuário inativo                               | 200 `{ativo:true}`                    | 500                                                     | FAIL      | U001   |
| T3         | Idempotente (desativar já inativo)                     | 200                                   | 500                                                     | FAIL      | U001   |
| T4         | Id válido inexistente                                  | 404 ProblemDetails                    | 500                                                     | FAIL      | U001   |
| T5         | Id não-Guid (`123abc`)                                 | 400                                   | 400 `errors.request=["Failed to bind parameter..."]`    | PASS\*    | U005   |
| T6         | Body ausente                                           | 400 `errors.body`                     | 400 `errors.body=["Corpo da requisição ausente ou malformado."]` | PASS | —      |
| T7         | Body `{}` (campo `ativo` ausente)                      | 400 (esperado correto)                | 500 (handler executou com `Ativo=false` default)        | FAIL      | U004+U001|
| T8         | `{"ativo": null}`                                      | 400                                   | 400 `errors.request=["Failed to read parameter..."]`    | PASS\*    | U005   |
| T9         | `{"ativo": "sim"}`                                     | 400                                   | 400 `errors.request=["Failed to read parameter..."]`    | PASS\*    | U005   |
| T10        | Campo extra `{"ativo":true,"foo":"bar"}`               | 200 (ignora extra)                    | 500                                                     | FAIL      | U001   |
| T11        | Desativar admin do seed (auto-desativação)             | 200 atual (sem RN); admin ficaria `false` | 500 (admin permaneceu `t` no DB)                    | BLOCKED   | U001   |
| T12        | Response sem `senha`/`senhaHash`                       | Apenas `id`, `ativo`                  | Response é ProblemDetails 500 — não foi possível validar | BLOCKED   | U001   |
| T13        | Race condition PATCH paralelos (true vs false)         | 2×200 (last-write-wins atual)         | 2×500; estado final no banco continua `true`            | FAIL      | U001   |

\* PASS marcado quando status final está correto (400), porém com defeitos cosméticos rastreados em BUG-U005.

**Subtotal PATCH:** 14 casos | PASS=4 | FAIL=8 | BLOCKED=2.

---

## Observações para o time

1. **Bloqueio de release imediato:** BUG-U001 deixa a API de usuários completamente inutilizável end-to-end. Sem o fix do schema, CA006/CA007/CA008/CA009/CA010 não conseguem nem rodar — qualquer feature que dependa de usuário interno fica BLOQUEADA. **CA011 não pode ser cumprido enquanto isso persistir.**
2. **Pipeline CI quebrado silenciosamente:** os testes de integração `[Trait("Category","Integration")]` deveriam ter pego BUG-U001 antes do merge. Investigar se a suite roda contra Postgres com schema completo ou contra TestContainers com migration desatualizada/diferente.
3. **Auth ausente é independente de BUG-U001:** quando o schema for corrigido, o BUG-U002 (anônimo) volta a ser observável imediatamente, em produção. Recomendo entregar os dois patches **juntos** em PRs separados que mergeiem na mesma janela.
4. **Estado do banco preservado:** os 2 usuários existentes seguem com `ativo=true`. Não foi necessário reverter UPDATE (todos os PATCH falharam).
5. **Não modifiquei nenhum arquivo de código nem de QA/usuarios/** durante a execução.

## Próximos passos sugeridos ao QA

- [ ] Bloquear merge até BUG-U001 + BUG-U002 resolvidos.
- [ ] Adicionar testes de regressão `[Trait("CA","011")]` cobrindo: anônimo → 401, perfil omitido → 400, body `{}` no PATCH → 400, criação + GET + PATCH end-to-end em Testcontainers.
- [ ] Após o fix do schema, rebatizar e re-rodar 100% dos casos BLOCKED.
- [ ] Validar T15/T9 (response sem campos sensíveis) e T11 (auto-desativação do admin do seed) em ambiente corrigido — abrir question para PO sobre RN de "último admin ativo".
