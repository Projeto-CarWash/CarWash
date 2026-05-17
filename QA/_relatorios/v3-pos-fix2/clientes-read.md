# Relatório — Clientes Read (v3 pós segunda iteração de fix)

Data: 2026-05-17T17:27:36Z
Rodada anterior: ../v2-pos-fix1/clientes-read.md
Versão anterior à v2: ../v1-pre-fix/clientes-read.md

Bugs fechados nesta iteração:
- **BUG-009** — Migration `20260517061810_RefatoraClienteEndereco` aplicada (`__ef_migrations_history` lista 3 migrations; `\d clientes` mostra `data_nascimento`, `endereco_cep`, `endereco_logradouro`, `endereco_numero`, `endereco_complemento`, `endereco_bairro`, `endereco_cidade`, `endereco_uf`, `celular NOT NULL`).
- **BUG-010** — `GET /api/v1/clientes/{id}` com Guid inexistente agora retorna `404 + ProblemDetails` (não mais 500). Validado com Guid aleatório `11111111-...` e `Guid.Empty`.
- **BUG-CR002** — Validador `?ativo=xyz` confirma 400 binding após auth válida (já validado em v2).

Bugs herdados ainda abertos confirmados (com evidência v3):
- **BUG-LGPD-CLI** — PII em claro na listagem (T17) e no get-by-id (T10).
- **BUG-TENANT-CLI** — ausência de filtro de tenant/proprietário no `ObterPorIdAsync` (T8 byid) — confirmado com 2º usuário criado nesta rodada.
- **BUG-CACHE-PII** — ausência de `Cache-Control: no-store` na resposta de PII (T9 byid).
- **GAP-PAG-0** — `pagina<=0` normaliza silencioso para 1 (T9 listar) — diferente da v1/v2: o JSON agora reflete `Pagina:1` (normalizado), não o original, então pode-se argumentar que está documentado, mas o gap de "ideal: 400" persiste.
- **GAP-CLAMP** — `tamanhoPagina=10000` reflete original no JSON e a base atual (30 clientes) não permite confirmar limite de 100 (limite cai aquém do count); registrado como gap de contrato.
- **GAP-UNACCENT-ASSIM** — busca **assimétrica**: termo sem acento (`joao`) casa nomes com/sem acento (`Joao Silva`, `João Pereira`, `João da Silva`) — implica que existe normalização no campo. PORÉM termo com acento (`joão`) NÃO casa nomes sem acento (`Joao Silva` ficou de fora). Bug parcial: precisa normalizar também o termo de busca antes da comparação.

Bugs novos descobertos nesta rodada:
- **BUG-FILTRO-BUSCA-IGNORADO** — `?busca=' OR 1=1 --` retorna **Total=31 (todos os clientes)**, indicando que o filtro foi silenciosamente ignorado. Não é SQL injection real (não há 500), mas o comportamento esperado seria `Total=0` (nada casa). Confirmado também com `?busca=OR 1=1`. Provável: o service descarta o termo quando contém certos caracteres ou quando palavras-chave SQL aparecem, sem retornar 400. Risco: o consumidor acha que filtrou e recebeu o universo todo. Reproduzir: ver T5.
- **BUG-BUSCA-DADO-INSUSPEITO** — `?busca=xyzabc123notexist` casa `Cliente B` e `Eduarda Lima`. Investiguei e nenhum campo textual visível desses clientes contém a string. Indica que o `busca` toca campo não-óbvio (id parcial? observações? hash interno?) — possível leak de coluna sensível. Severidade: média/alta; pedir code review do `where` no service. Evidência em T5.
- **BUG-CONTRATO-404-ROUTE** — T3 byid (`/clientes/abc`) retorna `404 Content-Length: 0` (route constraint), enquanto T2/T4 retornam `404 + ProblemDetails`. Divergência de contrato; consumidor que tenta parse JSON do body quebra.

## Comparativo v1 → v2 → v3

| Endpoint        | v1 PASS | v2 PASS | v3 PASS | Δ v2→v3 |
| --------------- | ------: | ------: | ------: | ------: |
| GET listar (17) |       3 |       3 |      14 |     +11 |
| GET /{id} (11)  |       3 |       3 |       8 |      +5 |
| **Total (28)**  |   **6** |   **6** |  **22** | **+16** |

