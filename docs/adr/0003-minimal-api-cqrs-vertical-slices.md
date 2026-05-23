# ADR 0003 — Minimal API + CQRS com Vertical Slices (sem MediatR)

- **Status:** Aceita
- **Data:** 2026-05-17
- **Autores:** Guilherme Brogio (arquiteto técnico) — decisão formalizando o padrão já adotado nos slices `Auth` e `Usuarios`.
- **Escopo:** Backend `.NET 8` — camada HTTP (`CarWash.Api`) e camada de aplicação (`CarWash.Application`).

---

## Contexto

O backend do CarWash precisa expor uma API REST estável para o frontend React e, internamente, organizar **dezenas de casos de uso** (RFs do DRP — autenticação, cadastro de usuários, agendamento com regra anti-conflito RN011, finalização de atendimento, etc.). A camada HTTP do .NET 8 oferece duas abordagens principais:

1. **MVC Web API** — controllers herdando de `ControllerBase`, ações decoradas com atributos `[HttpGet]`/`[HttpPost]`, pipeline com filtros e binding por convenção. É o padrão histórico do ASP.NET.
2. **Minimal API** — endpoints definidos como funções via `app.MapPost("/rota", handler)`, com `EndpointFilter` e `TypedResults`. Introduzido no .NET 6 e amadurecido no .NET 7/8.

Ortogonalmente, a organização da camada de aplicação pode seguir:

- **Camadas tradicionais** (`Controllers → Services → Repositories`) — comum em projetos MVC. Tende a produzir "fat services" e acoplamento difuso conforme o domínio cresce.
- **CQRS (Command Query Responsibility Segregation)** — cada operação vira uma classe (`Command`/`Query`) com seu handler dedicado. Casa naturalmente com **Vertical Slice Architecture** (cada feature mora em uma pasta isolada com command, validator, handler, DTOs).

No momento da decisão, o código já apresenta divergência:

- `CarWash.Api/Endpoints/Auth/AuthEndpoints.cs` e `CarWash.Api/Endpoints/Usuarios/UsuariosEndpoints.cs` usam **Minimal API + CQRS** (slices em `CarWash.Application/Auth/Login`, `CarWash.Application/Usuarios/CriarUsuario`, etc., com `ICommandHandler<,>` próprio).
- `CarWash.Api/Controllers/ClientesController.cs` usa **MVC + IClienteService** — herança da fase inicial do projeto.

A inconsistência precisa ser resolvida antes que mais RFs (agendamento, dashboard, histórico) sejam implementados.

---

## Decisão

**Todo endpoint novo segue o padrão Minimal API + CQRS, organizado em Vertical Slices, com handlers próprios (`ICommandHandler<,>` / `IQueryHandler<,>`) — sem MediatR.**

Concretamente:

- **HTTP — Minimal API.** Endpoints definidos via `app.MapGroup("/api/v1/<recurso>").MapPost(...)`. Controllers MVC ficam reservados a cenários excepcionais (upload multipart pesado, server-sent events que demandem `ControllerBase`) — não há nenhum no MVP.
- **CQRS.** Cada caso de uso vira um par `Command`/`Query` + `Handler`. Comandos retornam DTO de resposta; queries retornam DTOs de leitura. Não há barramento — o endpoint resolve o handler concreto via DI (`[FromServices] ICommandHandler<TCommand, TResponse>`).
- **Vertical Slice Architecture.** Cada feature mora em uma pasta sob `CarWash.Application/<Agregado>/<CasoDeUso>/`, contendo:
  - `XxxCommand.cs` (record imutável, `ICommand<TResponse>`)
  - `XxxCommandValidator.cs` (FluentValidation)
  - `XxxHandler.cs` (implementa `ICommandHandler<XxxCommand, TResponse>`)
  - DTOs/Response em `Common/` quando reutilizados entre slices do mesmo agregado.
- **Sem MediatR.** As interfaces `ICommandHandler<,>` e `IQueryHandler<,>` definidas em `CarWash.Application/Abstractions/Messaging/` substituem o `IMediator`. Registro dos handlers feito por convenção em `CarWash.Application/DependencyInjection.cs` (scan de assembly).
- **Validação.** Aplicada por `EndpointFilter<ValidationFilter<TCommand>>` quando o command é parâmetro direto do endpoint; inline (chamando `IValidator<T>` no handler do endpoint) quando o command é montado a partir de rota + body.
- **Migração.** `ClientesController` será migrado para `CarWash.Api/Endpoints/Clientes/ClientesEndpoints.cs` + slices em `CarWash.Application/Clientes/`. Tarefa rastreada fora desta ADR.

