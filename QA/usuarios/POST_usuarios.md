# POST /api/v1/usuarios

## Resumo

- **Metodo:** POST
- **Path:** `/api/v1/usuarios`
- **Proposito:** criar usuario interno (Admin ou Funcionario) do CarWash.
- **Autenticacao observada:** **NENHUMA** — a rota nao chama `RequireAuthorization()` em `backend/src/CarWash.Api/Endpoints/Usuarios/UsuariosEndpoints.cs:29`. Qualquer requisicao anonima cria usuario. Documentado como `Tbug-Auth`.
- **Autenticacao esperada (apos correcao):** Bearer JWT de Admin (RF014). Anonimo deve responder `401 Unauthorized`.
- **Produces:** `201 Created` | `400 BadRequest` | `409 Conflict` | `500 InternalServerError`.
- **Content-Type esperado:** `application/json`.

## Pre-requisitos

- Backend em execucao em `http://localhost:8080` (`dotnet run --project backend/src/CarWash.Api`).
- PostgreSQL up com migrations aplicadas (tabela `usuarios` com UK `uk_usuarios_email`).
- Opcional para uso futuro (quando a rota for protegida): variavel `ACCESS_TOKEN` com JWT valido de perfil Admin.

```bash
export BASE_URL="http://localhost:8080"
# Opcional (uso futuro):
# export ACCESS_TOKEN="eyJhbGciOi..."
```

## Resumo dos casos

| ID         | Cenario                                                            | Esperado                                                  |
|------------|--------------------------------------------------------------------|-----------------------------------------------------------|
| Tbug-Auth  | POST sem header `Authorization`                                    | **Atual: 201 (BUG).** Esperado: 401 Unauthorized          |
| T1         | Golden path — payload valido com perfil `Funcionario`              | 201 + `Location` + body sem senha                         |
| T2         | Email duplicado (segundo POST com mesmo email)                     | 409 + ProblemDetails `email-already-exists`               |
| T3         | Nome vazio ou apenas espacos                                       | 400                                                       |
| T4         | Email malformado                                                   | 400                                                       |
| T5         | Senha curta (< 8 caracteres)                                       | 400                                                       |
| T6         | Senha sem digito ou sem letra                                      | 400                                                       |
| T7         | Perfil string desconhecida (`"Gerente"`)                           | 400 (enum binding)                                        |
| T8         | Perfil `null` ou omitido                                           | 400; risco: enum struct default-ando para `Admin`         |
| T9         | Body vazio `{}`                                                    | 400 com lista de campos                                   |
| T10        | Body com JSON malformado                                           | 400                                                       |
| T11        | Normalizacao do email (`"  USER@MAIL.COM  "`)                      | 201; armazenado como `user@mail.com`                      |
| T12        | Boundaries de tamanho (nome 120/121; email 150/151)                | 201 / 400 / 201 / 400                                     |
| T13        | Unicode e emoji no nome                                            | 201                                                       |
| T14        | Race condition — dois POSTs simultaneos com mesmo email            | 1x 201 + 1x 409 (UK do banco)                             |
| T15        | Resposta nunca expoe senha nem hash                                | 201 sem campos `Senha`, `SenhaHash`, etc.                 |

---

## Tbug-Auth — POST sem autenticacao (BUG CRITICO)

Confirma o achado: a rota nao exige autenticacao. Em producao, isso permite a qualquer pessoa criar conta Admin. Bug critico de LGPD/seguranca.

```bash
curl -i -X POST "$BASE_URL/api/v1/usuarios" \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Sem Auth",
    "email": "sem.auth@carwash.local",
    "senha": "Senha@1234",
    "perfil": "Admin"
  }'
```

**Comportamento atual (bug):**

- Status: `201 Created`.
- Header: `Location: /api/v1/usuarios/{id}`.
- Body: `UsuarioResponse` com Admin recem criado.

**Comportamento esperado apos correcao:**

- Status: `401 Unauthorized`.
- Body: `ProblemDetails` sem expor que o endpoint existe alem do necessario.

**Logs Serilog:**

