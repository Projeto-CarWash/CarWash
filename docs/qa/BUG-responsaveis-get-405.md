# BUG — `GET /api/v1/clientes/{id}/responsaveis` retorna 405 (dropdown de responsável vazio)

**Severidade:** Alta (impede o fluxo de seleção de responsável no agendamento — RF024 — pela UI).
**Cards afetados:** RF024 (Seleção do responsável no momento do agendamento), RF023 (Cadastro de responsáveis vinculados ao cliente titular).
**Componentes:** backend `ResponsaveisEndpoints`, frontend `agendamentoService.buscarResponsaveisPorCliente`.

## Descrição

O frontend (PR #177, RF024) popula o dropdown de responsável do wizard de agendamento chamando:

```
GET /api/v1/clientes/{clienteId}/responsaveis
```

(`frontend/src/services/agendamentoService.ts` → `buscarResponsaveisPorCliente`).

O backend, porém, só registra **POST** nessa rota (`ResponsaveisEndpoints.cs` → `grupo.MapPost("/", CriarAsync)`); **não há `GET`**. A chamada retorna **405 Method Not Allowed** (`Allow: POST`). Como o service trata erro retornando `[]`, o dropdown fica **sempre vazio** e o usuário não consegue selecionar um responsável já cadastrado ao agendar.

Os dados existem e são expostos por outra rota (`GET /api/v1/clientes/{id}` inclui `responsaveis[]`), o que confirma que falta apenas o endpoint dedicado.

## Passos de reprodução (API)

```bash
TOKEN=$(curl -s -X POST http://localhost:8080/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@carwash.local","senha":"<seed>"}' | jq -r .accessToken)

# 1) cria cliente + responsável (ambos OK)
CID=...   # POST /api/v1/clientes (201)
curl -s -o /dev/null -w "%{http_code}\n" -X POST \
  "http://localhost:8080/api/v1/clientes/$CID/responsaveis" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"nome":"Resp","documento":"<cpf>","grauVinculo":"RESPONSAVEL_FINANCEIRO"}'   # => 201

# 2) lista responsáveis pela rota que o frontend consome
curl -s -D - "http://localhost:8080/api/v1/clientes/$CID/responsaveis" \
  -H "Authorization: Bearer $TOKEN"
# => HTTP/1.1 405 Method Not Allowed / Allow: POST   (esperado: 200 com a lista)
```

## Evidência

```
HTTP/1.1 405 Method Not Allowed
Content-Length: 0
Allow: POST
```

Enquanto `GET /api/v1/clientes/{id}` retorna `responsaveis: [{ responsavelId, nome, documento, grauVinculo, ativo, ... }]` corretamente.

## Comportamento esperado

`GET /api/v1/clientes/{clienteTitularId}/responsaveis` deve retornar **200** com a lista de responsáveis ativos do cliente, no formato que o frontend espera (`{ id, nome, documento }[]`).

## Correção proposta (MOMENTO 4 — backend primeiro)

Adicionar `grupo.MapGet("/", ListarAsync)` em `ResponsaveisEndpoints`, com query/handler que retorna os responsáveis do `clienteTitularId` (reutilizando o repositório de responsáveis), + testes de integração (200 com itens, 200 lista vazia). Nenhuma alteração de frontend necessária (o contrato já é o consumido).
