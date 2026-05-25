# Backlog — Card 200 / RF005 — Endpoint `POST /api/v1/clientes/{id}/veiculos` (cadastro de veículo)

> Status: refinamento do analista. Pendente de decisão de Arquiteto/CEO nas lacunas L1–L3.
> Rastreabilidade base: P2 (cadastro disperso de clientes) → RF005 (Must) + RF004 (Must) → UC003 → Módulo Cadastro de Veículos (DAT §4.1) / Backend/API.
> Origem: dívida do contrato HTTP — o frontend (`frontend/src/services/veiculoService.ts → cadastrar`) já chama `POST /api/v1/clientes/{id}/veiculos` mas o backend não expõe a rota. O `VeiculosController` MVC anterior foi removido por estar fora do padrão da ADR 0003 e nunca foi registrado em `Program.cs`.

## Resumo executivo

Expõe o endpoint de cadastro avulso de veículo seguindo o padrão Minimal API +
CQRS (ADR 0003) — slice em `CarWash.Application/Veiculos/Criar/`, paralelo ao já
existente `Clientes/Criar/`. Cobre RF005 (validar placa e impedir duplicidade)
e RF004 (vínculo obrigatório a um cliente existente), com defesa em
profundidade: validator FluentValidation → value object `Placa` (regex
Mercosul/antigo) → unique index `uk_veiculos_placa` + CHECK
`ck_veiculos_placa_formato` no banco. Não substitui o fluxo de RF021
(veículos cadastrados junto do cliente, já entregue) — este endpoint atende o
caso "adicionar veículo a um cliente já existente" que o frontend chama hoje
sem contrapartida no backend.

## User stories

### US-200.1 — Cadastro de veículo em cliente existente
**Como** Funcionário, **quero** cadastrar um veículo para um cliente já existente
**para que** eu vincule novos carros ao titular sem precisar refazer o cadastro
do cliente. (Card: contrato HTTP, slice de comando; RF005 + RF004; UC003.)

### US-200.2 — Bloqueio de placa duplicada
**Como** Administrador, **quero** que o sistema bloqueie o cadastro de placa
já existente (em qualquer cliente, em qualquer filial) **para que** a base de
veículos permaneça única e consistente. (Card: RN003 + RN011 — base do
"mesmo veículo" usado no anti-conflito de agendamento; CA003.)

### US-200.3 — Validação de formato de placa Mercosul/antigo
**Como** Funcionário, **quero** que o sistema só aceite placa nos formatos
Mercosul (`AAA0A00`) ou antigo (`AAA0000`) **para que** o cadastro fique
consistente e o filtro por placa funcione previsivelmente em toda a aplicação.
(Card: RN003; defesa em profundidade — value object `Placa` + CHECK
`ck_veiculos_placa_formato`.)

### US-200.4 — Cliente inexistente ou inativo
**Como** Funcionário, **quero** receber 404 quando o cliente do path não existir
e 409 (ou 400, dependendo de L2) quando estiver inativo **para que** o erro
seja claro e eu não fique preso a uma mensagem genérica.
(Card: RF004 — vínculo obrigatório; CA002.)

## Lacunas para decisão (Arquiteto / CEO)

- **L1 (bloqueante)** — Idempotência: o slice `Agendamentos/Confirmar` adota
  header `Idempotency-Key` + tabela `idempotencia_requisicoes` (ADR 0004). Os
  slices `Clientes/Criar` e `Usuarios/Criar` **não** adotam. O frontend hoje
  não envia `Idempotency-Key` no cadastro de veículo. **Decisão necessária:**
  cadastro de veículo entra como "não idempotente" (igual a Cliente/Usuario,
  default da casa) ou exige idempotência (igual a Confirmar agendamento)? A
  análise sugere **default da casa = sem idempotência** — o duplo POST cai
  natural no UK `uk_veiculos_placa` (409) sem efeito colateral. Levar à
  arquitetura para registro.
- **L2** — Cliente inativo: a regra do MVP define que veículo pode ser adicionado
  a cliente inativo? Default analista: **não** — retorna 409
  (`ClienteInativoException` análogo ao padrão dos demais slices). Confirmar
  com o proprietário.
- **L3** — Campo `ano` no payload: o domínio `Veiculo` aceita `Ano` opcional
  (1900–2100, CHECK `ck_veiculos_ano`); o `veiculoService.ts` atual **não envia**.
  Decisão: backend aceita `ano?` como opcional desde já (mantendo o frontend
  retrocompatível) ou bloqueamos `ano` até o frontend expor o campo? Default
  analista: **aceitar opcional** — o schema já existe, não custa nada.

## Critérios de aceite (Given/When/Then)

1. **CA-200.1 — Sucesso (RF005, RF004):** Dado um cliente ativo e válido,
   quando enviar `POST /api/v1/clientes/{id}/veiculos` com payload válido,
   então o sistema responde `201 Created` com `Location:
   /api/v1/clientes/{clienteId}/veiculos/{veiculoId}` e envelope
   `{ id, mensagem, traceId }` (padrão de `CriarClienteResponse`).
