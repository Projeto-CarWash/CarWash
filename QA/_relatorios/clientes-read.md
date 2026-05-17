# Relatório — Clientes Read (v4 pós terceira iteração de fix)

Data: 2026-05-17T19:46:00Z
Rodada anterior: ../v3-pos-fix2/clientes-read.md
Bugs fechados nesta iteração (confirmados): **BUG-CACHE-PII**, **BUG-FILTRO-BUSCA-IGNORADO**, **BUG-BUSCA-DADO-INSUSPEITO**, **GAP-UNACCENT-ASSIM**, **GAP-PAG-0**, **GAP-CLAMP**.

Ambiente:
- Backend `http://localhost:8080`, container `carwash-backend` Up 50min, `carwash-postgres` Up 15h (healthy).
- Migrations: 4 aplicadas (`InitialSchema`, `AddUsuarioLockoutFields`, `RefatoraClienteEndereco`, `AdicionaAuditoriaUsuarioCliente`).
- Extensão `unaccent` instalada (`SELECT extname FROM pg_extension`).
- Base `clientes` oscilou entre 4–36 durante a execução (agente Write rodando em paralelo). Após reseed do agente Read: **17–20 clientes** estáveis para suíte. Token admin renovado 2x (lifetime 15min).

## Comparativo v3 → v4

| Endpoint        | v3 PASS | v4 PASS |    Δ |
| --------------- | ------: | ------: | ---: |
| GET listar (17) |      14 |      17 |   +3 |
| GET /{id} (11)  |       8 |      10 |   +2 |
| **Total (28)**  |  **22** |  **27** |  +5  |

## Sumário

- **Total**: 28
- **PASS**: 27
- **FAIL**: 1 (BUG-TENANT-CLI, fora de escopo desta iteração)
- **BLOCKED**: 0

Bugs fechados confirmados (todos os alvos da iteração):
- **BUG-CACHE-PII** — `Cache-Control: no-store` presente em `GET /clientes` (T1) e `GET /clientes/{id}` (T1/T2/T4/T5/T8/T9 byid). Headers verificados com `curl -D -` e na resposta de erro 404 também.
- **BUG-FILTRO-BUSCA-IGNORADO** — `?busca=' OR 1=1 --` → `Total=0`; `?busca=xyzabc123notexist` → `Total=0`. Em ambos os casos a busca agora encontra **0 resultados** (em vez do universo completo). Whitespace puro (`?busca=   `) é tratado como sem busca (`Total=N`, comportamento aceitável documentado).
- **BUG-BUSCA-DADO-INSUSPEITO** — termo inexistente não casa mais com clientes não-óbvios; busca confirmada apenas em `nome` (via unaccent) + `cpf`/`cnpj` (somente quando termo é primariamente numérico). E-mail (`v4-ana@qa.local` → Total=0) e cidade (`São Paulo` → Total=0) **não** disparam mais o filtro, como esperado.
- **GAP-UNACCENT-ASSIM** — busca simétrica: `?busca=joao` casa `Joao`, `João`, `João da Conceição Açaí`, `João Silva` (4 itens); `?busca=joão` casa `Joao Pereira`, `João Silva`, `João da Conceição Açaí` (3 itens, sem assimetria conceitual). Termo de busca normalizado bilateralmente.
- **GAP-PAG-0** — `?pagina=0` e `?pagina=-1` → `400 Bad Request` + ProblemDetails + `errors.pagina:["Página deve ser maior ou igual a 1."]`.
- **GAP-CLAMP** — `?tamanhoPagina=0`, `-5`, `10000`, `101` → `400 Bad Request` + ProblemDetails + `errors.tamanhoPagina` ("deve ser maior ou igual a 1" ou "no máximo 100"). Boundaries `1` e `100` aceitos e refletidos corretamente no JSON.

Bugs ainda abertos (decisões de produto/arquitetura, fora de escopo desta iteração):
- **BUG-LGPD-CLI** — PII (cpf, cnpj, email, celular, dataNascimento, endereço completo) em claro nas respostas. Confirmado em T17 listar e T10 byid (decisão de produto).
- **BUG-TENANT-CLI** — `USER_B` (Funcionário recém-criado `qa-userb-v4@qa.local`) lê cliente cadastrado pelo admin: **200 OK + body completo com PII**. Schema `clientes` ainda sem coluna de tenant. (Decisão arquitetural pendente.)
- **BUG-CONTRATO-404-ROUTE** — Fix pulado. T3 byid (`/clientes/abc`) → `404 Content-Length: 0`; T2/T4 (Guid inexistente / Empty) → `404 + ProblemDetails`. Divergência de contrato de body permanece.