- Atual: `"Usuario criado. UsuarioId=..., Email=sem.auth@carwash.local"` — registrando criacao anonima.
- Esperado: log de tentativa de acesso nao autorizado, sem persistir o usuario.

**Sinais de bug:**

- Qualquer 2xx ao chamar a rota sem `Authorization`.
- Ausencia de middleware/policy de autorizacao no pipeline para essa rota.

---

## T1 — Golden path

Cria um Funcionario com payload valido. Verifica status, header `Location`, formato do body e ausencia de campos sensiveis.

```bash
curl -i -X POST "$BASE_URL/api/v1/usuarios" \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Maria Souza",
    "email": "maria.souza@carwash.local",
    "senha": "Senha@1234",
    "perfil": "Funcionario"
  }'
```

```json
{
  "nome": "Maria Souza",
  "email": "maria.souza@carwash.local",
  "senha": "Senha@1234",
  "perfil": "Funcionario"
}
```

**Resposta esperada:**

- Status: `201 Created`.
- Header: `Location: /api/v1/usuarios/{id}` (mesmo `id` do body).
- Body (`UsuarioResponse`):

```json
{
  "id": "f1d2c3b4-...",
  "nome": "Maria Souza",
  "email": "maria.souza@carwash.local",
  "perfil": "Funcionario",
  "ativo": true,
  "criadoEm": "2026-05-17T12:34:56.789Z",
  "atualizadoEm": "2026-05-17T12:34:56.789Z"
}
```

**Logs Serilog (Information):**

- `"Usuario criado. UsuarioId={Id}, Email={Email}"` com email normalizado.

**Sinais de bug:**

- Status diferente de 201.
- `Location` ausente, vazio ou apontando para path diferente do `id` retornado.
- Campos `senha`, `senhaHash`, `passwordHash` no body.
- `criadoEm` != `atualizadoEm` na criacao.

---

## T2 — Email duplicado

Executa T1 duas vezes. O segundo POST deve cair na verificacao pre-insert ou na UK do banco.

```bash
curl -i -X POST "$BASE_URL/api/v1/usuarios" \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Maria Souza 2",
    "email": "maria.souza@carwash.local",
    "senha": "Senha@1234",
    "perfil": "Funcionario"
  }'
```

**Resposta esperada:**

- Status: `409 Conflict`.
- Body `ProblemDetails` com `type` contendo slug `email-already-exists`:

```json
{
  "type": "https://carwash.local/errors/email-already-exists",
  "title": "Email ja cadastrado",
  "status": 409,
  "detail": "Ja existe usuario com o email informado."
}
```

**Logs Serilog:**

- Warning: `"Tentativa de criar usuario com email duplicado. Email=maria.souza@carwash.local"`.
- Possivel Debug: violacao de UK `uk_usuarios_email` (SQL state 23505) capturada e traduzida para 409.

**Sinais de bug:**

- 500 em vez de 409 → exception handler nao trata `PostgresException` com SQL state `23505`.
- `type` sem o slug esperado.
- Mensagem expondo SQL ou stack trace.

---

## T3 — Nome vazio ou apenas espacos

```bash
curl -i -X POST "$BASE_URL/api/v1/usuarios" \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "   ",
    "email": "t3@carwash.local",
    "senha": "Senha@1234",
    "perfil": "Funcionario"
  }'
```

**Resposta esperada:**

- Status: `400 BadRequest`.
- Body `ProblemDetails` com `errors.Nome` contendo mensagem do validator (regra 1..120, nao pode ser vazio).

**Logs Serilog:**

- Warning de validacao com campos invalidados.

**Sinais de bug:**

- Status 201 (validator nao trim-a o input).
- `errors` ausente.

---

## T4 — Email malformado

```bash
curl -i -X POST "$BASE_URL/api/v1/usuarios" \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Email Ruim",
    "email": "nao-e-email",
    "senha": "Senha@1234",
    "perfil": "Funcionario"
  }'
```

**Resposta esperada:**

- Status: `400 BadRequest`.
- `errors.Email` com mensagem do validator (`EmailAddress`).

**Sinais de bug:**

- Status 201 (Email VO ou validator nao validando formato).
- 500 (excecao do VO escapando para o pipeline).