2. **CA-200.2 — Placa duplicada (RN003, RF005):** Dado um veículo já cadastrado
   com placa `ABC1D23`, quando outro cadastro for tentado (mesmo cliente ou
   cliente diferente, mesma filial ou outra), então o sistema responde `409
   Conflict` com mensagem "Placa já cadastrada." e nenhum registro é gravado.
   Defesa: validator faz pré-check; UK `uk_veiculos_placa` é a rede final.
3. **CA-200.3 — Formato inválido (RN003):** Dado um payload com placa
   `1234567` (ou qualquer string que não case com `^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$`),
   quando o cadastro for tentado, então o sistema responde `400 Bad Request`
   com erro de campo `placa` antes de tocar o banco (validator/value object).
4. **CA-200.4 — Cliente inexistente (RF004):** Dado um `clienteId` que não
   existe na base, quando o cadastro for tentado, então o sistema responde
   `404 Not Found` com mensagem "Cliente não encontrado."
5. **CA-200.5 — Cliente inativo (L2):** Dado um cliente com `Ativo = false`,
   quando o cadastro for tentado, então o sistema responde `409 Conflict` com
   mensagem "Cliente inativo." (pendente confirmação L2).
6. **CA-200.6 — Campos obrigatórios:** `placa`, `modelo`, `fabricante`, `cor`
   ausentes ou em branco retornam `400` com erros de campo individualizados
   (mesmo formato do `ValidationException` dos demais slices).
7. **CA-200.7 — Limites de tamanho:** `modelo`/`fabricante` ≤80, `cor` ≤40,
   `placa` ≤8 (regex já garante 7). Acima → `400`.
8. **CA-200.8 — Autorização (RNF003/RNF004):** Sem token JWT válido →
   `401 Unauthorized`. O endpoint herda `RequireAuthorization()` do grupo.
9. **CA-200.9 — Auditoria (RNF009):** o handler aceita `UsuarioId` opcional
   (vindo do `sub` do JWT) e registra criado_por; log estruturado de criação
   com `TraceId`, `ClienteId`, `VeiculoId`, `UsuarioId` (mascara placa? — não
   é PII de pessoa, log direto).
10. **CA-200.10 — Cobertura de testes de negócio (CA011 do DRP):** suite cobre
    CA-200.2 (duplicidade), CA-200.3 (formato), CA-200.4 (cliente inexistente)
    e CA-200.5 (cliente inativo, se L2 = sim) com testes de integração
    Testcontainers contra Postgres real.

## Tarefas — trilha Backend (.NET)

| ID | Tarefa | Esforço | Dep. |
|----|--------|---------|------|
| BE-01 | Slice `CarWash.Application/Veiculos/Criar/`: `CriarVeiculoCommand` (record, `ICommand<CriarVeiculoResponse>`) + `CriarVeiculoRequest` (DTO HTTP) + `CriarVeiculoResponse` (`Id`, `Mensagem`, `TraceId`) | PP | L3 |
| BE-02 | `CriarVeiculoCommandValidator` (FluentValidation): `placa` regex Mercosul/antigo, `modelo`/`fabricante` ≤80, `cor` ≤40, `ano` opcional 1900–2100, `clienteId` UUID | P | L3 |
| BE-03 | Porta `IVeiculoRepository` em `CarWash.Application/Veiculos/Persistence/` (`AdicionarAsync`, `ExistePlacaAsync`, `ObterClienteAtivoAsync` ou reaproveitar `IClienteRepository`) | P | — |
| BE-04 | Implementação EF Core de `IVeiculoRepository` em `CarWash.Infrastructure/Persistence/Repositories/` — tratamento de `DbUpdateException` por UK `uk_veiculos_placa` → `ConflictException` | P | BE-03 |
| BE-05 | `CriarVeiculoHandler` (`ICommandHandler<,>`): pré-check de cliente existente e ativo (L2) → pré-check de placa (defesa em 2 camadas, mesmo padrão do `CriarClienteHandler`) → cria entidade `Veiculo.Criar(...)` → persiste com auditoria (`RegistrarCriadoPor`) | P | BE-01, BE-03 |
| BE-06 | Endpoint `MapVeiculos()` em `CarWash.Api/Endpoints/Veiculos/VeiculosEndpoints.cs`: `app.MapGroup("/api/v1/clientes/{clienteId:guid}/veiculos").RequireAuthorization()` → `MapPost("/", CriarAsync)` com `.Produces<CriarVeiculoResponse>(201)`, `ProducesProblem(400/401/404/409)` | P | BE-05 |
| BE-07 | Registrar `MapVeiculos()` em `CarWash.Api/Endpoints/EndpointRouteBuilderExtensions.cs` (após `MapClientes()`) | PP | BE-06 |
| BE-08 | Registro do handler e do repositório em `CarWash.Application/DependencyInjection.cs` e `CarWash.Infrastructure/DependencyInjection.cs` (já é automático via scan, validar) | PP | BE-05, BE-04 |
| BE-09 | Logs estruturados (`ILogger<CriarVeiculoEndpointMarker>`) com `TraceId`, `ClienteId`, `VeiculoId`, `UsuarioId` — RNF009 | PP | BE-06 |
| BE-10 | Testes de unidade do validator (regex placa, limites de tamanho, ano fora da faixa) | PP | BE-02 |
| BE-11 | Testes de integração (Testcontainers + Postgres): CA-200.1, CA-200.2, CA-200.3, CA-200.4, CA-200.5, CA-200.8 — checklist CA-200.10 | M | BE-06, BE-04 |