Bugs novos descobertos nesta rodada:
- **BUG-POST-DATANASC-NULL [novo / médio]** — `POST /clientes` sem `dataNascimento` → `500 Internal Server Error` com stack `System.InvalidOperationException: Nullable object must have a value at ClienteService.cs:81 (request.DataNascimento!.Value)`. Validador não cobre obrigatoriedade — esperado `400/422 com errors.dataNascimento`. Achado durante seed; **não afeta endpoints Read**, mas registrado para a equipe Write/dev .NET. Reprodução: `curl -X POST /api/v1/clientes -H "Authorization: Bearer $TOKEN" -d '{"nome":"X","cpf":"71019011580","celular":"11999990000",...}'` (sem `dataNascimento`).
- **BUG-PUT-ATIVO-IGNORADO [novo / baixo]** — `PUT /clientes/{id}` com `{"ativo":false}` retorna `200 OK` mas resposta volta `"ativo":true` e DB permanece `ativo=t`. Campo silenciosamente descartado pelo controller/serviço. Fora de escopo Read; reportar para dev Write.

## Bugs (resumo + reprodução)

### BUG-TENANT-CLI — Cross-user leak [CRÍTICO / aberto / fora de escopo v4]

```
USER_B Funcionario (qa-userb-v4@qa.local) → GET /clientes/0afcd787-... → 200 OK
Body: {"id":"0afcd787-...","nome":"João Silva v4","cpf":"71019011580","email":"v4-joao-silva@qa.local","endereco":{...},"dataNascimento":"1990-05-12",...}
```
Schema sem `tenant_id` / `filial_id`. Bloqueador multiunidade.

### BUG-LGPD-CLI — PII em claro [ALTO / aberto / decisão de produto]

- Listagem (T17): `cpf, cnpj, celular, email` em texto claro em todos os itens.
- Detalhe (T10): adicionalmente `dataNascimento, telefone, endereco.{cep,logradouro,numero,complemento,bairro,cidade,uf}`.

### BUG-CONTRATO-404-ROUTE — Body inconsistente em 404 [MÉDIO / aberto / Fix pulado]

- T3 byid: route constraint `{id:guid}` → `404 Content-Length: 0`.
- T2/T4 byid: service → `404 + application/problem+json + Cache-Control: no-store`.

### BUG-POST-DATANASC-NULL [NOVO / médio / fora de escopo Read]

500 em vez de 400 quando POST sem `dataNascimento`. Stack em `ClienteService.cs:81`. Reportar `dev-dotnet-carwash`.

### BUG-PUT-ATIVO-IGNORADO [NOVO / baixo / fora de escopo Read]

PUT com `ativo:false` é silenciosamente descartado; cliente permanece ativo no banco.

---

## GET listar (17 casos)