---

## Justificativa

### Por que Minimal API

1. **Performance e overhead.** Minimal API evita o pipeline MVC (model binding pesado, content negotiation, action filters por convenção). Benchmarks oficiais Microsoft mostram throughput ~20–30% superior em endpoints triviais. Não é o driver principal — mas é ganho de graça.
2. **Explícito vence implícito.** Em MVC, é comum descobrir comportamento estranho rastreando convenções (binding por nome, filtros globais, hosts de model state). Em Minimal API, o que o endpoint faz está visível na assinatura: parâmetros, atributos `[FromBody]`/`[FromServices]`, filtros listados após `.MapPost(...)`.
3. **Tamanho do "vocabulário".** MVC traz `ControllerBase`, `ActionResult`, `IActionResult`, `ActionFilterAttribute`, `IAsyncActionFilter`, model binding providers, etc. Minimal API trabalha com poucas primitivas (`IEndpointRouteBuilder`, `IEndpointFilter`, `TypedResults`). Menor curva de leitura para quem entra no projeto.
4. **Alinha com a arquitetura interna.** Como cada slice já é uma unidade pequena (1 command + 1 handler), faz sentido que o endpoint também seja uma função pequena, não um método dentro de uma classe com 10 ações.

### Por que CQRS (separação Command/Query)

1. **Modelo de leitura ≠ modelo de escrita.** Operações de escrita do CarWash carregam regras de negócio densas (RN011 — anti-conflito de agendamento global por veículo; RN003 — janelas operacionais; RN009/RN010 — finalização). Operações de leitura (listagem de agenda, dashboard) são projeções simples otimizadas para a UI. Forçar os dois pelo mesmo serviço/método mistura responsabilidades e prejudica os dois caminhos.
2. **Testabilidade.** Cada handler é uma classe com dependências explícitas no construtor — trivial de testar em unidade (mock de repositório) e em integração (Testcontainers + Postgres real). Não há `HttpContext`, `ControllerBase` ou estado de framework no caminho do teste.
3. **Rastreabilidade requisito→código.** Cada CA (critério de aceite) do DRP mapeia para um command/handler nomeado. Code review e QA acompanham `CA006` → `CriarAgendamentoCommand` → `CriarAgendamentoHandler` sem intermediários.
4. **Audit trail e logging coerentes.** O handler é o ponto único onde a intenção de escrita acontece. Configuração de logging estruturado, `ICurrentRequestContext.DefinirEvento(...)` e emissão de eventos de auditoria vivem no handler — o endpoint só faz transporte.

### Por que Vertical Slice Architecture

1. **Coesão por feature.** Tudo que diz respeito a "criar usuário" mora junto: `CriarUsuarioCommand.cs`, `CriarUsuarioCommandValidator.cs`, `CriarUsuarioHandler.cs`. Refatorar ou remover a feature é mexer em uma pasta.
2. **Baixo acoplamento entre slices.** Slices não dependem uns dos outros — dependem do domínio (`CarWash.Domain`) e da infraestrutura (`CarWash.Infrastructure`). Isso evita o "service god class" comum em arquiteturas em camadas puras.
3. **Casa com a literatura moderna.** Jimmy Bogard (autor do MediatR) e Vladimir Khorikov defendem que Clean Architecture pura em projetos médios produz mais abstrações do que valor; Vertical Slice resolve isso mantendo Domain isolado mas dispensando "Application Services" genéricos.

### Por que SEM MediatR