## Tarefas — trilha Frontend (React)

| ID | Tarefa | Esforço | Dep. |
|----|--------|---------|------|
| FE-01 | Validar contrato consumido por `veiculoService.cadastrar` contra o response real (`id`, `mensagem`, `traceId`) | PP | BE-06 |
| FE-02 | Mensagens de erro: mapear 409/400 para feedback no formulário (`useForm`/Zod). Reuso do padrão de `criarCliente`. | PP | BE-06 |
| FE-03 | Smoke test E2E (Playwright/Cypress) opcional do fluxo "criar veículo em cliente existente" | P | FE-02 |

## Definition of Ready

- RF005 + RF004 vinculados; lacunas L1, L2, L3 decididas (L1 e L2 são bloqueantes
  para BE-04/BE-05).
- Contrato HTTP do request/response congelado e alinhado com `veiculoService.ts`.
- Critérios de aceite escritos e estimados.
- Slice `Clientes/Criar` foi reapresentado ao dev responsável como referência.

## Definition of Done

- Endpoint exposto e registrado em `EndpointRouteBuilderExtensions.MapCarWashEndpoints`.
- Validator + value object + UK do banco cobrindo as 3 camadas de defesa para placa (RN003 + RAT03).
- 11 testes (10 CAs + 1 cobertura CA011) verdes no pipeline de integração.
- Validação server-side (RAT03), HTTPS (RNF004), respostas com `traceId`, logs estruturados (RNF009).
- `veiculoService.ts` continuou funcionando sem alteração de payload (regressão zero no frontend).
- Homologação com o proprietário (premissa A1 / CA005) — cenário "adicionar Civic do cliente João".

## Prioridade e estimativa

- **Prioridade:** Must (P2 → RF005 → CA001).
- **Esforço total:** M (BE) + PP (FE) → ~1 sprint de 1 dev fullstack ou 0,5
  sprint de 1 backend + 0,5 dia de frontend para conferência.
- **Dependências externas:** decisão L1/L2/L3.
- **Bloqueia:** RF007/RF019/RF020 dependem do cadastro de veículo funcionar
  pelo backend para o cenário "criar veículo e já agendar" (hoje só RF021
  fechado).

## Decisões do arquiteto (2026-05-25)

- **L1 (idempotência) — CONFIRMADO sem idempotência.** Endpoint humano-driven; RN003 + UK `uk_veiculos_placa` já garantem idempotência efetiva por chave natural (duplo POST cai em 409). Introduzir `Idempotency-Key` aqui seria overkill — segue o default dos slices `Clientes/Criar` e `Usuarios/CriarUsuario`. Reavaliar só se algum job batch/integração externa passar a chamar este endpoint.
- **L2 (cliente inativo) — AJUSTADO para 422 Unprocessable Entity (NÃO 409).** O padrão da casa é claro: `RecursoInativoException` (mapeada para 422 no `ExceptionHandlingMiddleware`) é o que descreve "requisição válida mas estado de negócio impede a operação". 409 fica reservado para conflito de unicidade (placa duplicada, `ConflictException`). A invariante deve viver em **`Cliente.AdicionarVeiculo`** (`Domain/Entities/Cliente.cs:235`) — adicionar `if (!Ativo) throw new DomainException("Cliente inativo não pode receber novos veículos.");` e o handler converte `DomainException` em `RecursoInativoException` (ou o handler lança direto a `RecursoInativoException` antes do pré-check de placa). Defesa de domínio + handler, não só validator.
- **L3 (campo `ano` opcional) — APROVADO.** Aceitar `ano?` opcional desde já; schema/CHECK já existe e custo zero. Frontend permanece retrocompatível.
- **Dependência de migrations:** este card depende da Decisão 1 (consolidação para `Persistence/Migrations/`) estar resolvida antes do BE-11 (integração com Postgres real).

## Rastreabilidade resumida

| Rastreável | ID |
|------------|----|
| Problema (DVP-E §4.1) | P2 |
| Requisito (DRP §3) | RF005 (Must), RF004 (Must), RF003 reuso |
| Regra de negócio (DRP §4) | RN002, RN003, RN011 (base) |
| Critério de aceite global (DRP §10) | CA001, CA003, CA011 |
| Risco mitigado (DAT §11) | RAT03 (validação server-side) |
| Módulo (DAT §4.1) | Cadastro de Veículos |
| ADR base | ADR 0003 (Minimal API + CQRS), ADR 0001 (UUID em app) |
