# Backlog — Card 204 / RF017 — Cadastro de filiais para operação multiunidade

> Status: implementado. Lacunas L1–L8 ratificadas pelo ADR-0007 (Aceita) e entregues nesta PR. Próximo passo: backfill de `codigo` em homologação e fechamento do card 207 (`NOT NULL` em `codigo`).
> Rastreabilidade base: P6 (capacidade operacional rígida) + P7 (conflito entre filiais) → RF017 (Must) + RF018 (Must) — habilitadores de RF019/RF020 → UC009 → Módulo Filiais / Cadastro (DAT §4.1) / Backend/API.
> Origem: a entidade `Filial` já existe no domínio e é referenciada por agendamento, agenda e auditoria, mas o backend nunca expôs o endpoint de cadastro. O frontend (`frontend/src/services/filialService.ts`) já consome `GET /api/v1/filiais?ativo=true` e está bloqueado por 404 — ver lacuna L7 sobre incluir a listagem neste card ou abrir card derivado.

## Resumo executivo

Expõe o cadastro de filiais (RF017) e consolida as regras de capacidade (RF018 /
RN009) e habilitação para agendamento (RF019/RF020/RN010/RN011). A entidade
`Filial` já existe em `backend/src/CarWash.Domain/Entities/Filial.cs` com `Id`,
`Nome`, `Ativa`, `CelulasAtivas`, `Timezone` e auditoria — este card **estende**
o agregado para incluir os campos exigidos pela spec do usuário (`codigo`,
`cnpj`, endereço estruturado, cidade/UF), abre o slice
`CarWash.Application/Filiais/Criar/` no padrão CQRS-lite (ADR 0003) e adiciona o
endpoint `POST /api/v1/filiais` com defesa em profundidade: validator
FluentValidation → invariantes no método de fábrica → UNIQUE/CHECK no banco. A
filial criada já fica acessível para o agendamento porque
`IAgendamentoCatalogoRepository.ObterFilialResumoAsync` consulta a tabela
`filiais` diretamente (CA4) — RF019/RF020 estavam represados apenas pela ausência
deste cadastro. A migration é **aditiva** (colunas novas nullable em produção)
para permitir rollout sem janela de manutenção, com backfill mínimo descrito
abaixo.

## User stories

### US-204.1 — Cadastro de filial (caminho feliz)
**Como** Administrador, **quero** cadastrar uma filial informando nome, código,
CNPJ, endereço, cidade, UF e quantidade de células ativas **para que** a unidade
operacional fique apta para receber agendamentos imediatamente.
(Card: RF017 + RF018; mapeia CA1 do usuário → CA-204.1.)

### US-204.2 — Bloqueio de duplicidade
**Como** Administrador, **quero** ser bloqueado ao tentar cadastrar uma filial
com `codigo`, `cnpj` ou `nome` (ver L1) já existente **para que** a base de
filiais permaneça única e a referência por `filialId` seja inequívoca.
(Card: RF017; mapeia CA3 → CA-204.3.)

### US-204.3 — Validação de campos obrigatórios e formato
**Como** Administrador, **quero** receber 400 com erro por campo quando um campo
obrigatório está ausente, o tamanho fora da faixa, o CNPJ inválido, a UF fora
das 27 oficiais ou as `celulasAtivas` fora de 1..100 **para que** eu corrija o
erro sem ambiguidade. (Card: RF017 + RF018 + RN009 + invariantes do VO
`Endereco`; mapeia CA2 → CA-204.2.)

### US-204.4 — Filial cadastrada aparece no agendamento
**Como** Funcionário que está criando um agendamento, **quero** que a filial
recém-cadastrada apareça na seleção **para que** eu não precise reiniciar a
sessão para usá-la. (Card: RF019 + RN010 — `ObterFilialResumoAsync` já lê da
tabela `filiais`; mapeia CA4 → CA-204.4. Ver L7 quanto à listagem.)

### US-204.5 — Filial inexistente bloqueia agendamento
**Como** Funcionário, **quero** que o agendamento com `filialId` inexistente
seja rejeitado **para que** não fiquem agendamentos órfãos. (Card: integridade
referencial — `CalculadoraResumoAgendamento.GarantirFilialAsync` já lança
`NotFoundException` ⇒ 404; mapeia CA5 → CA-204.5.)