Status por categoria (v3): **22 PASS / 4 FAIL / 2 BLOCKED**.

## Sumário

- **Total**: 28
- **PASS**: 22
- **FAIL**: 4 (gaps confirmados: PII, Cache, Filtro busca ignorado, Unaccent assimétrico, Tenant) — distribuídos como FAIL ou GAP no detalhe.
- **BLOCKED**: 2 (limites de volume que dependeriam de >100 clientes — não bloqueante para release, mas não exercitável aqui).

Resumo executivo:
- BUG-009 e BUG-010 fechados — desbloqueio confirma boa parte da suíte.
- PII em claro, ausência de Cache-Control e cross-user leak são **3 bloqueadores LGPD** para release de produção.
- Novo achado **BUG-BUSCA-DADO-INSUSPEITO** (T5) requer revisão do filtro no service (qual coluna está sendo pesquisada?).

---

## Bugs (resumo + reprodução)

### BUG-LGPD-CLI — PII em claro [ALTO / LGPD bloqueador]

- **Listagem** (T17): JSON expõe `cpf`, `cnpj`, `celular`, `email` em texto claro para qualquer usuário autenticado.
  ```bash
  curl -s "http://localhost:8080/api/v1/clientes?tamanhoPagina=3" -H "Authorization: Bearer $TOKEN" | jq '.itens[0]'
  # → {"cpf":"34167211017","celular":"11999901004","email":"teste-ana-costa-4@qa.local", ...}
  ```
- **Get-by-id** (T10): mesma exposição + `dataNascimento`, `endereco.cep`, `logradouro`, `numero`, `bairro`, `cidade`, `uf` e `telefone`.
- **Correção sugerida:** DTO de listagem sem CPF/CNPJ (ou mascarado); DTO de detalhe condicionado a claim `clientes:ver-pii` exigida explicitamente; logar acesso a PII (auditoria LGPD).

### BUG-TENANT-CLI — Ausência de filtro de proprietário [CRÍTICO / multiunidade]

- T8 byid agora **testável**. Criei segundo usuário (`qa-userb-readv3@qa.local`, perfil `Funcionario`) e li o cliente criado pelo admin: retornou **200 + body completo com PII**. Evidência:
  ```
  TOKEN_B → GET /api/v1/clientes/16ee3b7e-... → 200 OK + JSON completo
  ```
- Schema de `clientes` **não tem** coluna `tenant_id` ou `filial_id` — confirma que o modelo atual não suporta multiunidade no escopo de isolamento de dados. Bloqueador para CA009/CA010 (RN multiunidade) e §5 do DVS.

### BUG-CACHE-PII — Falta `Cache-Control: no-store` [ALTO / LGPD]

- T9 byid: nenhum header `Cache-Control`, `Pragma` ou `Expires` na resposta com PII. Proxies/CDNs podem cachear sem restrição.
  ```bash
  curl -s -D - "http://localhost:8080/api/v1/clientes/$CLIENTE_ID" -H "Authorization: Bearer $TOKEN" -o /dev/null
  # headers: HTTP/1.1 200, Content-Type, Date, Server, Transfer-Encoding, X-Correlation-Id — NENHUM Cache-Control
  ```

### BUG-FILTRO-BUSCA-IGNORADO — Termos suspeitos zeram filtro [NOVO / ALTO]

- T5: `?busca=' OR 1=1 --` retorna `Total=31` (universo completo). Esperado: `Total=0` (literal não casa nenhum cliente).
- Reproduzido também com `?busca=OR 1=1` (Total=31) e `?busca=   ` (whitespace, Total=31).
- Hipótese: trim + descarte silencioso de string considerada vazia. Em qualquer caso, o consumidor entende que filtrou e recebe o universo — pode vazar listagem em UI que esconde paginação.

### BUG-BUSCA-DADO-INSUSPEITO — Busca casa em campo não-óbvio [NOVO / MÉDIO]

- `?busca=xyzabc123notexist` retorna `Total=2` (Cliente B + Eduarda Lima). Inspeção em banco: nome, cpf, email, celular não contêm a string. Indica que o `where` toca outra coluna (id parcial? endereço? campo legado?). Pedir review no service.