---

## T5 — Senha curta

```bash
curl -i -X POST "$BASE_URL/api/v1/usuarios" \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Senha Curta",
    "email": "t5@carwash.local",
    "senha": "Ab1xyz",
    "perfil": "Funcionario"
  }'
```

**Resposta esperada:**

- Status: `400 BadRequest`.
- `errors.Senha` mencionando minimo de 8 caracteres.

**Sinais de bug:**

- Status 201 (politica NIST 800-63B nao aplicada).
- Mensagem expondo a senha enviada.

---

## T6 — Senha sem digito ou sem letra

Sem digito:

```bash
curl -i -X POST "$BASE_URL/api/v1/usuarios" \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Sem Digito",
    "email": "t6a@carwash.local",
    "senha": "SenhaForte",
    "perfil": "Funcionario"
  }'
```

Sem letra:

```bash
curl -i -X POST "$BASE_URL/api/v1/usuarios" \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Sem Letra",
    "email": "t6b@carwash.local",
    "senha": "12345678",
    "perfil": "Funcionario"
  }'
```

**Resposta esperada:**

- Ambos: `400 BadRequest` com `errors.Senha` indicando exigencia de letra E digito.

**Sinais de bug:**

- Status 201 em qualquer um dos dois casos.
- Validator exigindo caixa mista ou caractere especial (politica documentada como NIST 800-63B: apenas letra + digito).

---

## T7 — Perfil string desconhecida

```bash
curl -i -X POST "$BASE_URL/api/v1/usuarios" \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Perfil Errado",
    "email": "t7@carwash.local",
    "senha": "Senha@1234",
    "perfil": "Gerente"
  }'
```

**Resposta esperada:**

- Status: `400 BadRequest`.
- Mensagem do binder de enum (`The JSON value could not be converted to ... PerfilUsuario`) ou do validator.

**Sinais de bug:**

- Status 201 com `perfil` aceitando string arbitraria.
- 500 com stack trace de `JsonException`.

---

## T8 — Perfil null ou omitido

Com `perfil: null`:

```bash
curl -i -X POST "$BASE_URL/api/v1/usuarios" \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Sem Perfil",
    "email": "t8a@carwash.local",
    "senha": "Senha@1234",
    "perfil": null
  }'
```

Omitido:

```bash
curl -i -X POST "$BASE_URL/api/v1/usuarios" \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Sem Perfil 2",
    "email": "t8b@carwash.local",
    "senha": "Senha@1234"
  }'
```

**Resposta esperada:**

- Status: `400 BadRequest`.
- `errors.Perfil` mencionando obrigatoriedade.

**Risco confirmado:**

- `PerfilUsuario` e um `enum` (struct, nao nullable). Se o command tipar como `PerfilUsuario` direto (sem `?`), o binder pode default-ar para o primeiro valor — geralmente `Admin`. Verifique o banco: se T8 criar Admin silenciosamente, **e bug de escalada de privilegio**.

**Sinais de bug:**

- Status 201 e usuario persistido com `perfil = "Admin"`.
- Status 201 e `perfil` ausente do response (binder swallow).

---

## T9 — Body vazio

```bash
curl -i -X POST "$BASE_URL/api/v1/usuarios" \
  -H "Content-Type: application/json" \
  -d '{}'
```

**Resposta esperada:**

- Status: `400 BadRequest`.
- ProblemDetails com `errors` listando `Nome`, `Email`, `Senha`, `Perfil`.

**Sinais de bug:**

- 500 (NRE no handler antes da validacao).
- 400 sem `errors` por campo.

---

## T10 — Body malformado

```bash
curl -i -X POST "$BASE_URL/api/v1/usuarios" \
  -H "Content-Type: application/json" \
  -d '{ "nome": "x", "email": "x@y.z", '
```

**Resposta esperada:**

- Status: `400 BadRequest`.
- ProblemDetails generico de JSON invalido (sem stack trace).

**Sinais de bug:**

- 500 com `JsonException` vazando.

---

## T11 — Normalizacao do email

`Email` VO faz `Trim().ToLowerInvariant()`. O email gravado deve ficar minusculo e sem espacos.