### US-204.6 — Filial inativa bloqueia novos agendamentos
**Como** Administrador, **quero** que filial com `Ativa = false` bloqueie a
criação de novos agendamentos **para que** unidades temporariamente desativadas
não recebam serviços. (Card: RN010 + `RecursoInativoException` → 422 já
implementado no `CalculadoraResumoAgendamento`; mapeia CA6 → CA-204.6. A
alteração de status fica fora deste card — ver L8.)

### US-204.7 — Cadastro exige autenticação e permissão
**Como** sistema, **quero** rejeitar o cadastro de filial sem JWT válido (401) e
sem permissão de gestão (403) **para que** apenas perfis autorizados afetem a
operação multiunidade. (Card: RNF003 + RNF004; mapeia CA7+CA8 → CA-204.7 e
CA-204.8. Política de permissão precisa de decisão — ver L5.)

### US-204.8 — Falha interna não vaza detalhes
**Como** Administrador, **quero** que uma falha de infraestrutura responda 500
com `correlationId` e mensagem genérica **para que** eu tenha rastreabilidade
sem exposição de stack. (Card: RNF003 + middleware já existente; mapeia CA9 →
CA-204.9.)

## Lacunas para decisão (Arquiteto / CEO)

- **L1 (bloqueante)** — **Unicidade de `nome`**: a tabela `filiais` já possui
  `uk_filiais_nome` (UNIQUE case-sensitive). A spec diz "unicidade
  recomendada". Opções: (a) manter UNIQUE case-sensitive como hoje; (b) tornar
  case-insensitive (índice funcional `LOWER(nome)`); (c) relaxar para
  não-unique e usar só `codigo` + `cnpj` como chaves naturais. Recomendação
  analista: **(b) case-insensitive** — preserva o controle anti-duplicidade
  típico de cadastros gerenciais e elimina "Filial Centro" vs "FILIAL CENTRO".
- **L2 (bloqueante)** — **CNPJ obrigatório**: a spec deixa "quando política
  exigir". O CarWash hoje **não opera como marketplace** (RN001), todas as
  filiais são do mesmo proprietário/CNPJ ou do mesmo grupo econômico. Opções:
  (a) `cnpj` obrigatório e único; (b) opcional mas único quando presente; (c)
  opcional sem unicidade. Recomendação analista: **(b) opcional + UNIQUE
  parcial (NULLS DISTINCT no Postgres)** — permite cadastrar filiais sob o
  mesmo CNPJ raiz quando aplicável e mantém integridade quando preenchido.
  Confirmar com o proprietário se há cenário de múltiplas filiais sob o mesmo
  CNPJ no MVP.
- **L3** — **UF**: a spec aceita só regex `[A-Z]{2}`. O VO `Endereco` já valida
  contra a lista oficial das 27 UFs (`UfsValidas` em `Endereco.cs:13`).
  Recomendação analista: **reaproveitar `Endereco`** — mesmo nível de defesa
  do cliente, sem precisar criar VO `Uf` isolado. Custo de não reaproveitar:
  divergência de validação entre cadastro de filial e cliente. Ver L6 sobre
  estrutura de `Endereco` na spec (a spec usa só `endereco` string + `cidade`
  + `uf`; o VO exige `cep`, `logradouro`, `numero`, `bairro`).
- **L4** — **Campo `ativo` no POST**: a spec diz "opcional, default `true`". O
  domínio já cria com `Ativa = true` na fábrica e expõe `Inativar()/Ativar()`.
  Recomendação analista: **ignorar `ativo` no POST** (sempre criar ativo) e
  abrir card derivado para `PATCH /api/v1/filiais/{id}/status` (mesmo padrão
  de `AlterarStatusCliente/Servico/Usuario`). Justificativa: criar inativa é
  caso de borda raro e ortogonal ao fluxo "cadastrei e quero usar".