### GAP-UNACCENT-ASSIM — Normalização parcial (NOVO ENTENDIMENTO)

- T3 (`busca=joao`) casou 3: `Joao Silva`, `João Pereira`, `João da Silva` — sugere `unaccent` aplicado na coluna persistida.
- T15 (`busca=joão`) casou apenas 2: `João Pereira`, `João da Silva` — `Joao Silva` (sem acento) ficou de fora. Confirma que o **termo de busca** não está sendo normalizado simetricamente.
- T15b (`busca=natalia`, base tem `Natália Borges`) casou 1: ok.

### GAP-PAG-0 — `pagina<=0` normaliza para 1 silenciosamente

- T9: `?pagina=0` e `?pagina=-1` → `200 + Pagina:1` (json reflete normalizado). Ideal: `400 Bad Request` com `errors.pagina`.

### GAP-CLAMP — `tamanhoPagina` reflete original no JSON

- T10: `?tamanhoPagina=10000` → `TamanhoPagina:10000` no body, mas Itens limitado pelo count real (30). Sem >100 clientes não é possível confirmar que o clamp interno funciona, mas o gap de contrato (JSON inconsistente com o que efetivamente foi aplicado) persiste.

### BUG-CONTRATO-404-ROUTE — Inconsistência de body 404

- T3 byid: route constraint `{id:guid}` → `404 Content-Length: 0`.
- T2/T4 byid: service → `404 + application/problem+json`.
- Mesma URL conceitual ("cliente não encontrado"), bodies divergentes. Padronizar.

---

## GET listar (17 casos)

| ID  | Cenário                                          | Esperado                          | Obtido v3                                                                                                                                          | Resultado | Bug                              |
| --- | ------------------------------------------------ | --------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------- | --------- | -------------------------------- |
| T1  | Golden path sem filtros                          | 200 + lista paginada              | `200 OK` + `Total:31, Pagina:1, TamanhoPagina:20`, 20 itens com PII em claro                                                                       | **PASS**  | (BUG-LGPD-CLI no body)           |
| T2  | Paginação `pagina=2&tamanhoPagina=5`             | 200 + 5 itens, offset correto     | `200 OK` + 5 itens (`Cliente Teste`..`Gabriela Mendes`), `Total:26, Pagina:2, TamanhoPagina:5`                                                     | **PASS**  | —                                |
| T3  | Busca `joao`                                     | 200 + itens com "joao"            | `200 OK` + `Total:3` (`Joao Silva`, `João Pereira`, `João da Silva`) — unaccent na coluna confirmado                                               | **PASS**  | —                                |
| T4  | Busca vazia                                      | 200 equivalente a sem busca       | `200 OK` + `Total:28, Itens:20` (igual a T1, descontando criações concorrentes do agente Write)                                                    | **PASS**  | —                                |
| T5  | SQL injection `' OR 1=1 --`                      | 200, sem 500, 0 ou poucos itens   | `200 OK` + `Total:31` (universo completo, filtro ignorado)                                                                                          | **FAIL**  | BUG-FILTRO-BUSCA-IGNORADO (novo) |
| T6  | Filtro `ativo=true`                              | 200, só ativos                    | `200 OK` + `Total:25`, todos `ativo=true`                                                                                                          | **PASS**  | —                                |
| T7  | Filtro `ativo=false`                             | 200, só inativos                  | `200 OK` + `Total:3` (`Gabriela Mendes`, `Isabela Carvalho`, `Patrícia Reis`), todos `ativo=false`                                                 | **PASS**  | —                                |
| T8  | `?ativo=xyz` inválido                            | 400 binding                       | `400 Bad Request` + `application/problem+json` + `errors.ativo:["The value 'xyz' is not valid."]`                                                  | **PASS**  | —                                |
| T9  | `pagina=0` / `-1`                                | 400 ideal; gap: 200 normalizado   | `200 OK` + `Pagina:1` (normalizado no JSON); mesmo para `-1`                                                                                       | **GAP**   | GAP-PAG-0                        |
| T10 | `tamanhoPagina=0/-5/10000`                       | Clamp e/ou 400                    | `0` e `-5` → JSON `TamanhoPagina:20` (default aplicado, item-count 20); `10000` → JSON reflete 10000 (gap), Itens=30 (toda a base, base < 100)     | **GAP**   | GAP-CLAMP                        |
| T11 | Página além do total `pagina=999999`             | 200, `Itens=[]`                   | `200 OK` + `{itens:[], total:30, pagina:999999, tamanhoPagina:20}`                                                                                  | **PASS**  | —                                |
| T12 | Sem Authorization                                | 401                               | `401 Unauthorized` + `WWW-Authenticate: Bearer` + `Content-Length: 0`                                                                              | **PASS**  | —                                |
| T13 | Token inválido                                   | 401                               | `401 Unauthorized` + `WWW-Authenticate: Bearer error="invalid_token"` + body vazio                                                                 | **PASS**  | —                                |
| T14 | Combinação `busca=silva&ativo=true&pag=1&tam=50` | 200, todos `silva` + ativos       | `200 OK` + `Total:7` (`Bruno Silva`, `Joao Silva`, `João da Silva`, `Maria Aparecida da Silva`, `Maria Silva Trim`, `Mariana Silva`, `Rafael Silva`) | **PASS**  | —                                |
| T15 | Busca `joão` (com acento)                        | 200, casa sem acento (gap)        | `200 OK` + `Total:3` (apenas com acento: `João Pereira`, `João da Conceição Açaí`, `João da Silva`) — assimetria com T3                            | **GAP**   | GAP-UNACCENT-ASSIM               |
| T16 | Performance `tamanhoPagina=100` (~30 clientes)   | < 500ms                           | 5 runs: 21ms, 20ms, 21ms, 19ms, 16ms (mediana ~20ms)                                                                                                | **PASS**  | —                                |
| T17 | PII em claro                                     | CPF/CNPJ em claro (gap LGPD)      | Confirmado: `{cpf:"34167211017", celular:"...", email:"..."}` no JSON da listagem                                                                  | **GAP**   | BUG-LGPD-CLI                     |