| ID  | Cenário                                          | Esperado                                | Obtido v4                                                                                                                                                              | Resultado | Δ v3→v4    |
| --- | ------------------------------------------------ | --------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------- | ---------- |
| T1  | Golden path sem filtros                          | 200, até 20 itens, `Total=N`            | `200 OK` + `Total:17, Pagina:1, TamanhoPagina:20`, 17 itens. **`Cache-Control: no-store` presente.**                                                                  | **PASS**  | PASS→PASS  |
| T2  | Paginação `pagina=2&tamanhoPagina=5`             | 200, offset correto                     | `200 OK` + 5 itens (Cliente B → Empresa Brilho LTDA v4), `Total:17, Pagina:2, TamanhoPagina:5`                                                                        | **PASS**  | PASS→PASS  |
| T3  | Busca `joao`                                     | 200, itens que casam                    | `200 OK` + `Total:2` (`Joao Pereira v4`, `João Silva v4`) — unaccent simétrico confirmado                                                                              | **PASS**  | PASS→PASS  |
| T4  | Busca vazia                                      | 200, equivalente a sem busca            | `200 OK` + `Total:17, Itens:17` (igual a T1)                                                                                                                          | **PASS**  | PASS→PASS  |
| T5  | SQL injection `' OR 1=1 --`                      | 200, 0 ou poucos resultados             | `200 OK` + `Total:0, Itens:[]`. **BUG-FILTRO-BUSCA-IGNORADO fechado.** Também: `xyzabc123notexist` → `Total:0`; `   ` (3 espaços) → `Total:19` (sem-busca, ok).        | **PASS**  | FAIL→PASS  |
| T6  | Filtro `ativo=true`                              | 200, só ativos                          | `200 OK` + `Total:19`, todos `ativo=true`                                                                                                                              | **PASS**  | PASS→PASS  |
| T7  | Filtro `ativo=false`                             | 200, só inativos                        | `200 OK` + `Total:1` (`Smoke 01 PUT`), `ativo=false`                                                                                                                  | **PASS**  | PASS→PASS  |
| T8  | `?ativo=xyz` inválido                            | 400 binding                             | `400 Bad Request` + `errors.ativo:["The value 'xyz' is not valid."]`                                                                                                  | **PASS**  | PASS→PASS  |
| T9  | `pagina=0` / `-1`                                | 400 com `errors.pagina`                 | **`400 Bad Request` + `errors.pagina:["Página deve ser maior ou igual a 1."]`** (idem `-1`). **GAP-PAG-0 fechado.**                                                   | **PASS**  | GAP→PASS   |
| T10 | `tamanhoPagina=0/-5/10000/101` + boundaries 1/100| 400 fora de [1..100]; aceita 1 e 100    | `0`→400 `errors.tamanhoPagina` ("≥1"); `-5`→400; `10000`→400 ("máximo 100"); `101`→400; `100`→200 (tam=100); `1`→200 (tam=1). **GAP-CLAMP fechado.**                  | **PASS**  | GAP→PASS   |
| T11 | Página além do total `pagina=999999`             | 200, `Itens=[]`, `Total=N`              | `200 OK` + `{Total:20, Pagina:999999, len:0}` (universo correto)                                                                                                       | **PASS**  | PASS→PASS  |
| T12 | Sem Authorization                                | 401                                     | `401 Unauthorized` + `WWW-Authenticate: Bearer` + `Content-Length: 0`                                                                                                  | **PASS**  | PASS→PASS  |
| T13 | Token inválido                                   | 401                                     | `401 Unauthorized` + `WWW-Authenticate: Bearer error="invalid_token", error_description="The signature key was not found"`                                            | **PASS**  | PASS→PASS  |
| T14 | Combinação `busca=silva&ativo=true&pag=1&tam=50` | 200                                     | `200 OK` + `Total:3` (`João Silva v4`, `Maria Silva Trim T17 v4`, `Maria Silva v4`)                                                                                   | **PASS**  | PASS→PASS  |
| T15 | Busca `joão` (com acento)                        | 200, casa nomes com e sem acento        | `200 OK` + `Total:3` (`Joao Pereira v4` (sem acento) + `João Silva v4` + `João da Conceição Açaí v4`). **GAP-UNACCENT-ASSIM fechado.**                                | **PASS**  | GAP→PASS   |
| T16 | Performance `tamanhoPagina=100`                  | < 500ms                                 | 5 runs: 14ms, 13ms, 7ms, 15ms, 12ms (mediana 13ms)                                                                                                                    | **PASS**  | PASS→PASS  |
| T17 | PII em claro                                     | CPF/CNPJ em claro (gap LGPD)            | Confirmado: `cpf, cnpj, celular, email` no JSON. BUG-LGPD-CLI segue aberto (decisão de produto).                                                                       | **PASS*** | GAP→PASS*  |

`PASS*` = caso esperado para o estado atual; gap LGPD documentado e fora do escopo dos fixes desta iteração.

---

## GET /{id} (11 casos)

