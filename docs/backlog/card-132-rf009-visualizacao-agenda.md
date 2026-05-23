# Backlog — Card 132 / RF009 — Visualização de agenda (simples e detalhado)

> Status: refinamento do analista. Pendente de decisão de CEO/Arquiteto nas lacunas L1–L7.
> Rastreabilidade base: P1 (agendamento desorganizado) → RF009 (Must) → UC005 → Módulo Agenda (DAT §4.1) / Serviço de Agenda (DAT §4 — Backend/API).

## Resumo executivo

RF009 entrega uma consulta de leitura (sem persistência) da agenda em dois formatos
— simples (resumo para grade/lista) e detalhado (atendimento completo) — sob o
endpoint `GET /api/v1/agenda`, com filtros obrigatórios de período e filial,
ordenação determinística, timezone UTC e retorno vazio padronizado. Segue o padrão
de QUERY/QueryHandler já estabelecido em `Clientes/Listar`. Há 7 lacunas que
precisam de decisão antes do início (a mais crítica: o filtro de status do card
pede 4 valores e o domínio só tem 3).

## User stories

### US-132.1 — Consulta de agenda no formato simples
**Como** Funcionário, **quero** consultar a agenda de uma filial em um período no
formato simples **para que** eu visualize rapidamente os atendimentos do dia em
uma grade/lista. (Card: contratos de API, projeção simples; CA001/RF009; UC005.)

### US-132.2 — Consulta de agenda no formato detalhado
**Como** Funcionário, **quero** abrir o detalhe completo de um atendimento na
agenda **para que** eu veja cliente, veículo, serviços, valores e observações.
(Card: projeção detalhado; RF009/RF011; UC005.)

### US-132.3 — Filtragem da agenda
**Como** Administrador, **quero** filtrar a agenda por cliente, responsável e
status **para que** eu localize atendimentos específicos sem percorrer a lista
inteira. (Card: filtros opcionais; RF009; relacionado a RF-FUT004 — filtros
avançados ficam de fora.)

### US-132.4 — Período sem eventos e validação de parâmetros
**Como** Funcionário, **quero** receber uma resposta clara quando não houver
atendimentos ou quando os parâmetros forem inválidos **para que** eu saiba
distinguir "agenda vazia" de "consulta malformada". (Card: retorno vazio, regras
de período, códigos HTTP; RNF005/RNF009.)

## Lacunas para decisão (CEO / Arquiteto)

- **L1 (bloqueante)** — Status: o card pede filtro `AGENDADO|EM_ANDAMENTO|CONCLUIDO|CANCELADO`;
  o domínio (`StatusAgendamento`) e o DRP §3 (quadro de dados) só preveem
  `agendado/cancelado/finalizado`. Criar `EmAndamento` no enum ou restringir o
  filtro a 3 valores no MVP?
- **L2** — Campo `titulo` (formato simples) não existe no domínio. Derivar do
  nome do primeiro serviço? Concatenar serviços? Usar nome do cliente?
- **L3** — `servicosResumo`: regra exata da string curta ("Lavagem + 1").
- **L4** — Política de exposição de `cpfCnpj` e `telefone/celular` no detalhado
  (mascarar / expor / omitir).
- **L5** — `usuarioId` filtra por `ResponsavelId` ou por `CriadoPor`?
- **L6** — 404 de filial inexistente: usar ou tratar como lista vazia?
- **L7** — Escopo de UI: o card é só backend ou inclui tela React?

## Tarefas — trilha Backend (.NET)

| ID | Tarefa | Esforço | Dep. |
|----|--------|---------|------|
| BE-01 | Slice `Agendamentos/ListarAgenda`: `ListarAgendaQuery` + enum `FormatoAgenda` | PP | L1,L5 |
| BE-02 | `ListarAgendaQueryValidator` (formato, inicio<fim, janela 31d, filialId UUID, status/IDs opcionais) | P | L1 |
| BE-03 | Porta `IAgendaConsultaRepository` + DTOs `AgendaItemSimples`/`AgendaItemDetalhado` | P | L2,L3,L4 |
| BE-04 | Implementação EF Core do repositório (projeção, joins, ordenação `inicio ASC, criadoEm ASC`, sem cache) | M | BE-03 |
| BE-05 | `ListarAgendaHandler` (projeção simples/detalhado a partir da mesma fonte) | P | BE-01,BE-03 |
| BE-06 | Endpoint `GET /api/v1/agenda` em `AgendamentosEndpoints` (binding de query, `RequireAuthorization`) | P | BE-05 |
| BE-07 | Envelope de resposta `{ message, data, traceId }` + retorno vazio padronizado | PP | BE-06 |
| BE-08 | Mapeamento de erros HTTP (400/401/403/404?/500) com mensagens do card | PP | BE-06,L6 |
| BE-09 | Logs estruturados (filtros aplicados, tempo de resposta) — RNF009 | PP | BE-05 |
| BE-10 | Testes de unidade do validator + testes de integração dos 11 itens do checklist QA | M | BE-06 |

## Tarefas — trilha Frontend (React) — somente se L7 = inclui UI

| ID | Tarefa | Esforço | Dep. |
|----|--------|---------|------|
| FE-01 | Cliente de API `getAgenda(formato, filtros)` tipado (TS) | P | BE-06 |
| FE-02 | Tela/visão de agenda com alternância simples/detalhado | M | FE-01 |
| FE-03 | Controles de filtro (período, filial, cliente, responsável, status) | P | FE-01,L1 |
| FE-04 | Estados de vazio / erro / carregando + revalidação após mudanças | P | FE-02 |
| FE-05 | Responsividade (RNF007) e tema claro/escuro (RNF010) | P | FE-02 |

## Definition of Ready
- RF009 vinculado; lacunas L1–L7 decididas (L1, L2, L3, L4, L5 são bloqueantes
  para BE; L6 bloqueia BE-08; L7 define se FE entra na sprint).
- Contratos de DTO simples/detalhado congelados.
- Critérios de aceite escritos e estimados.

## Definition of Done
- Endpoint implementado conforme contrato; 13 itens do checklist de implementação cobertos.
- 11 itens do checklist QA com testes automatizados verdes.
- Validação server-side, HTTPS (RNF004), respostas com `traceId`, logs estruturados (RNF009).
- Consistência simples vs detalhado provada por teste.
- Se houver UI: responsiva (RNF007) e com tema (RNF010).
- Homologação com o proprietário (premissa A1 / CA005).