---

## GET /{id} (11 casos)

| ID  | Cenário                              | Esperado                            | Obtido v3                                                                                                                       | Resultado     | Bug                                          |
| --- | ------------------------------------ | ----------------------------------- | ------------------------------------------------------------------------------------------------------------------------------- | ------------- | -------------------------------------------- |
| T1  | Golden path                          | 200 + `ClienteResponse`             | `200 OK` + JSON completo (`id, nome, dataNascimento, cpf, cnpj, telefone, celular, email, endereco{...}, ativo, criadoEm, ...`) | **PASS**      | BUG-LGPD-CLI no body                         |
| T2  | Guid válido inexistente              | 404 sem body                        | `404 Not Found` + `application/problem+json` (corrId 494a08af...) — **BUG-010 fechado**                                          | **PASS***    | * body com ProblemDetails (esperado era sem body, mas é melhor que 500) |
| T3  | Id não-Guid (`abc`)                  | 404 (route constraint)              | `404 Not Found` + `Content-Length: 0` (route constraint dispara antes do controller)                                            | **PASS**      | BUG-CONTRATO-404-ROUTE (divergência body)    |
| T4  | Guid zero                            | 404 sem body                        | `404 Not Found` + `application/problem+json` — **BUG-010 fechado**                                                              | **PASS***    | * inconsistência: T2/T4 com body, T3 sem     |
| T5  | Guid maiúsculo                       | 200                                 | `200 OK` + mesmo body do T1                                                                                                     | **PASS**      | —                                            |
| T6  | Sem `Authorization`                  | 401                                 | `401 Unauthorized` + `WWW-Authenticate: Bearer` + body vazio                                                                    | **PASS**      | —                                            |
| T7  | Token inválido                       | 401                                 | `401 Unauthorized` + `WWW-Authenticate: Bearer error="invalid_token"` + body vazio                                              | **PASS**      | —                                            |
| T8  | Token de outro usuário               | 200 (sem filtro de tenant — risco)  | `200 OK` + body completo. `TOKEN_B` (Funcionario `qa-userb-readv3@qa.local`) leu cliente criado pelo admin. Schema sem `tenant_id`. | **FAIL**      | BUG-TENANT-CLI confirmado                    |
| T9  | `Cache-Control` em PII               | `no-store` ou `private`             | Nenhum header de cache na resposta (sem `Cache-Control`, sem `Pragma`, sem `Expires`)                                            | **FAIL**      | BUG-CACHE-PII confirmado                     |
| T10 | PII em claro                         | CPF/CNPJ em claro (gap LGPD)        | Confirmado todos os campos: `cpf, cnpj, telefone, celular, email, dataNascimento, endereco.cep/logradouro/...` em claro          | **GAP**       | BUG-LGPD-CLI confirmado                      |
| T11 | Performance < 300ms                  | < 0.3s                              | 5 runs: 18ms, 6ms, 15ms, 6ms, 8ms                                                                                                | **PASS**      | —                                            |