- **L5 (bloqueante)** — **Permissão**: hoje todos os endpoints administrativos
  usam apenas `RequireAuthorization()` sem policy específica (ver
  `Program.cs` — só existe a policy `auth-login` para rate limiting). A spec
  fala em "permissão de gestão de filiais". Opções: (a) seguir o padrão da
  casa (`RequireAuthorization()` puro — qualquer usuário autenticado cadastra)
  até o RF-FUT003 (perfis); (b) criar agora a policy `filiais.gerenciar`
  reaproveitável também para usuários e serviços; (c) limitar via claim de
  papel (`role=Administrador`) — exigiria popular `Usuario.Perfil` em
  `LoginHandler`. Recomendação analista: **(a) seguir o padrão da casa no
  MVP** + sinalizar CA-204.8 como "verificado quando RF-FUT003 chegar". Não
  inventar policy isolada para filial sem decisão de governança de papéis.
- **L6 (bloqueante)** — **Estrutura do endereço**: a spec do usuário descreve
  `endereco` (string 5..255), `cidade` (2..100), `uf` (2). O VO `Endereco` da
  casa exige `cep`, `logradouro`, `numero`, `complemento`, `bairro`, `cidade`,
  `uf`. Opções: (a) seguir a spec do usuário (uma string + cidade + uf) e
  criar um VO mais raso `EnderecoFilial`; (b) adotar `Endereco` completo (igual
  cliente) — mais campos no payload, mas consistente. Recomendação analista:
  **(b) reaproveitar `Endereco` completo** — filial precisa de endereço
  estruturado para integrações futuras (NF-e, mapa, frete) e o custo extra no
  payload é 4 campos. Se o CEO priorizar simplicidade do MVP, cair na (a) com
  novo VO. Bloqueante pois muda contrato HTTP, validator, migration e teste.
- **L7** — **Escopo de leitura**: o frontend (`filialService.ts:25`) já chama
  `GET /api/v1/filiais?ativo=true` e está com 404. A spec do usuário só pede
  cadastro. Opções: (a) entregar GET de listagem ativa **neste card** para
  destravar RF019 end-to-end; (b) abrir card derivado 205 só para o GET.
  Recomendação analista: **(a) listar neste card** — sem o GET, US-204.4 não é
  verificável manualmente pela UI e o card 132/RF009 fica em homologação
  parcial. Custo extra: P (handler + endpoint + 1 teste). Decisão final do
  arquiteto pois afeta o T-shirt total do card.
- **L8** — **`PATCH /status` de filial**: explicitamente fora do escopo desta
  spec, mas necessário para US-204.6 (filial inativa). Recomendação analista:
  **manter fora**, abrir card derivado 206 quando o card 204 estiver
  homologado. US-204.6 ainda é testável via seed de filial inativa.

## Critérios de aceite (Given/When/Then)

1. **CA-204.1 — Sucesso (RF017, mapeia CA1):** Dado um payload válido (nome,
   código, CNPJ opcional, endereço, cidade, UF, `celulasAtivas` entre 1 e 100),
   quando enviar `POST /api/v1/filiais`, então o sistema responde `201 Created`
   com `Location: /api/v1/filiais/{id}` e envelope
   `{ id, mensagem, traceId }` (padrão de `CriarClienteResponse`). A filial fica
   `ativa = true` por default (L4).
2. **CA-204.2 — Campos obrigatórios e formato (mapeia CA2):**
   - `nome` ausente, em branco, `<3` ou `>120` → 400 com chave `nome`.
   - `codigo` ausente, fora de 2..20, com caractere não-alfanumérico → 400.
   - `cnpj` inválido (DV) ou fora de 14 dígitos → 400 (validador já existe no VO
     `Cnpj`).
   - `uf` fora das 27 oficiais → 400 (defesa via VO `Endereco`, ver L6).
   - `celulasAtivas` ausente, `<1` ou `>100` → 400 com chave `celulasAtivas`
     (RN009 + CA008 do DRP).