1. **Custo de licença a partir de 2025.** Em 2024–2025 o autor do MediatR (Jimmy Bogard) anunciou modelo comercial para versões futuras. Mesmo que as versões 12.x permaneçam open source, o vetor de atualização passa a ter custo. Para um projeto acadêmico/MVP, evitar dependência opinativa é prudente.
2. **CQRS não exige um mediator.** O padrão CQRS é a separação **conceitual** entre command e query. O mediator é uma **implementação opcional** de pub/sub in-process. Nada impede o endpoint de receber o handler concreto via DI — é mais explícito e tem stack trace mais legível.
3. **Stack trace e debugging.** `await _mediator.Send(cmd)` esconde quem vai atender. `await handler.HandleAsync(cmd, ct)` mostra na assinatura do endpoint exatamente qual classe será chamada.
4. **Menos uma dependência transitiva.** O projeto fica autocontido na BCL + EF Core + FluentValidation + Argon2 + Npgsql.

---

## Consequências

### Positivas

- **Slices auto-contidos:** mudar uma feature não exige tour pelo projeto inteiro. PRs ficam pequenos e revisáveis.
- **Mapeamento direto DRP↔código:** cada CA tem um command/handler de nome óbvio. QA e auditoria seguem o nome.
- **Sem overhead de framework de mediator:** stack trace direto, sem reflexão para resolver handler, sem dependência paga.
- **Endpoints HTTP minimalistas:** o arquivo `Endpoints/<Recurso>/<Recurso>Endpoints.cs` lista as rotas como uma tabela legível.
- **OpenAPI/Swagger explícito:** cada `.Produces<>()` e `.ProducesProblem(...)` é declarado no endpoint — não há "mágica" de atributos espalhados.
- **Performance levemente superior** em endpoints triviais (cache, healthcheck, lookups).

### Negativas

- **Mais arquivos por feature.** Cada caso de uso vira ≥3 arquivos (command, validator, handler). Em features triviais isso parece exagero — mitigação: cumpre o padrão mesmo em casos simples, ganhando consistência.
- **Mais boilerplate no endpoint** (declarar `.Produces<>()`, `.ProducesProblem(...)` manualmente). MVC infere parte disso de atributos `[ProducesResponseType]`. Mitigação: helper `WithStandardProblems()` em backlog técnico.
- **Sem benefícios "out of the box" de MVC**: model binding complexo (forms, multipart), `IActionFilter` globais, `IActionConstraint`. Para o MVP nenhum é necessário — se surgir, fica registrado se vale exceção.
- **`ClientesController` vira dívida técnica até a migração.** Risco baixo (já em produção interna, sem mudança de contrato HTTP) — tarefa criada no backlog.
- **Curva de aprendizado** para quem só viu MVC. Mitigação: este ADR + [`../arquitetura-backend.md`](../arquitetura-backend.md).

---

## Alternativas consideradas

### A. MVC Web API + Service Layer (padrão clássico)

`Controllers/XxxController.cs` chamando `Services/IXxxService.cs`.

- **Prós:** padrão amplamente conhecido; muita documentação; model binding rico; filtros maduros.
- **Contras:** controllers e services tendem a inchar (responsabilidades difusas); pipeline mais pesado; convenções implícitas dificultam onboarding em projetos grandes; mistura leitura e escrita em um mesmo service.
- **Veredito:** Rejeitada como padrão geral. Permitida em exceções justificadas (não há nenhuma no MVP). Código legado (`ClientesController`) será migrado.

### B. Minimal API + Service Layer (sem CQRS)

Minimal API definindo endpoints, mas o handler interno é `IXxxService` com vários métodos.

- **Prós:** ganha o overhead reduzido de Minimal API; menos arquivos por feature.
- **Contras:** mantém o problema dos "fat services"; perde o mapeamento 1:1 entre CA e classe; testes ficam acoplados a um service com várias responsabilidades.
- **Veredito:** Rejeitada. Resolveria parte do problema HTTP mas não o problema da camada de aplicação, que é o real driver da decisão.

### C. MVC + CQRS (com ou sem MediatR)

Controllers MVC chamando `IMediator.Send(command)` ou handler resolvido por DI.

- **Prós:** mantém os benefícios do CQRS; padrão familiar em projetos enterprise.
- **Contras:** mantém overhead do pipeline MVC sem ganho proporcional; o controller passa a ser um arquivo "vazio" que apenas repassa para o mediator — adiciona indireção sem valor.
- **Veredito:** Rejeitada. Se o controller só repassa, vale tirar o controller da equação.

### D. Minimal API + CQRS com MediatR

Endpoints minimais resolvendo `IMediator` e despachando comandos.

