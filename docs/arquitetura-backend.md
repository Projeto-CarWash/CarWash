# Guia de Arquitetura do Backend — Minimal API + CQRS + Vertical Slices

> **Status:** Vigente (2026-05-17)
> **Decisão formal:** [ADR 0003 — Minimal API + CQRS com Vertical Slices](./adr/0003-minimal-api-cqrs-vertical-slices.md).
> **Público-alvo:** desenvolvedores backend que vão escrever ou revisar código em `backend/src/`.
> **Objetivo:** ensinar **como** e **por que** o backend é organizado da forma como é, com base na literatura e em exemplos reais do projeto.

---

## 1. Por que este documento existe

A ADR 0003 explica **o que foi decidido** e **por quê**. Este guia complementa a ADR com:

- as **definições da literatura** de cada padrão envolvido (com referências),
- comparações **lado a lado** entre os caminhos rejeitados (MVC clássico, CQRS com MediatR, etc.) e o caminho adotado,
- um **template prático** para implementar uma nova feature,
- as **armadilhas comuns** e como evitá-las.

Se você quer só a regra ("o que fazer"), pule para a §6 — Template prático. Se quer entender por que essa regra existe, leia da §2 em diante.

---

## 2. Conceitos da literatura

### 2.1 MVC — Model-View-Controller

**Origem.** Proposto por Trygve Reenskaug em 1979 no Xerox PARC, originalmente para Smalltalk-76. O objetivo era separar três responsabilidades em interfaces gráficas: o **modelo** (estado e regras), a **view** (apresentação) e o **controller** (input do usuário, despacho para o modelo).

**Adaptação à web.** Frameworks como Ruby on Rails (2005) e ASP.NET MVC (2009) levaram o padrão para a web. Em Web APIs modernas a "view" praticamente desaparece — o controller serializa o modelo diretamente em JSON.

**O que MVC significa hoje em .NET.** Uma classe `XxxController : ControllerBase` com métodos decorados por `[HttpGet]`/`[HttpPost]`, model binding por convenção, filtros (`IActionFilter`, `IAuthorizationFilter`), e um pipeline opinativo que roda em volta de cada ação.

**Referências.**
- Reenskaug, T. — *Models–Views–Controllers*, Xerox PARC technical note, 1979.
- Fowler, M. — *Patterns of Enterprise Application Architecture*, Addison-Wesley, 2003, cap. 14 ("Web Presentation Patterns").

### 2.2 Minimal API

