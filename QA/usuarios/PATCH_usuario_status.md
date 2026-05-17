# PATCH /api/v1/usuarios/{id}/status — Roteiro de Testes Manuais

## Resumo

- **Metodo:** `PATCH`
- **Path:** `/api/v1/usuarios/{id}/status`
- **Proposito:** alternar o flag `Ativo` de um usuario interno (ativacao/desativacao logica). Suporta RF014 e a politica de revogacao de acesso sem exclusao fisica.
- **Autenticacao observada no codigo:** **ANONIMA**. O endpoint NAO declara `RequireAuthorization()` em `backend/src/CarWash.Api/Endpoints/Usuarios/UsuariosEndpoints.cs:43`. Esperado pelo DRP/DAT: rota administrativa restrita (401 para anonimo, 403 para perfil sem permissao).
- **Respostas declaradas (`Produces`):** `200 OK`, `400 Bad Request`, `404 Not Found`, `500 Internal Server Error`.
- **Concorrencia:** **sem controle otimista** — last-write-wins.
- **Handler:** `AlterarStatusAsync` → `AlterarStatusUsuarioCommand` → `AlterarStatusUsuarioResponse`.
- **Validator:** `AlterarStatusUsuarioValidator` valida apenas `Id != Guid.Empty`. **Nao valida presenca explicita de `Ativo`** (bool default `false`).

## Pre-requisitos

1. Backend rodando em `http://localhost:8080` (perfil `Development`).
2. Banco PostgreSQL acessivel e migrado (seed do admin aplicado).
3. Ferramenta `curl` instalada; opcionalmente `jq` para inspecao de JSON.
4. Criar um usuario de teste via `POST /api/v1/usuarios` e guardar o `id` retornado em `USER_ID`.
5. Ter um segundo `id` invalido (`00000000-0000-0000-0000-000000000000` ou um Guid aleatorio nao persistido) para os cenarios negativos.
6. Para o caso T11, descobrir o `id` do admin do seed via `GET /api/v1/usuarios?email=admin@carwash.local` (ou consulta direta a tabela `usuarios`).

```bash
# Exemplo de criacao previa
USER_ID=$(curl -s -X POST http://localhost:8080/api/v1/usuarios \
  -H "Content-Type: application/json" \
  --data '{"nomeCompleto":"QA Teste","email":"qa.patch.status@carwash.local","senha":"Senha@123","perfil":"Atendente"}' \
  | jq -r '.id')
echo $USER_ID
```

## Tabela resumo dos casos

| Caso       | Cenario                                             | Body                              | Status esperado | Observacao                                      |
|------------|-----------------------------------------------------|-----------------------------------|-----------------|-------------------------------------------------|
| Tbug-Auth  | Requisicao anonima (sem `Authorization`)            | `{"ativo": false}`                | 401 esperado    | Atualmente **altera status** — BUG critico.     |
| T1         | Golden path — desativar usuario ativo               | `{"ativo": false}`                | 200             | `ativo` retorna `false`.                        |
| T2         | Reativar usuario inativo                            | `{"ativo": true}`                 | 200             | `ativo` retorna `true`.                         |
| T3         | Toggle idempotente — desativar ja inativo           | `{"ativo": false}`                | 200             | Sem erro; sem efeito colateral.                 |
| T4         | Id valido inexistente                               | `{"ativo": true}`                 | 404             | `NotFoundException`.                            |
| T5         | Id nao-Guid (`123abc`)                              | `{"ativo": true}`                 | 400             | Falha no route binding.                         |
| T6         | Body ausente (sem `--data`)                         | (vazio)                           | 400             | Mensagem `Corpo da requisicao ausente...`.      |
| T7         | Body `{}` (campo `ativo` ausente)                   | `{}`                              | 400 (esperado)  | Hoje vira `false` silenciosamente — BUG.        |
| T8         | Body com `ativo: null`                              | `{"ativo": null}`                 | 400             | Falha de desserializacao.                       |
| T9         | Body com tipo errado                                | `{"ativo": "sim"}`                | 400             | Falha de desserializacao.                       |
| T10        | Campo extra ignorado                                | `{"ativo": true, "foo": "bar"}`   | 200             | Tolerante a campos desconhecidos.               |
| T11        | Desativar admin do seed (auto-desativacao)          | `{"ativo": false}`                | 200 (atual)     | Sem RN documentada — abrir question.            |
| T12        | Verificar resposta nao vaza senha/hash              | `{"ativo": false}`                | 200             | Inspecionar payload — sem `senhaHash`.          |
| T13        | Race condition (PATCH paralelos opostos)            | `{"ativo": true}` e `false`       | 200 ambos       | last-write-wins, sem 409 — risco documentado.   |