```bash
curl -i -X POST "$BASE_URL/api/v1/usuarios" \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Normaliza",
    "email": "  USER@MAIL.COM  ",
    "senha": "Senha@1234",
    "perfil": "Funcionario"
  }'
```

**Resposta esperada:**

- Status: `201 Created`.
- Body: `"email": "user@mail.com"`.
- Segundo POST com `"user@mail.com"` ou `"User@Mail.com"` deve retornar `409` (T2).

**Sinais de bug:**

- `email` no response com caixa original ou espacos.
- Duas linhas no banco com mesmo email em capitalizacoes diferentes (UK deveria prevenir, mas se normalizacao falhar, UK pode aceitar duplicatas case-sensitive).

---

## T12 — Boundaries de tamanho

Nome 120 chars (esperado 201):

```bash
NOME120=$(printf 'a%.0s' {1..120})
curl -i -X POST "$BASE_URL/api/v1/usuarios" \
  -H "Content-Type: application/json" \
  -d "{
    \"nome\": \"$NOME120\",
    \"email\": \"t12a@carwash.local\",
    \"senha\": \"Senha@1234\",
    \"perfil\": \"Funcionario\"
  }"
```

Nome 121 chars (esperado 400):

```bash
NOME121=$(printf 'a%.0s' {1..121})
curl -i -X POST "$BASE_URL/api/v1/usuarios" \
  -H "Content-Type: application/json" \
  -d "{
    \"nome\": \"$NOME121\",
    \"email\": \"t12b@carwash.local\",
    \"senha\": \"Senha@1234\",
    \"perfil\": \"Funcionario\"
  }"
```

Email 150 chars (local part longo, dominio curto — esperado 201):

```bash
LOCAL=$(printf 'a%.0s' {1..138})
curl -i -X POST "$BASE_URL/api/v1/usuarios" \
  -H "Content-Type: application/json" \
  -d "{
    \"nome\": \"Email Limite\",
    \"email\": \"${LOCAL}@x.co\",
    \"senha\": \"Senha@1234\",
    \"perfil\": \"Funcionario\"
  }"
```

Email 151 chars (esperado 400):

```bash
LOCAL=$(printf 'a%.0s' {1..139})
curl -i -X POST "$BASE_URL/api/v1/usuarios" \
  -H "Content-Type: application/json" \
  -d "{
    \"nome\": \"Email Estouro\",
    \"email\": \"${LOCAL}@x.co\",
    \"senha\": \"Senha@1234\",
    \"perfil\": \"Funcionario\"
  }"
```

**Sinais de bug:**

- 201 acima do limite (validator desligado).
- 500 em estouro (excecao do banco em vez de validacao de aplicacao).
- 400 dentro do limite (off-by-one).

---

## T13 — Unicode e emoji no nome

```bash
curl -i -X POST "$BASE_URL/api/v1/usuarios" \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Jose da Silva",
    "email": "t13@carwash.local",
    "senha": "Senha@1234",
    "perfil": "Funcionario"
  }'
```

```bash
curl -i -X POST "$BASE_URL/api/v1/usuarios" \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Lava Jato Top",
    "email": "t13b@carwash.local",
    "senha": "Senha@1234",
    "perfil": "Funcionario"
  }'
```

**Resposta esperada:**

- Status: `201 Created`.
- `nome` preservado byte a byte no body.

**Sinais de bug:**

- Status 400 negando caracteres acentuados.
- 201 com nome truncado ou com `?` no lugar de glyphs.

---

## T14 — Race condition

Dois POSTs simultaneos com o mesmo email. Apenas um pode vencer; o outro deve receber 409 via UK do banco.

```bash
BODY='{"nome":"Race","email":"race@carwash.local","senha":"Senha@1234","perfil":"Funcionario"}'

printf '%s\n%s\n' "$BODY" "$BODY" | \
  xargs -P2 -I{} -d'\n' curl -s -o /dev/null -w "%{http_code}\n" \
    -X POST "$BASE_URL/api/v1/usuarios" \
    -H "Content-Type: application/json" \
    -d {}
```

**Resposta esperada:**