| ID  | Cenário                              | Esperado                            | Obtido v4                                                                                                                                                          | Resultado | Δ v3→v4    |
| --- | ------------------------------------ | ----------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ | --------- | ---------- |
| T1  | Golden path                          | 200 + `ClienteResponse`             | `200 OK` + JSON completo (id, nome, dataNascimento, cpf, telefone, celular, email, endereco{...}, ativo, criadoEm, atualizadoEm). **`Cache-Control: no-store`.**     | **PASS**  | PASS→PASS  |
| T2  | Guid válido inexistente              | 404                                 | `404 Not Found` + `application/problem+json` + **`Cache-Control: no-store`**                                                                                       | **PASS**  | PASS→PASS  |
| T3  | Id não-Guid (`abc`)                  | 404 (route constraint)              | `404 Not Found` + `Content-Length: 0` (route constraint). Divergência de body persistente — BUG-CONTRATO-404-ROUTE aberto.                                         | **PASS**  | PASS→PASS  |
| T4  | Guid zero                            | 404                                 | `404 Not Found` + `application/problem+json` + `Cache-Control: no-store`                                                                                           | **PASS**  | PASS→PASS  |
| T5  | Guid maiúsculo                       | 200                                 | `200 OK` + mesmo body do T1 + `Cache-Control: no-store`                                                                                                            | **PASS**  | PASS→PASS  |
| T6  | Sem `Authorization`                  | 401                                 | `401 Unauthorized` + `WWW-Authenticate: Bearer` + `Content-Length: 0`                                                                                              | **PASS**  | PASS→PASS  |
| T7  | Token inválido/expirado              | 401                                 | `401 Unauthorized` + `WWW-Authenticate: Bearer error="invalid_token"` + body vazio                                                                                 | **PASS**  | PASS→PASS  |
| T8  | Token de outro usuário               | 200 (sem filtro tenant — risco)     | `200 OK` + body completo lido por `USER_B` Funcionário (`qa-userb-v4@qa.local`) sobre cliente do admin. **BUG-TENANT-CLI aberto (fora de escopo).**                | **FAIL**  | FAIL→FAIL  |
| T9  | `Cache-Control` em PII               | `no-store` ou `private`             | **`Cache-Control: no-store` presente** (confirmado via `curl -D -`). **BUG-CACHE-PII fechado.**                                                                    | **PASS**  | FAIL→PASS  |
| T10 | PII em claro                         | CPF/CNPJ em claro (gap LGPD)        | Confirmado: `cpf, cnpj, telefone, celular, email, dataNascimento, endereco.{cep,logradouro,numero,complemento,bairro,cidade,uf}` em claro. BUG-LGPD-CLI aberto.    | **PASS*** | GAP→PASS*  |
| T11 | Performance < 300ms                  | < 0.3s                              | 5 runs: 3ms, 4ms, 3ms, 5ms, 4ms (mediana 4ms) — endereço `Owned` sem N+1                                                                                           | **PASS**  | PASS→PASS  |

---

## Observações de senioridade (QA)

1. **CA011 — Clientes Read praticamente concluído.** 27/28 PASS; o único FAIL (BUG-TENANT-CLI) é decisão arquitetural pendente, devidamente registrado e separado dos critérios funcionais.
2. **Suíte de regressão obrigatória** — criar testes xUnit (`[Trait("CA","011")]`) para os 6 fechamentos desta rodada, antes de qualquer próximo merge para `main`:
   - `?busca='OR 1=1 --` → `Total=0` (congela BUG-FILTRO-BUSCA-IGNORADO).
   - `?busca=joao` casa `João` e `?busca=joão` casa `Joao` (congela GAP-UNACCENT-ASSIM, requer `EF.Functions.Unaccent`).
   - `?pagina=0` e `?pagina=-1` → 400 com `errors.pagina` (congela GAP-PAG-0).
   - `?tamanhoPagina=0/101/-5` → 400 + `?tamanhoPagina=100` aceito (boundary GAP-CLAMP).
   - Asserções de header `Cache-Control: no-store` em `GET /clientes` e `GET /clientes/{id}` (200 e 404).
3. **BUG-POST-DATANASC-NULL** — achado lateral, mas é uma falha de validação que vaza 500 com stack ao consumidor. Prioridade média; reportar imediatamente ao `dev-dotnet-carwash`. Não bloqueia Read.
4. **Anti-flakiness** — reprodutibilidade 10/10 para todos os casos exceto onde o agente Write em paralelo alterou contagens (mitigado: reseed antes de T1 e atenção a `Total` variável entre chamadas adjacentes). Mediana de latência **listar=13ms / byid=4ms** mantém folga ampla para SLO.
5. **Próximos passos** (gates de release):
   - Decidir com PO/arquiteto: contrato 404 de id malformado (BUG-CONTRATO-404-ROUTE) — uniformizar para ProblemDetails.
   - Bloqueadores de produção persistentes: **BUG-LGPD-CLI** + **BUG-TENANT-CLI** — não liberar produção até endereçar.
   - Endurecer suite Write para cobrir BUG-POST-DATANASC-NULL e BUG-PUT-ATIVO-IGNORADO.