---

## Detalhamento por caso

### Tbug-Auth — Requisicao anonima altera status (BUG)

```bash
curl -i -X PATCH "http://localhost:8080/api/v1/usuarios/$USER_ID/status" \
  -H "Content-Type: application/json" \
  --data '{"ativo": false}'
```

```json
{ "ativo": false }
```

- **Esperado (correto):** `401 Unauthorized` com `ProblemDetails`.
- **Observado atualmente:** `200 OK` + payload com `ativo: false`. A rota nao chama `RequireAuthorization()`.
- **Logs Serilog esperados (se corrigido):** entrada de autenticacao negada (`Authentication failed`).
- **Logs atuais:** `Status do usuario alterado. UsuarioId=..., Ativo=False` — sem qualquer indicio de identidade do chamador.
- **Sinais de bug:**
  - Resposta `200` sem token.
  - Persistencia no banco confirmada via `GET /api/v1/usuarios/{id}`.
  - Ausencia de claim/usuario logado nos logs.

### T1 — Golden path: desativar usuario ativo

```bash
curl -i -X PATCH "http://localhost:8080/api/v1/usuarios/$USER_ID/status" \
  -H "Content-Type: application/json" \
  --data '{"ativo": false}'
```

```json
{ "ativo": false }
```

- **Esperado:** `200 OK`, corpo com `{ "id": "<USER_ID>", "ativo": false }`.
- **Logs Serilog:** `Status do usuario alterado. UsuarioId=<USER_ID>, Ativo=False`.
- **Verificacao adicional:** `GET /api/v1/usuarios/{id}` deve refletir `ativo: false`.
- **Sinais de bug:** 500 inesperado, ausencia da entrada de log, payload sem o campo `ativo`.

### T2 — Reativar usuario inativo

```bash
curl -i -X PATCH "http://localhost:8080/api/v1/usuarios/$USER_ID/status" \
  -H "Content-Type: application/json" \
  --data '{"ativo": true}'
```

```json
{ "ativo": true }
```

- **Esperado:** `200 OK`, `ativo: true`.
- **Logs Serilog:** `Status do usuario alterado. UsuarioId=<USER_ID>, Ativo=True`.
- **Sinais de bug:** transicao nao registrada no log, status nao persistido, mascara de `ativo` como string.

### T3 — Toggle idempotente (desativar ja inativo)

```bash
curl -i -X PATCH "http://localhost:8080/api/v1/usuarios/$USER_ID/status" \
  -H "Content-Type: application/json" \
  --data '{"ativo": false}'
```

```json
{ "ativo": false }
```

- **Pre-condicao:** executar T1 antes (usuario ja `Ativo=false`).
- **Esperado:** `200 OK`, sem erro. Operacao deve ser idempotente.
- **Logs Serilog:** mesma mensagem de status. Avaliar se o handler escreve no banco mesmo sem mudanca de valor (excesso de UPDATE).
- **Sinais de bug:** 409/422 indevido, exception por "estado ja aplicado", multiplos UPDATEs visiveis no log SQL.

### T4 — Id valido inexistente

```bash
curl -i -X PATCH "http://localhost:8080/api/v1/usuarios/00000000-0000-0000-0000-000000000001/status" \
  -H "Content-Type: application/json" \
  --data '{"ativo": true}'
```

```json
{ "ativo": true }
```

- **Esperado:** `404 Not Found` com `ProblemDetails` apontando `NotFoundException`.
- **Logs Serilog:** entrada de warning/erro tratado mencionando `Usuario nao encontrado` ou equivalente.
- **Sinais de bug:** 500 sem tratamento, payload de sucesso, criacao silenciosa de novo usuario.

### T5 — Id nao-Guid (route binding)

```bash
curl -i -X PATCH "http://localhost:8080/api/v1/usuarios/123abc/status" \
  -H "Content-Type: application/json" \
  --data '{"ativo": true}'
```

```json
{ "ativo": true }
```

- **Esperado:** `400 Bad Request` da camada de route binding (Minimal API).
- **Logs Serilog:** request rejeitado antes do handler.
- **Sinais de bug:** 404 (rota encontrada) em vez de 400, 500 com `FormatException`, vazamento de stacktrace.

### T6 — Body ausente (sem `--data`)