3. **CA-204.3 — Duplicidade (mapeia CA3):**
   - `codigo` já existente → 409 Conflict com slug `filial-codigo-ja-existe`.
   - `cnpj` já existente (e política L2 = único) → 409 com slug
     `filial-cnpj-ja-existe`.
   - `nome` já existente (política L1 escolhida) → 409 com slug
     `filial-nome-ja-existe`.
   - Defesa em profundidade: pré-check no handler + UNIQUE no banco
     (`uk_filiais_codigo`, `uk_filiais_cnpj`, `uk_filiais_nome_lower`). O
     repositório traduz `DbUpdateException` por violação de UK em
     `ConflictException` (padrão `CriarVeiculoHandler`).
4. **CA-204.4 — Filial criada disponível para agendamento (mapeia CA4):** dado
   uma filial cadastrada e ativa, quando o frontend chamar
   `POST /api/v1/agendamentos/pre-confirmacao` com `filialId` apontando para
   ela, então a pré-confirmação responde 200 (ou bloqueia por outra invariante,
   nunca por "filial não encontrada"). Testar contra o banco real
   (Testcontainers) executando o slice `Filiais/Criar` seguido de
   `PreConfirmarAgendamento`.
5. **CA-204.5 — `filialId` inexistente (mapeia CA5):** dado `filialId` que não
   existe, quando criar agendamento, então responde 404 com mensagem "Filial
   informada não foi encontrada." (já implementado em
   `CalculadoraResumoAgendamento.GarantirFilialAsync` — confirmar na suíte).
6. **CA-204.6 — Filial inativa (mapeia CA6):** dado uma filial com `Ativa =
   false` (via seed), quando criar agendamento, então responde 422 com slug
   `recurso-inativo` (já implementado — confirmar na suíte). Sem `PATCH /status`
   neste card (L8).
7. **CA-204.7 — Sem JWT (mapeia CA7):** chamada sem `Authorization: Bearer` →
   401. Endpoint herda `RequireAuthorization()` do grupo `/api/v1/filiais`.
8. **CA-204.8 — Sem permissão (mapeia CA8):** **provisório** sob L5(a) — no MVP
   qualquer usuário autenticado consegue cadastrar; CA permanece "checked" como
   "verificação adiada para RF-FUT003". Se L5(b)/(c) for escolhida, o teste
   passa a verificar 403 para usuário sem claim/role apropriada.
9. **CA-204.9 — Falha interna (mapeia CA9):** simulando exceção em
   `IFilialRepository.AdicionarAsync` (ex: connection drop), o middleware
   retorna 500 com `{type, title: "Não foi possível concluir a operação...",
   status:500, correlationId}` sem stack — comportamento já garantido pelo
   `ExceptionHandlingMiddleware:185`.
10. **CA-204.10 — Auditoria (RNF009):** o `AuditLogInterceptor` já lista
    `Filial` em `EntidadesAuditaveis` (linha 23). O cadastro deve gerar um
    `AuditLog` com evento `filial.criada` + `CorrelationId` + `UsuarioId`.
    Verificado em integração.
11. **CA-204.11 — Cobertura de testes de negócio (CA011 do DRP):** suíte de
    integração cobre CA-204.1, CA-204.2 (mín. 3 cenários: nome inválido, UF
    inválida, células fora da faixa), CA-204.3 (codigo + cnpj + nome se L1
    aplicar), CA-204.4 (e2e com agendamento), CA-204.5, CA-204.6 e CA-204.7.

## Tarefas — trilha Backend (.NET)

> Premissa de migration: **aditiva e nullable em produção** (rollout) — colunas
> novas (`codigo`, `cnpj`, `endereco_*`) entram com `NULL` permitido + UK
> parcial `WHERE codigo IS NOT NULL`. Backfill mínimo: usar `nome` slugificado
> para `codigo` da única filial seed existente (se houver). Depois um card
> derivado (207) tornaria `codigo NOT NULL` quando o backfill estiver concluído.
> Justificativa: zero migration rollback no banco, zero downtime e zero risco
> para os testes de integração já verdes.