- **Prós:** desacopla endpoint de handler concreto; permite behaviors transversais (logging, validation, retry) via pipeline behaviors do MediatR.
- **Contras:** dependência opinativa (e potencialmente paga); stack trace mais opaco; behaviors transversais já são alcançáveis via `EndpointFilter` (validation) e middlewares (logging, exception). Para o porte do MVP, o ganho não compensa.
- **Veredito:** Rejeitada agora. Pode ser reavaliada se surgir necessidade real de pipeline behaviors (cross-cutting reuso entre dezenas de handlers).

### E. Clean Architecture pura por camadas (sem Vertical Slice)

`Application/Services/UsuarioService.cs` com vários métodos (Criar, Listar, AlterarStatus), separado em camadas estritas.

- **Prós:** familiar para times com background DDD/Clean clássico.
- **Contras:** força agrupamento por **tipo técnico** (Services, DTOs, Validators) em vez de **feature** — viola o princípio "coesão por mudança". PRs ficam espalhados.
- **Veredito:** Rejeitada. Vertical Slice é evolução natural do Clean Architecture para projetos com muitos casos de uso pequenos.

---

## Definições de referência (literatura)

> Esta seção é informativa — fundamenta o vocabulário usado na decisão. Para uma exposição mais didática (com exemplos de código e comparações lado a lado), ver [`../arquitetura-backend.md`](../arquitetura-backend.md).

- **Minimal API.** Modelo de definição de endpoints HTTP introduzido no ASP.NET Core 6, formalizado na documentação oficial como "lightweight syntax for creating HTTP APIs". Endpoints são funções, não métodos de controller; o pipeline MVC (filtros, model binders) é opt-in. — *Microsoft Docs, ".NET Minimal APIs overview".*

- **MVC (Model-View-Controller).** Padrão arquitetural descrito por Trygve Reenskaug (Xerox PARC, 1979) e popularizado em frameworks web por Rails (2005) e ASP.NET MVC (2009). Separa entrada (controller), processamento (model) e apresentação (view). No contexto de Web API, "view" some — o controller serializa diretamente o modelo. — *Reenskaug, "Models–Views–Controllers", 1979; Fowler, "Patterns of Enterprise Application Architecture", 2003, cap. 14.*

- **CQRS (Command Query Responsibility Segregation).** Padrão proposto por Greg Young (2010), derivado de **CQS** (Command-Query Separation, Bertrand Meyer, *Object-Oriented Software Construction*, 1988). Postula que **comandos** (intenções de mudar estado) e **queries** (perguntas que retornam dados) devem ter modelos, caminhos e responsabilidades separados. CQRS **não exige** event sourcing nem barramento — são opcionais. — *Greg Young, "CQRS, Task Based UIs, Event Sourcing", 2010; Fowler, "CQRS", 2011.*

- **Vertical Slice Architecture.** Termo cunhado por Jimmy Bogard (2018) como contraponto à Clean Architecture clássica. Organiza o código por **feature** (caso de uso) em vez de por **camada técnica**. Cada slice é coeso e quase independente — alterar uma feature exige tocar poucos arquivos no mesmo diretório. — *Jimmy Bogard, "Vertical Slice Architecture", jimmybogard.com, 2018.*

- **Clean Architecture.** Síntese de Robert C. Martin (2012) das ideias de Hexagonal (Cockburn), Onion (Palermo) e DDD (Evans). Coloca o domínio no centro; dependências sempre apontam para dentro. Não é incompatível com Vertical Slice — slices preservam o isolamento de `Domain` e `Infrastructure`, mas dispensam a camada "Application Services" genérica. — *Robert C. Martin, "Clean Architecture", 2017; "The Clean Architecture", blog, 2012.*

- **MediatR.** Biblioteca .NET de Jimmy Bogard que implementa o padrão Mediator (GoF, 1994) para despacho in-process de comandos/queries. Anunciou modelo de licenciamento comercial em 2024–2025 (versões futuras). — *github.com/jbogard/MediatR; Gamma et al., "Design Patterns", 1994.*

---

## Implicações operacionais

