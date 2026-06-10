# ADR 0005 — Consulta de agenda (RF009): endpoint de leitura `GET /api/v1/agenda` com projeção dupla

- **Status:** Aceita
- **Data:** 2026-05-22
- **Autores:** Arquiteto Técnico do CarWash — desenho do card 132 (RF009).
- **Escopo:** Backend `.NET 8` — slice de query `CarWash.Application/Agenda/Consultar`, endpoint `CarWash.Api/Endpoints/Agenda/AgendaEndpoints.cs`, repositório de leitura em `CarWash.Infrastructure`.

---

## Contexto

O RF009 do DRP pede a visualização da agenda em dois formatos (simples e detalhado). O card 132
especifica um endpoint de **consulta somente-leitura** com filtros por período/filial/cliente/responsável/status,
ordenação fixa e envelope de resposta padronizado `{message, data, traceId}`.

O analista levantou seis lacunas (L1–L6) que esta ADR resolve de forma fechada para a implementação.

Pontos do código atual que condicionam a decisão:

- `StatusAgendamento` tem apenas `Agendado`, `Cancelado`, `Finalizado` (db: `agendado/cancelado/finalizado`).
  Não existe `EM_ANDAMENTO`. O card cita `AGENDADO|EM_ANDAMENTO|CONCLUIDO|CANCELADO`.
- O recurso `GET /api/v1/agenda` é um caminho novo, distinto do grupo `/api/v1/agendamentos` (criação).
- `Agendamento` carrega `DuracaoTotalMin` e `ValorTotal` denormalizados — consulta de agenda sem N+1
  para esses dois campos já está garantida.
- O índice `idx_ag_filial_inicio` (`FilialId, Inicio`) já cobre o filtro principal (janela + filial + ordenação).

---

## Decisão

### Recurso e padrão

Novo endpoint **`GET /api/v1/agenda`**, em grupo/arquivo próprio (`AgendaEndpoints.cs`), separado de
`AgendamentosEndpoints.cs`. Segue o ADR 0003: slice de **query** (`IQuery`/`IQueryHandler`), sem MediatR.
O recurso `agenda` é uma **projeção de leitura** sobre o agregado `Agendamento` — não é o agregado em si,
por isso ganha caminho próprio.

### L1 — Mismatch de status

A API expõe **4 status no contrato de entrada/saída** mas mapeia para os **3 status reais** do domínio:

| Status da API (uppercase) | Status do domínio (db) | Tratamento |
|---------------------------|------------------------|------------|
| `AGENDADO`                | `agendado`             | filtra/serializa direto |
| `CONCLUIDO`               | `finalizado`           | filtra/serializa direto (alias) |
| `CANCELADO`               | `cancelado`            | filtra/serializa direto |
| `EM_ANDAMENTO`            | — (não existe)         | **aceito como filtro válido**; retorna `data: []` |

Decisão técnica: **não criar status novo**. O domínio permanece com 3 estados. `EM_ANDAMENTO` é um valor
de contrato reconhecido (não causa 400) que, por não ter correspondente persistido, sempre resolve para
conjunto vazio. Isso mantém o contrato do card estável e não quebra quando/se o negócio decidir criar o
4º estado no pós-MVP — a API já o aceita. Valores fora desses 4 → 400.

### L2 — Campo `titulo` (formato simples)

`titulo` é **derivado**, não persistido: nome do **primeiro serviço** do agendamento, escolhido de forma
determinística pela ordem `AgendamentoItem.CriadoEm ASC, AgendamentoItem.Id ASC`. Sem serviços (não deve
ocorrer — RF007 exige ≥1), `titulo` recebe `"Agendamento"`.

### L3 — Regra de `servicosResumo`

String curta derivada dos serviços, mesma ordenação do L2:
- 1 serviço: `"<nomeDoServico>"` — ex.: `"Lavagem"`.
- N serviços (N>1): `"<nomeDoPrimeiroServico> + <N-1>"` — ex.: `"Lavagem + 2"`.
- 0 serviços: `"Sem serviços"`.

### L4 — Exposição de PII no detalhado

`cpfCnpj` e `telefone/celular` são expostos **íntegros** (sem máscara) no formato detalhado — a agenda é
ferramenta operacional interna e o operador precisa do dado completo para contato. Coerente com
`GET /api/v1/clientes/{id}` que já expõe PII completa.

Mitigação obrigatória: o endpoint **sempre** envia `Cache-Control: no-store` (formato simples também
carrega `clienteNome` e `veiculoPlaca`, que são PII). Logs **nunca** registram CPF/CNPJ/telefone — só
contagem de itens, filtros e tempo.

### L5 — Filtro `usuarioId`

`usuarioId` filtra por **`Agendamento.ResponsavelId`** (o responsável pela execução, RF024/CA009),
**não** por `CriadoPor`. A semântica de negócio de "agenda de um responsável" é a do executor.

### L6 — 404 de filial

**Sem 404 por filial.** `filialId` é obrigatório e validado apenas como UUID sintático. Filial inexistente
ou de outro tenant cai em **200 com `data: []`** e a mensagem de lista vazia. Justificativa: a consulta é
um filtro sobre uma coleção — "nenhum evento para essa filial" é resultado válido, não erro. Evita um
roundtrip extra ao banco só para validar existência e evita vazar a existência/inexistência de filiais.

### Status na serialização

A resposta serializa `status` **sempre em uppercase do contrato** (`AGENDADO`, `CONCLUIDO`, `CANCELADO`),
nunca o valor lowercase do banco. A conversão db→API é centralizada em um mapeador estático no slice.

---

## Consequências

### Positivas
- Contrato do card 132 atendido integralmente sem alteração de domínio nem migração de dados.
- `EM_ANDAMENTO` aceito desde já — quando o pós-MVP criar o estado, nenhuma quebra de contrato.
- Reuso do índice `idx_ag_filial_inicio` existente — zero migração de índice se a projeção for desenhada
  com a ordenação correta.

### Negativas
- `EM_ANDAMENTO` retornando sempre vazio é um comportamento "silencioso" — documentado no OpenAPI e nos
  testes para não virar bug-report. Mitigação: comentário no validator e no mapeador.
- A projeção detalhada faz 4 joins — aceitável para janela máxima de 31 dias; reavaliar se o volume crescer.

---

## Re-avaliação
- Quando o negócio decidir criar `EM_ANDAMENTO` como estado real: adicionar ao enum, migração de
  `ck_ag_status`, e o mapeador passa a ter correspondência 1:1 — o contrato da API não muda.
- Se a janela de 31 dias se mostrar pequena para algum relatório: tratar como RF separado, não alargar aqui.

---

## Referências
- ADR 0003 — [`./0003-minimal-api-cqrs-vertical-slices.md`](./0003-minimal-api-cqrs-vertical-slices.md).
- DRP — RF009 (visualização de agenda), RF024/CA009 (responsável).
- Card 132 — RF009.
