# ADR 0004 — Confirmação de agendamento em duas etapas (pré-confirmação + confirmação idempotente)

- **Status:** Aceita
- **Data:** 2026-05-22
- **Autores:** Arquiteto técnico CarWash — desenho do card 133 (RF015).
- **Escopo:** Backend `.NET 8` (`CarWash.Api`, `CarWash.Application`, `CarWash.Infrastructure`, `CarWash.Domain`) e frontend React (`frontend/src/pages/Agendamentos`).
- **Requisitos:** RF015, RF007, RF020, RN011, RAT01, RAT03, DAT §4.2, DAT §9.1.

---

## Contexto

O RF007 (card 131) já entrega a criação de agendamento em um único `POST /api/v1/agendamentos`,
que submete e persiste de imediato. O RF015 exige uma **etapa explícita de revisão** das
informações antes de concluir o agendamento, com:

1. um endpoint que gera um **resumo** das informações sem persistir nada;
2. um endpoint de **confirmação** que persiste em transação única;
3. **idempotência** de 24h na confirmação (duplo clique, retry de rede → 1 só agendamento);
4. **revalidação** de conflito de agenda (RN011) no momento da confirmação;
5. proteção contra confirmar dados que mudaram entre a prévia e a confirmação.

Três decisões de fundo precisavam ser fechadas: como transportar o estado entre as duas
chamadas (token), como detectar divergência de dados (hash) e como armazenar idempotência.

---

## Decisão

### 1. Fluxo de dois endpoints sob o grupo existente

- `POST /api/v1/agendamentos/pre-confirmacao` → `200 OK`, calcula resumo, **não persiste**.
- `POST /api/v1/agendamentos/confirmar` → `201 Created`, persiste em transação única.
- O `POST /api/v1/agendamentos` legado do RF007 é **mantido e marcado `[Obsolete]`/`Deprecated`**
  no MVP (sem remoção) — é o caminho de criação direta usado por testes e integrações; o
  frontend deixa de usá-lo. Remoção fica para um card pós-MVP.

### 2. `tokenConfirmacao` stateless assinado (HMAC-SHA256), sem tabela

O token é um **JWT-like compacto assinado com HMAC-SHA256**, gerado na pré-confirmação,
contendo o hash do resumo, o `usuarioId`, o `traceId` e `exp` (15 min). Não há tabela de
sessão de confirmação. Justificativa: o token é curto, de vida curta, e o estado que ele
carrega (o resumo) é reconstruível na confirmação a partir do payload. Tabela seria custo
de manutenção e limpeza sem ganho — YAGNI. O segredo reusa a infraestrutura de
`JwtOptions` já existente, em chave dedicada.

A distinção **400 (inválido)** vs **410 (expirado)** sai da validação: assinatura quebrada,
formato inválido ou `usuarioId` divergente → 400; assinatura válida porém `exp` no passado
→ 410.

### 3. `hashResumo` = SHA-256 sobre JSON canônico dos campos de negócio

O hash cobre exatamente os campos que, se alterados, mudam o agendamento:
`filialId, clienteId, veiculoId, responsavelId, servicoIds (ordenados), inicio,
duracaoTotalMin, valorTotal, observacoes normalizada`. Serialização canônica
determinística (ordem fixa de chaves, sem espaços). É comparado na confirmação contra o
hash embutido no token; divergência → 409 com mensagem de revisão.

### 4. Idempotência em tabela dedicada `idempotencia_requisicoes`

`idempotencyKey` + `payload_hash` numa tabela própria, com `UNIQUE` na key. Replay com
mesmo payload devolve a resposta gravada (`201`); mesmo key + payload diferente → `409`.
A anti-race usa o mesmo padrão do RF007: `UNIQUE` no banco + tradução da violação
`23505` em exceção de aplicação. Registros expiram em 24h e são limpos por um
job de varredura simples.

### 5. Status inalterado

O agendamento confirmado nasce com status `agendado` — o mesmo valor de banco e enum
`StatusAgendamento.Agendado` já existentes. Nenhum status novo.

---

## Consequências

**Positivas**

- Reaproveita 100% das validações de existência/estado e a transação `AdicionarAsync` do RF007.
- Token stateless = zero estado de sessão para gerir/limpar.
- Idempotência robusta a duplo clique e retry de rede (RAT03 — defesa server-side).
- RN011 revalidada na confirmação (pré-check + constraint EXCLUDE) — fecha a race window.

**Negativas / trade-offs**

- O cliente precisa reenviar o payload completo na confirmação (o token não o carrega).
  Aceitável: o payload é pequeno e isso mantém o token curto.
- Uma tabela e uma migração novas; um job de limpeza. Custo baixo e isolado.
- Dois endpoints a manter; mitigado pela extração da lógica comum num serviço de domínio
  de cálculo de resumo, consumido pelos dois handlers.

---

## Alternativas consideradas

- **Token persistido em tabela `sessao_confirmacao`** — rejeitado: estado e limpeza extras
  sem ganho; o token stateless já garante TTL e integridade.
- **Idempotência via cabeçalho HTTP genérico com cache distribuído (Redis)** — rejeitado:
  introduz dependência de infraestrutura nova, proibido sem justificativa de RNF/RAT.
- **Pré-confirmação que já persiste em status `rascunho`** — rejeitado: cria status novo,
  fere "não persistir na prévia" e exige limpeza de rascunhos abandonados.