| ID | Tarefa | Esforço | Dep. |
|----|--------|---------|------|
| BE-01 | Estender `Filial` (Domain): adicionar `Codigo`, `Cnpj?` (VO), `Endereco?` (VO) + novo método de fábrica `Filial.Criar(id, nome, codigo, celulasAtivas, endereco, cnpj?, timezone?)` validando invariantes; manter a fábrica antiga `[Obsolete]` para não quebrar seeds. **Não** alterar visibilidade de campos existentes. | P | L1, L2, L6 |
| BE-02 | Atualizar `FilialConfiguration` (Infrastructure): coluna `codigo varchar(20)` + UNIQUE `uk_filiais_codigo` (parcial: `WHERE codigo IS NOT NULL` enquanto nullable); `cnpj varchar(14)` + UNIQUE parcial `uk_filiais_cnpj NULLS DISTINCT` (L2); colunas `endereco_*` em owned type (`Endereco`) com mesmos índices/CHECKs do `Cliente.Endereco`; trocar `uk_filiais_nome` por índice case-insensitive `uk_filiais_nome_lower` (L1); índice de busca `idx_filiais_cidade_uf` se L7 entregar GET com filtro por cidade. | P | BE-01 |
| BE-03 | **Nova migration** EF Core `AdicionaCadastroFilial` em `Persistence/Migrations/` (ADR 0006 — **não** editar `20260513114525_InitialSchema`). Operações: `ALTER TABLE filiais ADD COLUMN codigo`, `cnpj`, colunas de endereço, CHECK `ck_filiais_codigo_formato` (regex `^[A-Z0-9]{2,20}$`), UKs novos, drop do `uk_filiais_nome` cru + criação do parcial `LOWER(nome)`. Verificar Designer.cs e snapshot. | M | BE-02 |
| BE-04 | Slice `CarWash.Application/Filiais/Criar/`: `CriarFilialCommand` (record `ICommand<CriarFilialResponse>`), `CriarFilialRequest`, `CriarFilialResponse` (`Id`, `Mensagem`, `TraceId`), `EnderecoRequest` reaproveitando `Application/Clientes/Common/EnderecoRequest.cs` (ou criando `Filiais/Common/EnderecoFilialRequest.cs` se L6=a). | P | BE-01 |
| BE-05 | `CriarFilialCommandValidator` (FluentValidation): `nome` 3..120 trim + `SanitizeTextOrNull`; `codigo` regex `^[A-Z0-9]{2,20}$`, normalizado para UPPER no validator (`InputNormalizer.SanitizeTextOrNull` + `ToUpperInvariant`); `cnpj` opcional (L2) ou obrigatório, 14 dígitos + DV via `DocumentoValidator.CnpjValido`; bloco `When(endereco != null)` reaproveitando regras do `CriarClienteCommandValidator` (CEP 8 dig, logradouro, número, bairro, cidade, UF 2 chars); `celulasAtivas` entre 1 e 100 (RN009 / CA008). | P | BE-04 |
| BE-06 | Porta `IFilialRepository` em `CarWash.Application/Filiais/Persistence/`: `ExisteCodigoAsync`, `ExisteCnpjAsync`, `ExisteNomeAsync` (case-insensitive se L1=b), `AdicionarAsync(filial, correlationId, usuarioId, ct)`, `ObterPorIdAsync`, `ListarAsync(ativo?, busca?, pagina, tamanhoPagina, ct)` — segue o padrão de `IClienteRepository`/`IServicoRepository`. | P | BE-04 |
| BE-07 | Implementação EF Core `FilialRepository` em `CarWash.Infrastructure/Persistence/Repositories/`. Tratamento de `DbUpdateException` por UK → `ConflictException` com slug específico (`filial-codigo-ja-existe`, `filial-cnpj-ja-existe`, `filial-nome-ja-existe`) seguindo padrão de `ClienteRepository` (já mapeia UKs em ConflictException). | M | BE-06 |
| BE-08 | `CriarFilialHandler` (`ICommandHandler<,>`): pré-checks de `codigo`/`cnpj`/`nome` → cria via `Filial.Criar(...)` → `RegistrarCriadoPor(usuarioId)` (precisa adicionar `RegistrarCriadoPor` análogo ao `Cliente.RegistrarCriadoPor` em `Filial` — ver BE-01) → `AdicionarAsync`. Auditoria via `AuditLogInterceptor` (Filial já listada). | P | BE-04, BE-07 |
| BE-09 | Excepts dedicadas em `CarWash.Application/Filiais/Common/`: `FilialCodigoJaExisteException`, `FilialCnpjJaExisteException`, `FilialNomeJaExisteException` herdando de `ConflictException` com slug fixo. | PP | BE-04 |
| BE-10 | Endpoint `MapFiliais()` em `CarWash.Api/Endpoints/Filiais/FiliaisEndpoints.cs`: `app.MapGroup("/api/v1/filiais").WithTags("Filiais").RequireAuthorization()` → `MapPost("/", CriarAsync)` com `.Produces<CriarFilialResponse>(201)`, `ProducesProblem(400/401/403/409/422/500)`. Marker de log `CriarFilialEndpointMarker`. Body camelCase (padrão `System.Text.Json` da casa — confirmado em `ClientesEndpoints`). | P | BE-08 |
| BE-11 | Registrar `MapFiliais()` em `CarWash.Api/Endpoints/EndpointRouteBuilderExtensions.cs` (depois de `MapClientes()` e antes de `MapAgendamentos()`). | PP | BE-10 |
| BE-12 | **(condicional L7)** Slice `Filiais/Listar/`: `ListarFiliaisQuery(ativo?, busca?, pagina=1, tamanhoPagina=20)`, handler, `ListaFiliaisResponse` com `{ itens: [{id,nome,codigo,cidade,uf,ativo}], total }` (compatível com `FilialResumo` do front: `frontend/src/types/filial.ts`). Endpoint `GET /api/v1/filiais`. | P | BE-06 (porta), BE-10 (endpoint) |
| BE-13 | Logs estruturados no endpoint e handler: `TraceId`, `FilialId`, `UsuarioId`, `Codigo` (não logar CNPJ — PII fiscal). | PP | BE-10 |
| BE-14 | Testes de unidade do validator: nome inválido (3 casos), código inválido (2 casos), CNPJ inválido, UF inválida, células fora da faixa, payload completo válido. | P | BE-05 |
| BE-15 | Testes de unidade do domínio: `Filial.Criar` deve falhar para nome vazio, células fora da faixa, código mal formado, endereço inválido; deve criar com `Ativa=true`. | PP | BE-01 |
| BE-16 | Testes de integração (Testcontainers + Postgres) cobrindo CA-204.1, CA-204.2 (selecionando 3 cenários representativos), CA-204.3 (codigo, cnpj, nome — se L1=b), CA-204.4 (e2e cria filial + pré-confirma agendamento), CA-204.5 (`filialId` inexistente), CA-204.6 (filial inativa via seed direta), CA-204.7 (401 sem JWT), CA-204.10 (auditoria gerada). Checklist CA-204.11. | G | BE-10, BE-12, BE-07 |
| BE-17 | **(condicional L7)** Atualizar mock handler `frontend/src/test/handlers.ts:156` para refletir contrato real (caso o response volte diferente do esperado pelo front). | PP | BE-12 |