- **Estrutura de pastas (canônica):**

  ```
  backend/src/
    CarWash.Api/
      Endpoints/
        <Recurso>/<Recurso>Endpoints.cs   # classe estática com Map<Recurso>(this IEndpointRouteBuilder)
      Filters/
        ValidationFilter.cs
      Program.cs                          # chama app.MapAuth().MapUsuarios()...
    CarWash.Application/
      Abstractions/Messaging/
        ICommand.cs, ICommandHandler.cs
        IQuery.cs, IQueryHandler.cs
      <Agregado>/
        <CasoDeUso>/
          <CasoDeUso>Command.cs           # record com ICommand<TResponse>
          <CasoDeUso>CommandValidator.cs  # FluentValidation
          <CasoDeUso>Handler.cs           # ICommandHandler<,>
        Common/
          <Agregado>Response.cs           # DTO de resposta reutilizável
          <Agregado>NotFoundException.cs  # quando aplicável
        Persistence/
          I<Agregado>Repository.cs        # interface no Application; impl em Infrastructure
  ```

- **DI:** `CarWash.Application/DependencyInjection.cs` registra todos os `ICommandHandler<,>`/`IQueryHandler<,>` por scan de assembly. Endpoints recebem o handler concreto via `[FromServices]`.

- **Validação:**
  - Quando o `Command` é parâmetro direto do endpoint: `.AddEndpointFilter<ValidationFilter<TCommand>>()`.
  - Quando o `Command` é montado a partir de rota+body (ex.: `PATCH /usuarios/{id}/status`): validação inline no endpoint chamando `IValidator<T>` (ver `UsuariosEndpoints.AlterarStatusAsync`).

- **OpenAPI:** cada `.MapXxx(...)` declara `.WithName(...)`, `.Produces<T>(StatusCodes.Status<XXX>)` e `.ProducesProblem(...)` para todos os erros possíveis.

- **Migração do legado:**
  - `CarWash.Api/Controllers/ClientesController.cs` → `CarWash.Api/Endpoints/Clientes/ClientesEndpoints.cs`.
  - `CarWash.Application/Services/Clientes/ClienteService.cs` → slices em `CarWash.Application/Clientes/Criar/`, `CarWash.Application/Clientes/ObterPorId/`.
  - Contrato HTTP permanece idêntico (mesmas rotas, mesmos schemas).

- **Code review:** PRs que adicionem endpoint em `Controllers/` precisam de justificativa explícita; default é Minimal API.

---

## Re-avaliação

Esta ADR deve ser revisitada quando:

- Surgir necessidade real de **pipeline behaviors** cross-cutting compartilhados entre dezenas de handlers (logging contextual avançado, retry policy, idempotência) — pode justificar reintroduzir MediatR ou um mediador caseiro.
- O time crescer ao ponto de a curva de aprendizado de Minimal API + Vertical Slice virar gargalo de onboarding (sinal: PRs sendo rejeitados por "padrão errado").
- A Microsoft anunciar mudança significativa no roadmap de Minimal API (deprecação, fork de funcionalidades para MVC).
- O `ClientesController` permanecer não migrado por mais de 2 sprints após a aceitação desta ADR — disparar revisão para tornar a migração obrigatória ou aceitar o controller como exceção permanente.

---

## Referências

- Greg Young — ["CQRS, Task Based UIs, Event Sourcing agh!"](https://cqrs.wordpress.com/documents/cqrs-introduction/), 2010.
- Martin Fowler — ["CQRS"](https://martinfowler.com/bliki/CQRS.html), 2011.
- Jimmy Bogard — ["Vertical Slice Architecture"](https://www.jimmybogard.com/vertical-slice-architecture/), 2018.
- Robert C. Martin — ["The Clean Architecture"](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html), 2012.
- Microsoft — ["Minimal APIs overview"](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/overview).
- Microsoft — ["Choose between controller-based APIs and minimal APIs"](https://learn.microsoft.com/aspnet/core/fundamentals/apis).
- Bertrand Meyer — *Object-Oriented Software Construction*, Prentice Hall, 1988 (CQS original).
- ADR 0001 — [`./0001-geracao-de-uuid-pela-aplicacao.md`](./0001-geracao-de-uuid-pela-aplicacao.md).
- ADR 0002 — [`./0002-hash-de-senha-com-argon2id.md`](./0002-hash-de-senha-com-argon2id.md).
- Guia complementar — [`../arquitetura-backend.md`](../arquitetura-backend.md).