```bash
curl -i -X PATCH "http://localhost:8080/api/v1/usuarios/$USER_ID/status" \
  -H "Content-Type: application/json"
```

```json
(sem corpo)
```

- **Esperado:** `400 Bad Request` com `ProblemDetails` ou `ValidationProblemDetails` contendo `errors.body = ["Corpo da requisicao ausente ou malformado."]`. Handler trata `request == null` lancando `ValidationException`.
- **Logs Serilog:** entrada de validacao falha.
- **Sinais de bug:** 500 nao estruturado, ausencia de `errors.body`, mensagem em ingles ou stacktrace exposto.

### T7 — Body `{}` (campo `ativo` ausente) — BUG potencial

```bash
curl -i -X PATCH "http://localhost:8080/api/v1/usuarios/$USER_ID/status" \
  -H "Content-Type: application/json" \
  --data '{}'
```

```json
{}
```

- **Esperado (correto):** `400 Bad Request` com `errors.ativo = ["Campo obrigatorio"]`.
- **Observado atualmente:** `200 OK` com `ativo: false` (bool default). O validator nao verifica presenca explicita do campo.
- **Logs Serilog:** `Status do usuario alterado. UsuarioId=..., Ativo=False` — sem distinguir omissao de desativacao deliberada.
- **Sinais de bug:**
  - Desativacao silenciosa por omissao.
  - Impossibilidade de auditar a intencao do chamador.
  - Falta de regra em `AlterarStatusUsuarioValidator` que use `RuleFor(x => x.Ativo).NotNull()` em DTO com `bool?`.
- **Acao recomendada:** alterar `AlterarStatusUsuarioRequest.Ativo` para `bool?` + validacao explicita.

### T8 — Body com `ativo: null`

```bash
curl -i -X PATCH "http://localhost:8080/api/v1/usuarios/$USER_ID/status" \
  -H "Content-Type: application/json" \
  --data '{"ativo": null}'
```

```json
{ "ativo": null }
```

- **Esperado:** `400 Bad Request` durante desserializacao (System.Text.Json rejeita `null` para `bool` nao nullable) ou via validator se o DTO virar `bool?`.
- **Logs Serilog:** falha de modelo.
- **Sinais de bug:** 200 com `ativo: false` (default), 500 sem ProblemDetails, ausencia da mensagem na chave esperada (`ativo`).

### T9 — Body com tipo errado (`"sim"`)

```bash
curl -i -X PATCH "http://localhost:8080/api/v1/usuarios/$USER_ID/status" \
  -H "Content-Type: application/json" \
  --data '{"ativo": "sim"}'
```

```json
{ "ativo": "sim" }
```

- **Esperado:** `400 Bad Request` por falha de desserializacao.
- **Logs Serilog:** entrada de erro de binding/JSON.
- **Sinais de bug:** coercao para `true`/`false`, 500 com `JsonException` cru, mensagem em ingles sem estrutura ProblemDetails.

### T10 — Campo extra (`foo`) ignorado

```bash
curl -i -X PATCH "http://localhost:8080/api/v1/usuarios/$USER_ID/status" \
  -H "Content-Type: application/json" \
  --data '{"ativo": true, "foo": "bar"}'
```

```json
{ "ativo": true, "foo": "bar" }
```

- **Esperado:** `200 OK`, campo `foo` descartado silenciosamente (politica default do System.Text.Json).
- **Logs Serilog:** mensagem normal de alteracao.
- **Sinais de bug:** 400 por "campo desconhecido" (politica `JsonSerializerOptions.UnknownTypeHandling` divergente do esperado), persistencia do campo extra em coluna inesperada.

### T11 — Desativar admin do seed (auto-desativacao)

```bash
# Descobrir ADMIN_ID antes
ADMIN_ID="<id do admin do seed>"
curl -i -X PATCH "http://localhost:8080/api/v1/usuarios/$ADMIN_ID/status" \
  -H "Content-Type: application/json" \
  --data '{"ativo": false}'
```

```json
{ "ativo": false }
```

- **Esperado (regra desejavel):** `409 Conflict` ou `422 Unprocessable Entity` bloqueando desativacao do unico administrador / do proprio usuario logado.
- **Observado atualmente:** `200 OK` — sem regra de negocio que impeca.
- **Logs Serilog:** mesma mensagem `Status do usuario alterado`.
- **Acao recomendada:** abrir question para PO/PM: deve existir RN proibindo desativacao do ultimo admin ativo? Deve impedir auto-desativacao?
- **Sinais de bug:** acesso administrativo perdido sem trava — pode deixar o sistema sem nenhum admin.