- Saida com exatamente `201` e `409` (em qualquer ordem).
- Apenas uma linha em `usuarios` para `race@carwash.local`.

**Logs Serilog:**

- Um Information com criacao bem-sucedida.
- Um Warning indicando violacao de UK convertida em 409 (ou pre-check rejeitando se a ordem permitir).

**Sinais de bug:**

- `201` e `201` → UK ausente ou pre-check sem transacao serializavel.
- `500` em um dos dois → excecao de UK escapando sem tratamento.
- Duas linhas no banco com mesmo email → integridade quebrada.

---

## T15 — Resposta nunca expoe senha nem hash

Re-execute T1 e inspecione o body completo do response. Em nenhum caso pode haver:

- `senha`
- `senhaHash`
- `passwordHash`
- `password`
- qualquer string longa em base64 ou hex que pareca um hash bcrypt/argon2.

```bash
curl -s -X POST "$BASE_URL/api/v1/usuarios" \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Audit Senha",
    "email": "t15@carwash.local",
    "senha": "Senha@1234",
    "perfil": "Funcionario"
  }' | jq 'keys'
```

**Resposta esperada:**

- Chaves: `["ativo","atualizadoEm","criadoEm","email","id","nome","perfil"]` (ordem pode variar).

**Sinais de bug:**

- Qualquer chave relacionada a senha presente no body.
- Hash aparecendo em logs do Serilog (verificar `dotnet run` console e arquivos de log).

---

## Bugs e crashes a observar

- **Criacao anonima permitida (Tbug-Auth):** a rota nao tem `RequireAuthorization()`. Bug critico — qualquer um cria Admin.
- **500 em vez de 409 na UK:** se `PostgresException` com SQL state `23505` (`uk_usuarios_email`) nao for traduzida pelo exception handler.
- **Vazamento de hash de senha:** qualquer campo derivado de senha aparecendo no `UsuarioResponse` ou em logs.
- **ProblemDetails 400 sem `errors` por campo:** dificulta uso pelo front; checar `ValidationProblemDetails`.
- **Header `Location` ausente ou apontando para path errado:** `CreatedAtRoute` mal configurado.
- **Race criando 2 usuarios com mesmo email:** UK faltando, migration nao aplicada ou pre-check sem isolation adequada.
- **Enum struct default-ando para `Admin` quando perfil omitido (T8):** escalada de privilegio silenciosa.
- **Email VO nao normalizando (T11):** abre porta para duplicatas case-sensitive.
- **Off-by-one nos limites (T12):** 120/121 e 150/151.
- **Excecoes de JSON malformado (T10) escapando como 500.**

## Como reportar para o dev

Ao abrir o ticket para `dev-dotnet-carwash`, inclua:

1. **ID do caso** (Tbug-Auth, T1..T15) e descricao curta do cenario.
2. **Comando curl exato** usado, com `BASE_URL` resolvido.
3. **Request body** (JSON) e headers enviados.
4. **Response real:** status, headers relevantes (`Location`, `Content-Type`), body completo.
5. **Response esperado** conforme este documento.
6. **Trecho de log Serilog** correlato (com `UsuarioId` quando aplicavel).
7. **Snapshot do banco** (`SELECT id, nome, email, perfil, ativo, criado_em FROM usuarios WHERE email = '...'`) quando o bug envolver persistencia.
8. **Referencias de codigo:**
   - Endpoint: `backend/src/CarWash.Api/Endpoints/Usuarios/UsuariosEndpoints.cs:29`
   - Command: `CriarUsuarioCommand`
   - Validator: `CriarUsuarioValidator`
   - VO: `Email`
   - Constraint: `uk_usuarios_email`
9. **Severidade sugerida:**
   - Critica: Tbug-Auth, T8 (escalada via default enum), T15 (vazamento de hash), T14 (duplicacao real).
   - Alta: T2 retornando 500, T11 falha de normalizacao.
   - Media: T7/T10 com 500 em vez de 400, T12 off-by-one.
   - Baixa: T13 unicode rejeitado.
10. **Reproducao minima:** comando unico que dispara o bug em ambiente limpo (estado do banco descrito).