## Tarefas — trilha Frontend (React) — somente sinalização

| ID | Tarefa | Esforço | Dep. |
|----|--------|---------|------|
| FE-01 | Remover banner "ENDPOINT_FILIAIS_PENDENTE" em `frontend/src/services/filialService.ts:17` quando BE-12 entregar a listagem. | PP | BE-12 |
| FE-02 | (Pós-MVP — fora deste card) Tela de cadastro de filial consumindo `POST /api/v1/filiais`. Hoje só existe consumo de **listagem** no front; criação é via Postman/SQL. | — | — |

## Definition of Ready (para o arquiteto iniciar Stage 2)

- [ ] L1 decidida (estratégia de unicidade de `nome`).
- [ ] L2 decidida (CNPJ obrigatório? unicidade?).
- [ ] L5 decidida (policy ou `RequireAuthorization` puro).
- [ ] L6 decidida (estrutura de endereço — VO completo vs raso).
- [ ] L7 decidida (GET de listagem entra neste card ou em derivado 205).
- [ ] L3, L4 e L8 ratificadas (recomendações analista aceitas).
- [ ] Contrato HTTP do request/response congelado e alinhado com `frontend/src/types/filial.ts` (especialmente o response do GET, se L7=a).
- [ ] Decisão sobre rollout aditivo da migration confirmada (vs not-null direto) — recomendação analista: aditiva.