### T12 — Resposta nao deve vazar senha/hash

```bash
curl -s -X PATCH "http://localhost:8080/api/v1/usuarios/$USER_ID/status" \
  -H "Content-Type: application/json" \
  --data '{"ativo": false}' | jq
```

```json
{ "ativo": false }
```

- **Esperado:** payload contendo apenas campos publicos (`id`, `ativo`). Ausencia de `senha`, `senhaHash`, `passwordHash`, `salt`.
- **Logs Serilog:** nao devem conter hash ou senha.
- **Sinais de bug:** qualquer chave relacionada a credenciais no payload ou em log.

### T13 — Race condition (PATCH paralelos com valores opostos)

```bash
printf '%s\n' 'true' 'false' | xargs -P2 -I{} curl -s -o /dev/null -w "%{http_code}\n" \
  -X PATCH "http://localhost:8080/api/v1/usuarios/$USER_ID/status" \
  -H "Content-Type: application/json" \
  --data '{"ativo": {}}'
```

```json
{ "ativo": true }  // uma requisicao
{ "ativo": false } // outra requisicao
```

- **Esperado (atual):** ambos retornam `200 OK`. Valor final do banco eh o do ultimo `UPDATE` (last-write-wins).
- **Esperado (ideal, com concorrencia otimista):** uma das requisicoes recebe `409 Conflict` quando `RowVersion`/`xmin` diverge.
- **Logs Serilog:** duas entradas `Status do usuario alterado` proximas no tempo, sem aviso de colisao.
- **Sinais de bug/risco:** ausencia de mecanismo de versionamento, possibilidade de operador sobrescrever decisao de outro sem rastreio.

> Observacao: o snippet acima usa `xargs` com substituicao no JSON. Em ambientes onde a substituicao no `--data` for problematica, executar dois `curl` em background com `&` em terminais separados.

---

## Bugs e crashes a observar

- **Endpoint anonimo (Tbug-Auth):** ausencia de `RequireAuthorization()` em `UsuariosEndpoints.cs:43`. Critico — qualquer chamador altera status de qualquer usuario.
- **Body `{}` desativando silenciosamente (T7):** `bool` default `false` em DTO sem `bool?` permite omissao implicita. Mudar DTO para `bool?` + validator explicito.
- **500 em body ausente (T6):** se o `ValidationException("Corpo da requisicao ausente ou malformado.")` nao for capturado pelo middleware global e devolver ProblemDetails estruturado, vira 500 cru.
- **Logs sem `traceId`/`correlationId`:** mensagens `Status do usuario alterado` nao trazem correlacao com a requisicao HTTP — dificulta auditoria.
- **Ausencia de concorrencia otimista (T13):** sem `RowVersion`/`xmin` no agregado `Usuario`, ultima escrita vence sem aviso.
- **Validator nao cobre `Id == Guid.Empty` em todas as rotas:** quando o path entrega `Guid.Empty`, eh esperado 400 via validator; se nao acionar, propaga 500 estranho.
- **Auto-desativacao do admin do seed (T11):** sem RN, sistema pode ficar sem administrador.
- **Resposta vazando informacao sensivel (T12):** monitorar payload contra `senhaHash`/`senha`.

## Como reportar para o dev

Ao abrir issue ou comentar no PR, incluir:

1. **Caso de teste executado** (Tbug-Auth, T1..T13).
2. **Comando `curl` exato** usado, com placeholders resolvidos.
3. **Status code observado** vs. **esperado** (referenciar a tabela resumo).
4. **Payload de resposta integral** (sanitizado se contiver dado sensivel).
5. **Trecho do log Serilog** correspondente (com timestamp e mensagem `Status do usuario alterado. UsuarioId=..., Ativo=...`).
6. **Estado no banco antes/depois** (`SELECT id, ativo, atualizado_em FROM usuarios WHERE id = '<USER_ID>'`).
7. **Severidade sugerida:**
   - `Tbug-Auth` → bloqueador (seguranca).
   - `T7`, `T11` → alto (regra de negocio).
   - `T13` → medio (risco de concorrencia, documentar como debito tecnico).
   - Demais divergencias → conforme impacto.
8. **Sugestao de correcao** quando aplicavel (ex.: trocar `bool` por `bool?`, adicionar `RequireAuthorization("Admin")`, introduzir `RowVersion`).
9. **Referencia ao arquivo/linha:** `backend/src/CarWash.Api/Endpoints/Usuarios/UsuariosEndpoints.cs:43`.