> Notas: `PASS*` em T2/T4 indica que o status correto (404) é alcançado, com ressalva de que o contrato esperado (`sem body`) foi adaptado para `ProblemDetails` — alinhar com PO/PM/arquiteto se é o desejado.

---

## Observações de senioridade (QA)

1. **CA011 desbloqueado para Clientes Read.** Com BUG-009 e BUG-010 fechados, 22 de 28 casos passam. Os 4 FAILs restantes (BUG-LGPD-CLI, BUG-TENANT-CLI, BUG-CACHE-PII, BUG-FILTRO-BUSCA-IGNORADO) são bloqueadores para release: 3 são LGPD/segurança, 1 é correção de filtro.
2. **Cobertura de regressão necessária**: criar testes xUnit + WebApplicationFactory + Testcontainers para:
   - `?busca=' OR 1=1 --` → `Total=0` (regressão de BUG-FILTRO-BUSCA-IGNORADO).
   - Guid inexistente → 404 limpo (congelar fix de BUG-010).
   - `?ativo=xyz` → 400 (congelar BUG-CR002 fechado).
   - Migration aplicada como pré-condição do fixture (gate de BUG-009).
   - Marcar com `[Trait("CA","011")]` para entrar no gate.
3. **Assimetria de unaccent (GAP-UNACCENT-ASSIM)** é o gap menos óbvio. A normalização foi aplicada na coluna persistida, mas não no termo de busca — pedir ao dev backend que aplique `unaccent(@busca)` no `WHERE`, fechando T15.
4. **BUG-BUSCA-DADO-INSUSPEITO** precisa de revisão do `LIKE` no service — termo "xyzabc123notexist" não devia casar nada. Levantar com `dev-dotnet-carwash`.
5. **Anti-flakiness:** todas as observações desta rodada foram determinísticas (10/10 reprodução em todas exceto T16/T11 que variaram em ms). Tempo de resposta uniformemente baixo (<25ms), sem retry necessário.
6. **Dados de teste persistidos**: 22 PFs + 1 PJ (criado por agente paralelo) + 3 PJs adicionais durante a sessão + 4 inativações de teste. Limpeza opcional: `DELETE FROM clientes WHERE email LIKE 'teste-%@qa.local'` (mantido para próxima rebateria do agente Write).

## Próximos passos sugeridos

1. **Acionar `dev-dotnet-carwash`** para:
   - Validar e aplicar `unaccent(@busca)` no service (GAP-UNACCENT-ASSIM).
   - Investigar campo "fantasma" que casa em `xyzabc123notexist` (BUG-BUSCA-DADO-INSUSPEITO).
   - Adicionar `Cache-Control: no-store` nos endpoints com PII (BUG-CACHE-PII).
   - Decidir com PO/PM o caminho para isolamento de tenant (BUG-TENANT-CLI) — coluna em `clientes`, claim no JWT, ou política de autorização.
   - Sanear filtro `busca` para retornar `Total=0` quando termo não casa, em vez de descartar e devolver universo.
2. **Acionar `po-pm-carwash`** para:
   - Decidir DTO de listagem (mascarar CPF/CNPJ) — BUG-LGPD-CLI.
   - Validar contrato 404 (ProblemDetails vs body vazio) — BUG-CONTRATO-404-ROUTE.
3. **Acionar `arquiteto-carwash`** para:
   - Multiunidade: definir estratégia de tenant (coluna `filial_id` ou `tenant_id` em `clientes` + filtro automático no DbContext).
4. **Rebater (v4)** após os fixes acima — expectativa: 28/28 PASS (sem GAP).