**Origem.** Introduzido no ASP.NET Core 6 (2021) inspirado em estilos minimalistas como Express.js (Node), Flask (Python) e Giraffe (F#). Amadurecido no .NET 7 (filters, typed results) e .NET 8 (form binding, AOT).

**Definição operacional.** Endpoints são **funções**, não métodos de uma classe. A definição vive no startup ou em um arquivo de extensão:

```csharp
app.MapPost("/usuarios", async (CriarUsuarioCommand cmd, ICommandHandler<CriarUsuarioCommand, UsuarioResponse> handler, CancellationToken ct)
    => TypedResults.Created($"/usuarios/{(await handler.HandleAsync(cmd, ct)).Id}"));
```

**O que NÃO é Minimal API.** Não é um framework separado, não é "MVC sem atributos", não é "só para microservices". É um estilo de **bind & dispatch** que coexiste com MVC no mesmo projeto.

**Referências.**
- Microsoft Docs — *Minimal APIs overview*: <https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/overview>.
- Microsoft Docs — *Choose between controller-based APIs and minimal APIs*: <https://learn.microsoft.com/aspnet/core/fundamentals/apis>.

### 2.3 CQRS — Command Query Responsibility Segregation

**Pai do conceito (CQS).** Bertrand Meyer, em *Object-Oriented Software Construction* (1988), formulou a **Command-Query Separation**: todo método é ou um **comando** (muda estado, retorna `void`) ou uma **query** (lê estado, sem efeitos colaterais). Nunca ambos.

**CQRS (Greg Young, 2010).** Estende CQS do nível de método para o nível de **arquitetura**. Comandos e queries têm **modelos, caminhos e responsabilidades separados**. O modelo de leitura pode ser uma projeção otimizada para a UI (ex.: view materializada, denormalização); o de escrita carrega as invariantes do domínio.

**Mitos comuns.**
- "CQRS exige Event Sourcing." — **Falso.** Event sourcing é uma forma de implementar o lado da escrita; CQRS funciona com CRUD ortodoxo.
- "CQRS exige dois bancos." — **Falso.** Mesmo banco, mesmas tabelas, modelos diferentes em código.
- "CQRS exige MediatR/barramento." — **Falso.** MediatR implementa o pattern Mediator (GoF) para despacho; é uma escolha de **transporte interno**, não de CQRS.

**Referências.**
- Meyer, B. — *Object-Oriented Software Construction*, Prentice Hall, 1988 (CQS).
- Young, G. — *CQRS, Task Based UIs, Event Sourcing agh!*, 2010: <https://cqrs.wordpress.com/documents/cqrs-introduction/>.
- Fowler, M. — *CQRS*, 2011: <https://martinfowler.com/bliki/CQRS.html>.

### 2.4 Vertical Slice Architecture

**Origem.** Jimmy Bogard (autor do AutoMapper e do MediatR) cunhou o termo em 2018, em contraste com Clean Architecture clássica. O insight: em Clean Architecture, código relacionado a **uma feature** se espalha por várias camadas (Application Service, DTO, Validator, Mapper, Controller). Mudar a feature exige tocar arquivos em pastas diferentes.

**Definição.** Em Vertical Slice, cada **caso de uso** vira uma fatia vertical da aplicação: command + handler + validator + DTOs + (opcional) endpoint, todos juntos na **mesma pasta**. Slices não compartilham serviços; compartilham apenas o **domínio** (entidades, value objects, regras invariantes) e a **infraestrutura** (repositórios, EF Core, integrações).

**O que isso resolve.**
- **Coesão por mudança.** Implementar uma nova feature ou modificar uma existente toca arquivos próximos.
- **Baixo acoplamento entre features.** Slices não dependem uns dos outros — quebrá-los ou removê-los é local.
- **PRs pequenos e revisáveis.** Um PR de "criar agendamento" mexe em `Agendamentos/Criar/`, não em 8 pastas espalhadas.

**Relação com Clean Architecture.** Vertical Slice **não revoga** Clean Architecture — preserva o isolamento do `Domain` e a inversão de dependências para `Infrastructure`. O que muda é que a camada "Application Services" deixa de ser genérica e vira **uma pasta por caso de uso**.

**Referências.**
- Bogard, J. — *Vertical Slice Architecture*, 2018: <https://www.jimmybogard.com/vertical-slice-architecture/>.
- Khorikov, V. — *Unit Testing Principles, Practices, and Patterns*, Manning, 2020 (cap. 7 — discute trade-offs entre Clean Architecture e abordagens por feature).

### 2.5 Clean Architecture (referência)

**Robert C. Martin (2012).** Síntese das ideias de Hexagonal Architecture (Cockburn, 2005), Onion Architecture (Palermo, 2008) e DDD (Evans, 2003). Premissa: dependências sempre apontam para o domínio; o domínio não conhece banco, HTTP, framework.

**Como o projeto se posiciona.** O CarWash adota o **núcleo** de Clean Architecture (Domain isolado, Infrastructure satisfazendo interfaces da Application) e organiza a Application em **slices verticais** em vez de "Application Services" genéricos.

**Referências.**
- Martin, R. C. — *Clean Architecture: A Craftsman's Guide to Software Structure and Design*, Prentice Hall, 2017.
- Martin, R. C. — *The Clean Architecture*, 2012: <https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html>.

---

## 3. Por que Minimal API + CQRS no CarWash

Esta seção sintetiza a ADR 0003. Para a discussão completa, veja [a ADR](./adr/0003-minimal-api-cqrs-vertical-slices.md). Em resumo:

| Driver | Como Minimal API + CQRS atende |
|---|---|
| Muitos casos de uso pequenos (DRP tem ~25 RFs) | Cada um vira um slice isolado, fácil de testar e revisar |
| Regras de negócio densas (RN011, RN003, RN009) | Handler concentra a regra; sem fat service |
| Rastreabilidade requisito→código (CA001..CA011) | Cada CA mapeia para um command/handler nomeado |
| Time pequeno, sem necessidade de infra de mediator | `ICommandHandler<,>` próprio, resolvido por DI direta |
| Performance e simplicidade | Minimal API evita o pipeline MVC quando não há ganho |
| Evitar dependência opinativa/paga (MediatR) | Hand-rolled `ICommandHandler<,>` em ~20 linhas |

---

## 4. Comparação prática: MVC clássico × Minimal API + CQRS

### 4.1 Mesma feature, dois estilos

**Cenário:** criar usuário (`POST /api/v1/usuarios`).

#### Estilo A — MVC + Service Layer (rejeitado)

```csharp
// Controllers/UsuariosController.cs
[ApiController]
[Route("api/v1/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _service;
    public UsuariosController(IUsuarioService service) => _service = service;

    [HttpPost]
    [ProducesResponseType(typeof(UsuarioResponse), 201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> Criar([FromBody] CriarUsuarioDto dto, CancellationToken ct)
    {
        var resp = await _service.CriarAsync(dto, ct);
        return CreatedAtAction(nameof(ObterPorId), new { id = resp.Id }, resp);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken ct)
    {
        var u = await _service.ObterPorIdAsync(id, ct);
        return u is null ? NotFound() : Ok(u);
    }

    // ... mais 5 ações que crescem com o tempo
}

// Services/UsuarioService.cs — vira um "god service" com o tempo
public class UsuarioService : IUsuarioService
{
    public Task<UsuarioResponse> CriarAsync(CriarUsuarioDto dto, CancellationToken ct) { /* validar, hash, persistir */ }
    public Task<UsuarioResponse?> ObterPorIdAsync(Guid id, CancellationToken ct) { /* ler */ }
    public Task<UsuarioResponse> AlterarAsync(...) { /* atualizar */ }
    public Task DesativarAsync(...) { /* outra escrita */ }
    public Task<PagedResult<UsuarioResponse>> ListarAsync(...) { /* outra leitura */ }
    // ...
}
```

**Problemas.**
- O `UsuarioService` mistura escrita (Criar, Alterar) com leitura (ObterPorId, Listar) — viola CQS.
- Testar a regra de "e-mail duplicado" envolve mockar várias dependências do service inteiro.
- PR pequeno (mudar regra de Criar) mexe em arquivo grande (service com 8 métodos).
- O controller é praticamente um repassador — adiciona indireção sem valor.

#### Estilo B — Minimal API + CQRS + Vertical Slice (adotado)

```csharp
// CarWash.Application/Usuarios/CriarUsuario/CriarUsuarioCommand.cs
public sealed record CriarUsuarioCommand(string Nome, string Email, string Senha, PerfilUsuario Perfil)
    : ICommand<UsuarioResponse>;

// CarWash.Application/Usuarios/CriarUsuario/CriarUsuarioCommandValidator.cs
public sealed class CriarUsuarioCommandValidator : AbstractValidator<CriarUsuarioCommand>
{
    public CriarUsuarioCommandValidator()
    {
        RuleFor(c => c.Nome).NotEmpty().MaximumLength(120);
        RuleFor(c => c.Email).NotEmpty().EmailAddress();
        RuleFor(c => c.Senha).NotEmpty().MinimumLength(8);
    }
}

// CarWash.Application/Usuarios/CriarUsuario/CriarUsuarioHandler.cs
public sealed class CriarUsuarioHandler : ICommandHandler<CriarUsuarioCommand, UsuarioResponse>
{
    private readonly IUsuarioRepository _repo;
    private readonly IPasswordHasher _hasher;
    public CriarUsuarioHandler(IUsuarioRepository repo, IPasswordHasher hasher)
        => (_repo, _hasher) = (repo, hasher);

    public async Task<UsuarioResponse> HandleAsync(CriarUsuarioCommand cmd, CancellationToken ct)
    {
        // pré-check + UK no banco (defesa em duas camadas — ver código real para detalhes)
        if (await _repo.ExisteComEmailAsync(cmd.Email, ct))
            throw new ConflictException("E-mail já cadastrado.", "email-already-exists");

        var usuario = Usuario.Criar(Guid.NewGuid(), cmd.Nome, new Email(cmd.Email), _hasher.Hash(cmd.Senha), cmd.Perfil);
        await _repo.AdicionarAsync(usuario, ct);
        await _repo.SalvarAsync(ct);
        return UsuarioResponse.FromEntity(usuario);
    }
}

// CarWash.Api/Endpoints/Usuarios/UsuariosEndpoints.cs
public static class UsuariosEndpoints
{
    public static IEndpointRouteBuilder MapUsuarios(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/usuarios").WithTags("Usuarios");

        g.MapPost("/", async ([FromBody] CriarUsuarioCommand cmd,
                              [FromServices] ICommandHandler<CriarUsuarioCommand, UsuarioResponse> handler,
                              CancellationToken ct) =>
                TypedResults.Created($"/api/v1/usuarios/{(await handler.HandleAsync(cmd, ct)).Id}"))
            .AddEndpointFilter<ValidationFilter<CriarUsuarioCommand>>()
            .WithName("CriarUsuario")
            .Produces<UsuarioResponse>(201)
            .ProducesProblem(400)
            .ProducesProblem(409);

        return app;
    }
}
```

**Ganhos.**
- O handler tem **uma** responsabilidade (criar usuário). Testá-lo é direto.
- Mudar a regra de criação mexe em `Usuarios/CriarUsuario/` apenas.
- O endpoint mostra exatamente o que faz: bind do command → handler → resposta tipada.
- Cada CA do DRP vira um arquivo de teste rastreável.

### 4.2 Tabela de comparação

| Aspecto | MVC + Service | Minimal API + CQRS + Vertical Slice |
|---|---|---|
| Acoplamento entre operações | Alto (mesmo service) | Nulo (slices independentes) |
| Mapping CA→código | 1 CA → método dentro do service | 1 CA → 1 handler nomeado |
| Tamanho do arquivo HTTP | Cresce sem limite | Lista de `.MapXxx(...)` legível como tabela |
| Boilerplate por feature | Baixo (ação no controller) | Médio (command + validator + handler + endpoint) |
| Performance | Pipeline MVC completo | ~20–30% mais leve em endpoints triviais |
| Filtros transversais | `IActionFilter` (implícito) | `IEndpointFilter` (explícito por endpoint/grupo) |
| Discoverability | Atributos espalhados | Tudo na assinatura do `MapXxx` |
| Curva de aprendizado | Familiar | Maior (exige entender CQRS + slices) |
| Refatorar/remover feature | Mexe em 3+ pastas | Apaga 1 pasta |

---

## 5. Estrutura canônica de pastas

```
backend/src/
├─ CarWash.Domain/                       # entidades, VOs, regras invariantes (sem dependências de framework)
│  ├─ Entities/
│  ├─ ValueObjects/
│  └─ Common/                            # DomainException, GuardClauses
│
├─ CarWash.Application/                  # casos de uso (slices verticais)
│  ├─ Abstractions/
│  │  ├─ Messaging/
│  │  │  ├─ ICommand.cs                  # marker + tipo de resposta
│  │  │  ├─ ICommandHandler.cs
│  │  │  ├─ IQuery.cs
│  │  │  └─ IQueryHandler.cs
│  │  └─ IPasswordHasher.cs, ITimeProvider.cs, ...
│  ├─ Common/                            # exceções genéricas (ValidationException, ConflictException, NotFoundException)
│  ├─ <Agregado>/                        # ex.: Usuarios, Agendamentos, Clientes
│  │  ├─ <CasoDeUso>/                    # ex.: CriarUsuario, AlterarStatus, ObterPorId
│  │  │  ├─ <X>Command.cs    OU   <X>Query.cs
│  │  │  ├─ <X>CommandValidator.cs
│  │  │  └─ <X>Handler.cs
│  │  ├─ Common/                         # DTOs de resposta reutilizáveis no agregado
│  │  └─ Persistence/
│  │     └─ I<Agregado>Repository.cs     # interface; impl em Infrastructure
│  └─ DependencyInjection.cs             # registra handlers por scan
│
├─ CarWash.Infrastructure/               # implementações concretas
│  ├─ Persistence/                       # DbContext, EF configurations, migrations
│  ├─ Repositories/                      # implementação de I<X>Repository
│  ├─ Security/                          # Argon2idPasswordHasher etc.
│  └─ DependencyInjection.cs
│
└─ CarWash.Api/                          # camada HTTP
   ├─ Endpoints/
   │  └─ <Recurso>/<Recurso>Endpoints.cs # classe estática, método Map<X>()
   ├─ Filters/                           # ValidationFilter<T>, etc.
   ├─ Middleware/                        # exception handler, request context, etc.
   ├─ Infrastructure/                    # OpenAPI, ProblemDetails, JSON options
   └─ Program.cs                         # app.MapAuth().MapUsuarios().Map...()
```

**Regras de dependência:**
- `Domain` não depende de ninguém.
- `Application` depende só de `Domain` e de abstrações próprias (`Abstractions/`).
- `Infrastructure` depende de `Application` e `Domain` (implementa interfaces).
- `Api` depende de `Application` e `Infrastructure` (composição).

---

## 6. Template prático — criar uma nova feature

Cenário: implementar `POST /api/v1/agendamentos` (criar agendamento — RF020, RN011).

### Passo 1 — Criar a pasta do slice

```
CarWash.Application/Agendamentos/CriarAgendamento/
  CriarAgendamentoCommand.cs
  CriarAgendamentoCommandValidator.cs
  CriarAgendamentoHandler.cs
```

### Passo 2 — Command (intent imutável)

```csharp
namespace CarWash.Application.Agendamentos.CriarAgendamento;

public sealed record CriarAgendamentoCommand(
    Guid ClienteId,
    Guid VeiculoId,
    Guid ServicoId,
    Guid FilialId,
    DateTime InicioPrevisto)
    : ICommand<AgendamentoResponse>;
```

### Passo 3 — Validator (validação sintática/leve)

```csharp
public sealed class CriarAgendamentoCommandValidator : AbstractValidator<CriarAgendamentoCommand>
{
    public CriarAgendamentoCommandValidator()
    {
        RuleFor(c => c.ClienteId).NotEmpty();
        RuleFor(c => c.VeiculoId).NotEmpty();
        RuleFor(c => c.ServicoId).NotEmpty();
        RuleFor(c => c.FilialId).NotEmpty();
        RuleFor(c => c.InicioPrevisto)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Início previsto deve ser no futuro.");
    }
}
```

### Passo 4 — Handler (regra de negócio)

```csharp
public sealed class CriarAgendamentoHandler : ICommandHandler<CriarAgendamentoCommand, AgendamentoResponse>
{
    private readonly IAgendamentoRepository _repo;
    // ... outros colaboradores

    public async Task<AgendamentoResponse> HandleAsync(CriarAgendamentoCommand cmd, CancellationToken ct)
    {
        // 1. Carregar agregados envolvidos (cliente, veículo, serviço, filial).
        // 2. Construir o Agendamento via Agendamento.Criar(...) — regras invariantes no Domain.
        // 3. Persistir; a EXCLUDE constraint do RN011 protege contra race condition.
        // 4. Retornar DTO.
    }
}
```

### Passo 5 — Endpoint

Edite `CarWash.Api/Endpoints/Agendamentos/AgendamentosEndpoints.cs` (crie se não existir):

```csharp
public static class AgendamentosEndpoints
{
    public static IEndpointRouteBuilder MapAgendamentos(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/agendamentos").WithTags("Agendamentos");

        g.MapPost("/", async (
                [FromBody] CriarAgendamentoCommand cmd,
                [FromServices] ICommandHandler<CriarAgendamentoCommand, AgendamentoResponse> handler,
                CancellationToken ct) =>
            {
                var resp = await handler.HandleAsync(cmd, ct);
                return TypedResults.Created($"/api/v1/agendamentos/{resp.Id}", resp);
            })
            .AddEndpointFilter<ValidationFilter<CriarAgendamentoCommand>>()
            .WithName("CriarAgendamento")
            .Produces<AgendamentoResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);  // conflito RN011

        return app;
    }
}
```

E registre no `Program.cs`:

```csharp
app.MapAuth()
   .MapUsuarios()
   .MapAgendamentos();   // novo
```

### Passo 6 — Testes

```
backend/tests/CarWash.Application.Tests/Agendamentos/CriarAgendamento/
  CriarAgendamentoHandlerTests.cs       # unidade — mocks de repo
backend/tests/CarWash.IntegrationTests/Agendamentos/
  CriarAgendamentoEndpointTests.cs      # WebApplicationFactory + Testcontainers (postgres real)
```

---

## 7. Quando Command vs Query

| Operação | Tipo | Marker |
|---|---|---|
| `POST /agendamentos` | Command | `ICommand<TResponse>` |
| `PATCH /usuarios/{id}/status` | Command | `ICommand<TResponse>` |
| `DELETE /agendamentos/{id}` | Command | `ICommand<TResponse>` (ou `ICommand<Unit>` se sem resposta) |
| `GET /usuarios/{id}` | Query | `IQuery<TResponse>` |
| `GET /agendamentos?data=...` | Query | `IQuery<PagedResult<T>>` |
| `GET /dashboard/metricas` | Query | `IQuery<DashboardResponse>` |

**Regra prática:** se a operação muda estado do sistema (cria, altera, deleta), é Command. Se só lê, é Query. Quando estiver em dúvida — quase sempre é Command.

---

## 8. Armadilhas comuns

### 8.1 "Esse handler chama outro handler"

**Sintoma:** `CriarAgendamentoHandler` injeta `ICommandHandler<EnviarNotificacaoCommand, ...>` e despacha.

**Problema:** acopla slices. Quebra a propriedade de "remover a pasta = remover a feature".

**Solução:**
- Para regras compartilhadas, extrair **serviço de domínio** em `CarWash.Domain/Services/` (sem dependência de framework).
- Para integrações (e-mail, push), declarar uma **interface na Application** (`INotificadorAgendamento`) implementada em `Infrastructure`, e injetar no handler.
- Para reações entre agregados (criar X dispara Y), modelar com **domain events** publicados no fim do handler — outro handler reage via assinante. Só introduzir quando houver real necessidade.

### 8.2 "Esse DTO é igual ao da outra feature"

**Sintoma:** copy/paste de `UsuarioResponse` entre `CriarUsuario` e `ObterUsuarioPorId`.

**Solução:** mover para `Usuarios/Common/UsuarioResponse.cs`. Reuso **dentro do mesmo agregado** é OK. Reuso **entre agregados** é red flag — geralmente significa que falta uma fronteira de domínio bem definida.

### 8.3 "O validator não consegue checar X porque precisa do banco"

**Sintoma:** validator precisa saber se um e-mail já existe.

**Solução:** validator faz só validação **sintática** (formato, tamanho, NotEmpty). Validações que dependem de estado externo (unicidade, existência de FK, regras de negócio) ficam no **handler**. Isso mantém o validator puro e o handler com a regra completa de negócio.

### 8.4 "Tenho um endpoint que precisa de upload multipart pesado"

**Solução:** essa é uma das exceções legítimas onde MVC é mais conveniente (model binding multipart). Adicionar `ControllerBase` para esse endpoint específico **com justificativa em comentário** apontando para esta seção. Default permanece Minimal API.

### 8.5 "O ValidationFilter não funciona porque meu command vem da rota + body"

**Sintoma:** endpoint `PATCH /usuarios/{id}/status` recebe `id` da rota e `{ ativo: bool }` do body. O `ValidationFilter<TCommand>` espera o command pronto nos arguments.

**Solução:** validar inline (chamar `IValidator<T>` diretamente no handler do endpoint). Padrão já implementado em `UsuariosEndpoints.AlterarStatusAsync`. Se isso virar comum, criar um helper.

---

## 9. Mapeamento requisito → código

Esta convenção facilita rastreabilidade entre o DRP/DVP-E e o código.

| Artefato no DRP | Artefato no código |
|---|---|
| RF (Requisito Funcional) | Pasta de agregado em `CarWash.Application/<Agregado>/` |
| Caso de uso / CA | Pasta de slice em `CarWash.Application/<Agregado>/<CasoDeUso>/` |
| Comando do CA | `<CasoDeUso>Command.cs` |
| Resposta do CA | `<CasoDeUso>Response.cs` (ou em `Common/`) |
| Regra de negócio (RN) | Método no agregado em `CarWash.Domain/Entities/` |
| Restrição de banco (UK, FK, EXCLUDE) | EF Core configuration em `CarWash.Infrastructure/Persistence/Configurations/` |
| Endpoint REST | Método `Map<X>` em `CarWash.Api/Endpoints/<Recurso>/<Recurso>Endpoints.cs` |
| Critério de aceite (CA001..CA011) | Test fact em `tests/.../HandlerTests` + integration test |

---

## 10. Referências consolidadas

### Padrões e teoria

- **Meyer, B.** — *Object-Oriented Software Construction*, 2ª ed., Prentice Hall, 1997. (CQS)
- **Reenskaug, T.** — *Models–Views–Controllers*, Xerox PARC technical note, 1979.
- **Fowler, M.** — *Patterns of Enterprise Application Architecture*, Addison-Wesley, 2003.
- **Fowler, M.** — *CQRS* (bliki), 2011: <https://martinfowler.com/bliki/CQRS.html>.
- **Young, G.** — *CQRS Documents*, 2010: <https://cqrs.wordpress.com/documents/cqrs-introduction/>.
- **Evans, E.** — *Domain-Driven Design: Tackling Complexity in the Heart of Software*, Addison-Wesley, 2003.
- **Cockburn, A.** — *Hexagonal Architecture*, 2005: <https://alistair.cockburn.us/hexagonal-architecture/>.
- **Palermo, J.** — *The Onion Architecture*, 2008.
- **Martin, R. C.** — *Clean Architecture*, Prentice Hall, 2017.
- **Bogard, J.** — *Vertical Slice Architecture*, 2018: <https://www.jimmybogard.com/vertical-slice-architecture/>.
- **Gamma, Helm, Johnson, Vlissides** — *Design Patterns: Elements of Reusable Object-Oriented Software*, Addison-Wesley, 1994. (Mediator)
- **Khorikov, V.** — *Unit Testing Principles, Practices, and Patterns*, Manning, 2020.

### Documentação .NET

- *Minimal APIs overview*: <https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/overview>
- *Choose between controller-based APIs and minimal APIs*: <https://learn.microsoft.com/aspnet/core/fundamentals/apis>
- *Filters in Minimal API apps*: <https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/min-api-filters>
- *Endpoint routing*: <https://learn.microsoft.com/aspnet/core/fundamentals/routing>
- *TypedResults class*: <https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/responses>

### Decisões internas

- [ADR 0001 — UUID gerado pela aplicação](./adr/0001-geracao-de-uuid-pela-aplicacao.md)
- [ADR 0002 — Hash de senha com Argon2id](./adr/0002-hash-de-senha-com-argon2id.md)
- [ADR 0003 — Minimal API + CQRS com Vertical Slices](./adr/0003-minimal-api-cqrs-vertical-slices.md)
- [DAT — Documento de Arquitetura Técnica](./dat%20-%20Documento%20de%20Arquitetura%20T%C3%A9cnica.md)