## Definition of Done

- Endpoint exposto e registrado em `EndpointRouteBuilderExtensions.MapCarWashEndpoints`.
- Validator + invariantes do domínio + UK/CHECK do banco cobrindo as 3 camadas de defesa para `codigo`, `cnpj` e `celulasAtivas`.
- Mínimo 8 testes de integração verdes contra Postgres real (Testcontainers): CA-204.1, CA-204.2 (3 cenários), CA-204.3 (2–3 cenários), CA-204.4 (e2e), CA-204.5, CA-204.6, CA-204.7, CA-204.10.
- Validação server-side (RAT03), HTTPS (RNF004), respostas com `correlationId` (padronizado pelo `ExceptionHandlingMiddleware`), logs estruturados (RNF009 + CA-204.10 — auditoria interceptada automaticamente).
- Build verde no pipeline + lint zero warning (`dotnet format` + `dotnet build` sem novos warnings).
- Migration aplicada com sucesso em ambiente local de cada dev backend (`dotnet ef database update --project backend/src/CarWash.Infrastructure --startup-project backend/src/CarWash.Api`).
- Code review aprovado por 1 dev backend + 1 arquiteto.
- Homologação com o proprietário (premissa A1 / CA005): cenário "cadastrar Filial Matriz com CNPJ X, código MTZ, células 50; tentar duplicar; criar agendamento usando a nova filial".

## Prioridade e estimativa

- **Prioridade:** Must (P6+P7 → RF017+RF018 → CA001).
- **Esforço total:** **M–G** dependendo de L7 (GET incluso): M sem GET, G com GET. Estimativa absoluta: 3–5 dias de 1 dev backend + 0,5 dia de revisão pelo arquiteto + 0,5 dia de homologação. **Não comprometer prazo absoluto sem o CEO** (DAT §11 / roadmap).
- **Dependências externas:** decisões L1, L2, L5, L6 e L7 são bloqueantes para Stage 2.
- **Bloqueia:** CA001 (Must para Go/No-Go do MVP — RF017+RF018+RF019+RF020 todos dependem desta entrega para fechar a matriz de rastreabilidade do DRP §10).

## Rastreabilidade resumida

| Rastreável | ID |
|------------|----|
| Problema (DVP-E §4.1) | P6 (capacidade rígida), P7 (conflito entre filiais) |
| Requisito (DRP §3) | RF017 (Must), RF018 (Must); habilitadores de RF019, RF020 |
| Regra de negócio (DRP §4) | RN009 (1..100 células), RN010 (filial obrigatória no agendamento), RN011 (anti-conflito de veículo entre filiais) |
| Critério de aceite global (DRP §10) | CA001, CA007 (rejeição sem filial — já implementado), CA008 (células 1..100), CA011 (cobertura de testes) |
| Risco mitigado (DAT §11) | RAT01 (governança de schema — migration aditiva nova), RAT03 (validação server-side em camadas) |
| Módulo (DAT §4.1) | Filiais / Cadastro |
| ADR base | ADR 0001 (UUID em app), ADR 0003 (Minimal API + CQRS vertical slices), ADR 0006 (árvore canônica de migrations) |
| Casos de uso (DRP §6) | UC009 (Configurar filial e células ativas) |

## Pontos de validação com proprietário (premissa A1)

1. **Confirmar L2:** "Cada filial tem CNPJ próprio ou todas operam sob um único CNPJ?" — define se CNPJ é obrigatório no MVP.
2. **Confirmar L6:** "Para fins de NF-e/contato futuro, o endereço da filial precisa ser estruturado (CEP, logradouro, número, bairro) ou uma única linha de texto resolve no MVP?"
3. **Confirmar política de capacidade inicial:** filiais novas começam com quantas células ativas? Há um default que faça sentido (ex: 10) ou sempre exigir do operador?
4. **Confirmar `codigo`:** "Vocês já têm uma convenção interna de código de filial (ex: MTZ, FILIAL01)?" — se sim, documentar e treinar.
