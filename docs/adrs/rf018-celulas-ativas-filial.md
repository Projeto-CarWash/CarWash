# Desenho técnico — RF018 Configuração de células ativas por filial (com integração RF008)

- Branch: `feat/rf018-celulas-ativas-filial` (base: `main`).
- Autor do desenho: Arquiteto de Software Sênior (CarWash).
- Status: pronto para implementação.
- Referências canônicas (padrão a seguir): `Application/Usuarios/AlterarStatus/`, `Application/Usuarios/AlterarUsuario/`, `Application/Usuarios/CriarUsuario/`, `Application/Servicos/Criar/`, `Application/Servicos/Persistence/IServicoRepository.cs`, `Api/Endpoints/Usuarios/UsuariosEndpoints.cs`, `Api/Endpoints/EndpointRouteBuilderExtensions.cs`, `Api/Filters/ValidationFilter*`, `Api/Filters/ValidationProblems.cs`, `Api/Middleware/ExceptionHandlingMiddleware.cs`, `Infrastructure/Persistence/Repositories/UsuarioRepository.cs`, `Infrastructure/DependencyInjection.cs`, `Application/Abstractions/IAuditLogger.cs` e `ICurrentRequestContext.cs`, `Infrastructure/Persistence/Interceptors/AuditLogInterceptor.cs`.
- DAT/DRP referenciados: DAT §4.1 (módulo Filiais), DAT §4.2 (RN no backend), DAT §3.1 (camadas), DRP RF008/RF017/RF018, DRP RN009/RN010/RN011, DRP CA006/CA011.

> Observação importante sobre o estado atual: o domínio `Filial` (Domain/Entities/Filial.cs) e a configuração EF (`Infrastructure/Persistence/Configurations/FilialConfiguration.cs`) **já estão prontos** (factory `Criar`, mutator `AjustarCelulas`, check constraint `ck_filiais_celulas_faixa BETWEEN 1 AND 100`, UK `uk_filiais_nome`). O `Filial` já consta na lista de entidades auditáveis em `AuditLogInterceptor` — INSERT e UPDATE serão gravados em `audit_logs` automaticamente desde que o handler defina `ICurrentRequestContext.DefinirEvento(...)` antes do `SaveChanges`. Não vamos tocar nem o domínio nem o schema/migration nesta entrega.

---

## 1. Estrutura de pastas exata (novos arquivos)

Todos os caminhos abaixo são absolutos e seguem o padrão de vertical slice já adotado em `Application/Usuarios/...` e o padrão de endpoints `Api/Endpoints/<Recurso>/<Recurso>Endpoints.cs`.

### 1.1 Application — slices, persistência e DTOs comuns

```
/home/gbrogio/university/carwash/backend/src/CarWash.Application/Filiais/
├── Common/
│   └── FilialResponse.cs                                       (DTO de saída + factory FromEntity)
├── Persistence/
│   └── IFilialRepository.cs                                    (porta EF, escrita + leitura)
├── CriarFilial/
│   ├── CriarFilialRequest.cs                                   (DTO body POST)
│   ├── CriarFilialCommand.cs                                   (ICommand<FilialResponse>)
│   ├── CriarFilialCommandValidator.cs                          (FluentValidation)
│   └── CriarFilialHandler.cs                                   (ICommandHandler<,>)
├── AlterarCelulasAtivas/
│   ├── AlterarCelulasAtivasRequest.cs                          (DTO body PATCH — só celulasAtivas)
│   ├── AlterarCelulasAtivasCommand.cs                          (ICommand<FilialResponse>)
│   ├── AlterarCelulasAtivasCommandValidator.cs                 (FluentValidation, faixa 1..100)
│   └── AlterarCelulasAtivasHandler.cs                          (ICommandHandler<,>)
└── ObterFilialPorId/
    ├── ObterFilialPorIdQuery.cs                                (IQuery<FilialResponse>)
    └── ObterFilialPorIdHandler.cs                              (IQueryHandler<,>)
```

### 1.2 Infrastructure — repositório concreto

```
/home/gbrogio/university/carwash/backend/src/CarWash.Infrastructure/Persistence/Repositories/
└── FilialRepository.cs                                         (implementa IFilialRepository sobre CarWashDbContext)
```

### 1.3 API — endpoints e policy de autorização

```
/home/gbrogio/university/carwash/backend/src/CarWash.Api/Endpoints/Filiais/
└── FiliaisEndpoints.cs                                         (MapFiliais + 3 handlers de endpoint)

/home/gbrogio/university/carwash/backend/src/CarWash.Api/Extensions/
└── AuthorizationPoliciesExtensions.cs                          (AddCarWashAuthorization + policy "Admin")

/home/gbrogio/university/carwash/backend/src/CarWash.Api/Middleware/
└── (sem novos arquivos — só editar ExceptionHandlingMiddleware.cs)
```

### 1.4 Integração RF008 — sem pasta nova; arquivos a editar

Os artefatos do RF008 ficam dentro do slice de Agendamentos já existente (não invento abstração nova):
- editar `Application/Agendamentos/Persistence/IAgendamentoCatalogoRepository.cs` para adicionar `ContarSobreposicoesNaFilialAsync` e `ObterCelulasAtivasAsync` (este último também usado por validações futuras).
- editar `Infrastructure/Persistence/Repositories/AgendamentoCatalogoRepository.cs` para implementar a contagem.
- editar `Application/Agendamentos/Common/CalculadoraResumoAgendamento.cs` para invocar a verificação de capacidade.
- criar `Application/Agendamentos/Common/CapacidadeFilialEsgotadaException.cs`.

---

## 2. Contratos exatos (records C#)

Todos os DTOs são `sealed record` para alinhar ao padrão dos demais slices (`AlterarStatusUsuarioRequest`, `UsuarioResponse`, `CriarServicoRequest` etc.). Tipos primitivos: `Guid` para ids, `int` (NÃO `int?`) onde a presença é obrigatória e o validator vai exigir intervalo — exceto quando o card pede distinguir "ausente" de "valor zero/false", caso em que o tipo é nullable e o validator usa `NotNull()` para forçar presença explícita (padrão `AlterarStatusUsuarioCommand` / BUG-U004).

### 2.1 `FilialResponse` (DTO de saída) — `Application/Filiais/Common/FilialResponse.cs`

```csharp
using CarWash.Domain.Entities;

namespace CarWash.Application.Filiais.Common;

/// <summary>
/// DTO de saída da Filial (RF017/RF018). Reaproveitado por POST (201),
/// PATCH /celulas-ativas (200) e GET /{id} (200).
/// </summary>
public sealed record FilialResponse(
    Guid Id,
    string Nome,
    int CelulasAtivas,
    string Timezone,
    bool Ativa,
    DateTime CriadoEm,
    DateTime AtualizadoEm)
{
    public static FilialResponse FromEntity(Filial filial)
    {
        ArgumentNullException.ThrowIfNull(filial);
        return new FilialResponse(
            filial.Id,
            filial.Nome,
            filial.CelulasAtivas,
            filial.Timezone,
            filial.Ativa,
            filial.CriadoEm,
            filial.AtualizadoEm);
    }
}
```

### 2.2 Criar filial

`Application/Filiais/CriarFilial/CriarFilialRequest.cs`:

```csharp
namespace CarWash.Application.Filiais.CriarFilial;

/// <summary>
/// DTO de entrada do POST /api/v1/filiais. `celulasAtivas` é nullable para distinguir
/// "ausente no body" de `0` — o validator exige NotNull (mesmo padrão do
/// AlterarStatusUsuarioRequest, BUG-U004).
/// </summary>
public sealed record CriarFilialRequest(string? Nome, int? CelulasAtivas, string? Timezone);
```

`Application/Filiais/CriarFilial/CriarFilialCommand.cs`:

```csharp
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Filiais.Common;

namespace CarWash.Application.Filiais.CriarFilial;

public sealed record CriarFilialCommand(string? Nome, int? CelulasAtivas, string? Timezone)
    : ICommand<FilialResponse>;
```

Não há `CriarFilialResponse` próprio — reaproveitamos `FilialResponse` (mesmo padrão de `AlterarStatusUsuarioResponse` é dispensado aqui porque o POST devolve a entidade completa, como `UsuarioResponse` no `CriarUsuarioHandler`).

### 2.3 Alterar células ativas

`Application/Filiais/AlterarCelulasAtivas/AlterarCelulasAtivasRequest.cs`:

```csharp
namespace CarWash.Application.Filiais.AlterarCelulasAtivas;

/// <summary>
/// DTO de entrada do PATCH /api/v1/filiais/{id}/celulas-ativas. `celulasAtivas`
/// é nullable para diferenciar "ausente" de `0` — o validator exige NotNull para
/// rejeitar body vazio `{}` com mensagem específica do card (faixa 1..100).
/// </summary>
public sealed record AlterarCelulasAtivasRequest(int? CelulasAtivas);
```

`Application/Filiais/AlterarCelulasAtivas/AlterarCelulasAtivasCommand.cs`:

```csharp
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Filiais.Common;

namespace CarWash.Application.Filiais.AlterarCelulasAtivas;

public sealed record AlterarCelulasAtivasCommand(Guid FilialId, int? CelulasAtivas)
    : ICommand<FilialResponse>;
```

### 2.4 Obter filial por id

`Application/Filiais/ObterFilialPorId/ObterFilialPorIdQuery.cs`:

```csharp
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Filiais.Common;

namespace CarWash.Application.Filiais.ObterFilialPorId;

public sealed record ObterFilialPorIdQuery(Guid Id) : IQuery<FilialResponse>;
```

---

## 3. `IFilialRepository` (porta de persistência)

Caminho: `Application/Filiais/Persistence/IFilialRepository.cs`.

```csharp
using CarWash.Domain.Entities;

namespace CarWash.Application.Filiais.Persistence;

/// <summary>
/// Porta de persistência da aggregate <see cref="Filial"/>. A implementação concreta
/// vive em CarWash.Infrastructure. Mantém a Application desacoplada do EF Core.
/// Espelha o contrato de IUsuarioRepository.
/// </summary>
public interface IFilialRepository
{
    /// <summary>Read-only por id (AsNoTracking). Null se não existir.</summary>
    Task<Filial?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Por id com tracking — uso obrigatório quando a Use Case vai mutar antes de SalvarAsync.</summary>
    Task<Filial?> ObterPorIdRastreadoAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Verifica colisão de nome (UK `uk_filiais_nome`). Case-insensitive ILIKE.</summary>
    Task<bool> ExisteComNomeAsync(string nome, CancellationToken cancellationToken);

    /// <summary>Adiciona o aggregate à unidade de trabalho — não persiste.</summary>
    Task AdicionarAsync(Filial filial, CancellationToken cancellationToken);

    /// <summary>
    /// Persiste mudanças. Traduz <see cref="Microsoft.EntityFrameworkCore.DbUpdateException"/>
    /// que viole `uk_filiais_nome` em <see cref="Common.NomeFilialJaExisteException"/> (409).
    /// </summary>
    Task SalvarAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Projeção mínima usada pela validação de capacidade do RF008. Retorna o
    /// valor de <c>CelulasAtivas</c> da filial ou <c>null</c> se não existir.
    /// AsNoTracking. Mantém o contrato fora do `IAgendamentoCatalogoRepository`
    /// para evitar duplicação de responsabilidade entre slices.
    /// </summary>
    Task<int?> ObterCelulasAtivasAsync(Guid filialId, CancellationToken cancellationToken);
}
```

> Observação: a contagem de sobreposições (RF008) NÃO entra neste repositório. Ela pertence semanticamente ao slice de Agendamentos (consulta a `agendamentos`), então fica em `IAgendamentoCatalogoRepository`. Manter responsabilidades separadas evita um repositório de Filial saber a tabela de agendamentos.

### 3.1 Exceção de conflito específica do POST

`Application/Filiais/Common/NomeFilialJaExisteException.cs`:

```csharp
using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Filiais.Common;

public sealed class NomeFilialJaExisteException : ConflictException
{
    public const string MensagemPadrao = "Já existe uma filial cadastrada com este nome.";
    public const string SlugPadrao = "filial-nome-duplicado";

    public NomeFilialJaExisteException() : base(MensagemPadrao, SlugPadrao) { }
    public NomeFilialJaExisteException(Exception inner) : base(MensagemPadrao, SlugPadrao, inner) { }
}
```

(Mesmo padrão de `EmailJaExisteException`.)

---

## 4. `FilialRepository` (Infrastructure) — esqueleto + DI

Caminho: `Infrastructure/Persistence/Repositories/FilialRepository.cs`.

Assinatura inicial — espelha `UsuarioRepository` (intercepção de `DbUpdateException` pelo SQLSTATE `23505` + nome da UK):

```csharp
public sealed class FilialRepository : IFilialRepository
{
    private const string ConstraintNomeUnico = "uk_filiais_nome";
    private const string PostgresUniqueViolationSqlState = "23505";

    private readonly CarWashDbContext _db;

    public FilialRepository(CarWashDbContext db) => _db = db;

    public Task<Filial?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        _db.Filiais.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct);

    public Task<Filial?> ObterPorIdRastreadoAsync(Guid id, CancellationToken ct) =>
        _db.Filiais.FirstOrDefaultAsync(f => f.Id == id, ct);

    public Task<bool> ExisteComNomeAsync(string nome, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        var alvo = nome.Trim();
        // ILIKE para case-insensitive — alinhado ao comportamento esperado da UK.
        return _db.Filiais.AsNoTracking().AnyAsync(f => EF.Functions.ILike(f.Nome, alvo), ct);
    }

    public Task AdicionarAsync(Filial filial, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(filial);
        _db.Filiais.Add(filial);
        return Task.CompletedTask;
    }

    public async Task SalvarAsync(CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsNomeUniqueViolation(ex))
        {
            throw new NomeFilialJaExisteException(ex);
        }
    }

    public Task<int?> ObterCelulasAtivasAsync(Guid filialId, CancellationToken ct) =>
        _db.Filiais.AsNoTracking()
            .Where(f => f.Id == filialId)
            .Select(f => (int?)f.CelulasAtivas)
            .FirstOrDefaultAsync(ct);

    private static bool IsNomeUniqueViolation(DbUpdateException ex)
    {
        if (ex.InnerException is not Npgsql.PostgresException pg) return false;
        return pg.SqlState == PostgresUniqueViolationSqlState
            && pg.ConstraintName is not null
            && pg.ConstraintName.Contains(ConstraintNomeUnico, StringComparison.OrdinalIgnoreCase);
    }
}
```

### Registro no DI

Editar `Infrastructure/DependencyInjection.cs` e acrescentar, junto aos demais `services.AddScoped<I...Repository, ...Repository>();`:

```csharp
services.AddScoped<IFilialRepository, FilialRepository>();
```

Os handlers de `Application` são descobertos automaticamente pelo scan de `RegistrarHandlers` em `Application/DependencyInjection.cs` — nada a fazer ali. O `ValidationFilter<>` é registrado também por scan (`AddValidationFilters`) — nada a fazer.

---

## 5. Validators (FluentValidation)

Mensagens em português conforme o card — em particular a string EXATA "Valor de células ativas inválido. Informe um número inteiro entre 1 e 100." na faixa.

### 5.1 `CriarFilialCommandValidator`

```csharp
public sealed class CriarFilialCommandValidator : AbstractValidator<CriarFilialCommand>
{
    public const string MensagemFaixa =
        "Valor de células ativas inválido. Informe um número inteiro entre 1 e 100.";
    public const string MensagemNomeObrigatorio = "Nome da filial é obrigatório.";
    public const string MensagemNomeMaximo = "Nome da filial excede 120 caracteres.";
    public const string MensagemCelulasObrigatorio = "Campo 'celulasAtivas' é obrigatório.";

    public CriarFilialCommandValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage(MensagemNomeObrigatorio)
            .MaximumLength(120).WithMessage(MensagemNomeMaximo);

        RuleFor(x => x.CelulasAtivas)
            .NotNull().WithMessage(MensagemCelulasObrigatorio)
            .Must(v => v is >= Filial.MinCelulasAtivas and <= Filial.MaxCelulasAtivas)
                .WithMessage(MensagemFaixa);

        RuleFor(x => x.Timezone)
            .MaximumLength(64).When(x => !string.IsNullOrWhiteSpace(x.Timezone))
            .WithMessage("Timezone excede 64 caracteres.");
    }
}
```

### 5.2 `AlterarCelulasAtivasCommandValidator`

```csharp
public sealed class AlterarCelulasAtivasCommandValidator : AbstractValidator<AlterarCelulasAtivasCommand>
{
    public const string MensagemFaixa =
        "Valor de células ativas inválido. Informe um número inteiro entre 1 e 100.";
    public const string MensagemFilialIdInvalido = "Identificador da filial é obrigatório.";
    public const string MensagemCelulasObrigatorio = "Campo 'celulasAtivas' é obrigatório.";

    public AlterarCelulasAtivasCommandValidator()
    {
        RuleFor(x => x.FilialId)
            .NotEqual(Guid.Empty).WithMessage(MensagemFilialIdInvalido);

        RuleFor(x => x.CelulasAtivas)
            .NotNull().WithMessage(MensagemCelulasObrigatorio)
            .Must(v => v is >= Filial.MinCelulasAtivas and <= Filial.MaxCelulasAtivas)
                .WithMessage(MensagemFaixa);
    }
}
```

> Sobre rejeição de `null`, `decimal`, `string`, `boolean`, `array`: isso é responsabilidade do `System.Text.Json` (deserializer) — qualquer payload com tipo não numérico cai em `BadHttpRequestException` e é traduzido pelo `ExceptionHandlingMiddleware.ClassificarBadRequest` para 400 com `errors.celulasAtivas`. O ValidationFilter cobre `null` (campo ausente) e fora de faixa.

---

## 6. Handlers

### 6.1 `CriarFilialHandler`

Sequência:

1. `ArgumentNullException.ThrowIfNull(command)`.
2. `_ctx.DefinirEvento("FilialCriada")` para que o `AuditLogInterceptor` capture o INSERT na mesma transação do `SaveChanges`.
3. Pré-check de dedup: `await _repo.ExisteComNomeAsync(command.Nome!.Trim(), ct)` → se true, lança `NomeFilialJaExisteException()` (409). Camada 1 (mensagem amigável).
4. `var filial = Filial.Criar(Guid.NewGuid(), command.Nome!.Trim(), command.CelulasAtivas!.Value, command.Timezone);` — domínio reforça invariantes; em particular `DomainException` por faixa nunca dispara aqui porque o validator já cobriu.
5. `await _repo.AdicionarAsync(filial, ct);`
6. `await _repo.SalvarAsync(ct);` — camada 2: UK `uk_filiais_nome` (e CHECK `ck_filiais_celulas_faixa` no banco) fazem a defesa final. O repositório traduz `DbUpdateException` de UK em `NomeFilialJaExisteException`.
7. `await _audit.LogAsync("FilialCriada", "Filial", filial.Id, new { filial.Nome, filial.CelulasAtivas }, ct)` — registro adicional explícito do evento (além do snapshot do interceptor) para alinhar ao critério do card "valorAnterior=null / valorNovo".
8. `_log.LogInformation("Filial {FilialId} criada com {CelulasAtivas} células.", filial.Id, filial.CelulasAtivas);`
9. `return FilialResponse.FromEntity(filial);`

Constantes públicas no handler:
```csharp
public const string EventoAuditoria = "FilialCriada";
public const string EntidadeAuditoria = "Filial";
```

### 6.2 `AlterarCelulasAtivasHandler`

Sequência:

1. `ArgumentNullException.ThrowIfNull(command)`.
2. `var filial = await _repo.ObterPorIdRastreadoAsync(command.FilialId, ct)` — se `null`, `throw new NotFoundException("Filial não encontrada.")` → 404 com mensagem exata do card.
3. **Snapshot de auditoria** (antes do mutator): `var valorAnterior = filial.CelulasAtivas;`
4. **Idempotência**: se `valorAnterior == command.CelulasAtivas!.Value`, retorna `FilialResponse.FromEntity(filial)` sem salvar e sem auditar (mesmo padrão `AlterarStatusUsuarioHandler`).
5. `filial.AjustarCelulas(command.CelulasAtivas!.Value);` — domínio reforça faixa (DomainException → 400 via middleware), mas validator já cobre antes.
6. `_ctx.DefinirEvento("FilialCelulasAlteradas")` — para o interceptor diff capturar `celulas_ativas: before/after`.
7. `await _repo.SalvarAsync(ct);` — CHECK `ck_filiais_celulas_faixa` é defesa final.
8. `await _audit.LogAsync("FilialCelulasAlteradas", "Filial", filial.Id, new { valorAnterior, valorNovo = filial.CelulasAtivas }, ct)` — atende o critério explícito do card "auditoria com valorAnterior/valorNovo".
9. `_log.LogInformation("Células ajustadas. FilialId={FilialId}, De={De}, Para={Para}", filial.Id, valorAnterior, filial.CelulasAtivas);`
10. `return FilialResponse.FromEntity(filial);`

Constantes:
```csharp
public const string EventoAuditoria = "FilialCelulasAlteradas";
public const string EntidadeAuditoria = "Filial";
public const string MensagemNaoEncontrado = "Filial não encontrada.";
```

### 6.3 `ObterFilialPorIdHandler`

Sequência:

1. `var filial = await _repo.ObterPorIdAsync(query.Id, ct)` (AsNoTracking).
2. Se `null`, `throw new NotFoundException("Filial não encontrada.");`
3. `return FilialResponse.FromEntity(filial);`

Sem auditoria (leitura).

---

## 7. Endpoints

Caminho: `Api/Endpoints/Filiais/FiliaisEndpoints.cs`.

```csharp
namespace CarWash.Api.Endpoints.Filiais;

public static class FiliaisEndpoints
{
    private const string MensagemPayloadInvalido =
        "Dados da filial inválidos. Verifique os campos e tente novamente.";

    public static IEndpointRouteBuilder MapFiliais(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var grupo = app.MapGroup("/api/v1/filiais")
            .WithTags("Filiais")
            .RequireAuthorization(); // GET: autenticado simples (Q1 do refinamento).

        // POST /api/v1/filiais — Admin (Q1).
        grupo.MapPost("/", CriarAsync)
            .RequireAuthorization("Admin")
            .AddEndpointFilter<ValidationFilter<CriarFilialCommand>>()
            .WithName("CriarFilial")
            .Produces<FilialResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // GET /api/v1/filiais/{id} — autenticado simples.
        grupo.MapGet("/{id:guid}", ObterPorIdAsync)
            .WithName("ObterFilialPorId")
            .Produces<FilialResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // PATCH /api/v1/filiais/{id}/celulas-ativas — Admin (Q1).
        grupo.MapPatch("/{id:guid}/celulas-ativas", AlterarCelulasAtivasAsync)
            .RequireAuthorization("Admin")
            .WithName("AlterarCelulasAtivasFilial")
            .Produces<FilialResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<Created<FilialResponse>> CriarAsync(
        [FromBody] CriarFilialCommand command,
        [FromServices] ICommandHandler<CriarFilialCommand, FilialResponse> handler,
        CancellationToken ct)
    {
        var resposta = await handler.HandleAsync(command, ct).ConfigureAwait(false);
        return TypedResults.Created($"/api/v1/filiais/{resposta.Id}", resposta);
    }

    private static async Task<Ok<FilialResponse>> ObterPorIdAsync(
        Guid id,
        [FromServices] IQueryHandler<ObterFilialPorIdQuery, FilialResponse> handler,
        CancellationToken ct)
    {
        var resposta = await handler.HandleAsync(new ObterFilialPorIdQuery(id), ct).ConfigureAwait(false);
        return TypedResults.Ok(resposta);
    }

    private static async Task<Ok<FilialResponse>> AlterarCelulasAtivasAsync(
        Guid id,
        [FromBody] AlterarCelulasAtivasRequest? request,
        [FromServices] ICommandHandler<AlterarCelulasAtivasCommand, FilialResponse> handler,
        [FromServices] IValidator<AlterarCelulasAtivasCommand> validator,
        CancellationToken ct)
    {
        if (request is null)
        {
            throw ValidationProblems.BodyAusente(MensagemPayloadInvalido,
                "Corpo da requisição ausente ou malformado.");
        }

        var command = new AlterarCelulasAtivasCommand(id, request.CelulasAtivas);
        var resultado = await validator.ValidateAsync(command, ct).ConfigureAwait(false);
        ValidationProblems.EnsureValid(resultado, MensagemPayloadInvalido);

        var resposta = await handler.HandleAsync(command, ct).ConfigureAwait(false);
        return TypedResults.Ok(resposta);
    }
}
```

### 7.1 Registro central — editar `EndpointRouteBuilderExtensions.cs`

Adicionar `using CarWash.Api.Endpoints.Filiais;` e a chamada `app.MapFiliais();` no `MapCarWashEndpoints`.

---

## 8. Policy de autorização "Admin"

### 8.1 Estado atual constatado

- `Program.cs`:
  - `RoleClaimType = "perfil"` no `TokenValidationParameters`.
  - `builder.Services.AddAuthorization();` (sem policies além das defaults).
- `JwtAccessTokenService.Emitir` emite `new Claim("perfil", usuario.Perfil.ToString())` — valor PascalCase `"Admin"` ou `"Funcionario"` (NÃO o `ToDbValue` "ADMIN").

### 8.2 Decisão

Como o `RoleClaimType` já é `"perfil"`, `RequireRole("Admin")` funciona out of the box. Mas o card pede texto EXATO no 403 ("Você não possui permissão para alterar configuração da filial.") — `RequireRole` por si só devolve 403 sem corpo. Vamos resolver com **dois passos complementares**:

1. **Criar a policy nominal `"Admin"`** (clarifica intenção, futuro RBAC e telemetria — RT5 do DAT). Mesma semântica que `RequireRole("Admin")`.
2. **Customizar `JwtBearer.OnChallenge` e `JwtBearer.OnForbidden`** para escrever ProblemDetails com as mensagens exatas do card (401 e 403). Isso elimina a divergência de mensagem entre o middleware do JwtBearer e o restante do contrato HTTP do CarWash.

Arquivo novo `Api/Extensions/AuthorizationPoliciesExtensions.cs`:

```csharp
public static class AuthorizationPoliciesExtensions
{
    public const string AdminPolicy = "Admin";

    public static IServiceCollection AddCarWashAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(opt =>
        {
            opt.AddPolicy(AdminPolicy, p =>
            {
                p.RequireAuthenticatedUser();
                p.RequireRole("Admin"); // claim "perfil" == "Admin" (PascalCase)
            });
        });
        return services;
    }
}
```

Editar `Program.cs`: trocar `builder.Services.AddAuthorization();` por `builder.Services.AddCarWashAuthorization();`.

E para uniformizar as mensagens 401/403, **editar a configuração `AddJwtBearer` em `Program.cs`** adicionando `options.Events`:

```csharp
options.Events = new JwtBearerEvents
{
    OnChallenge = async ctx =>
    {
        // Bypass do comportamento default — escrevemos o corpo nós mesmos.
        ctx.HandleResponse();
        if (ctx.Response.HasStarted) return;

        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        ctx.Response.ContentType = "application/problem+json";
        var correlationId = ctx.HttpContext.Items[CorrelationIdMiddleware.ItemKey] as string
            ?? Guid.NewGuid().ToString("N");
        var problem = new ProblemDetails
        {
            Type = "https://carwash/errors/auth-required",
            Title = "Autenticação obrigatória para executar esta operação.",
            Status = StatusCodes.Status401Unauthorized,
        };
        problem.Extensions["correlationId"] = correlationId;
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(problem));
    },
    OnForbidden = async ctx =>
    {
        if (ctx.Response.HasStarted) return;
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        ctx.Response.ContentType = "application/problem+json";
        var correlationId = ctx.HttpContext.Items[CorrelationIdMiddleware.ItemKey] as string
            ?? Guid.NewGuid().ToString("N");
        var problem = new ProblemDetails
        {
            Type = "https://carwash/errors/forbidden",
            Title = "Você não possui permissão para alterar configuração da filial.",
            Status = StatusCodes.Status403Forbidden,
        };
        problem.Extensions["correlationId"] = correlationId;
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(problem));
    },
};
```

> Risco endereçado: a mensagem 403 acima é específica do RF018. Como o CarWash hoje só tem essa rota Admin-only no MVP, a mensagem cabe. **Quando aparecer a segunda rota Admin-only (próximo card)**, o handler `OnForbidden` deve passar a inspecionar o path da requisição para devolver mensagem por recurso, ou — preferencialmente — converter o 403 para um ProblemDetails com `title` genérico "Acesso negado." e um campo extra `slug` indicando o recurso. Documentado aqui para a próxima evolução.

---

## 9. Integração RF008 — validação de capacidade

### 9.1 Onde plugar a contagem

A defesa server-side da capacidade entra no fluxo de **criação** e **confirmação** de agendamento, no momento em que o serviço de domínio `CalculadoraResumoAgendamento` já validou filial/veículo/cliente/serviços. É o ponto único que ambos os fluxos compartilham — evita duplicação entre `CriarAgendamentoHandler` e `ConfirmarAgendamentoHandler`.

Concretamente, **dentro de `CalculadoraResumoAgendamento.CalcularAsync`**, após `GarantirFilialAsync(...)` e antes do cálculo do hash, acrescentar:

```csharp
await GarantirCapacidadeFilialAsync(filialId, inicioUtc, fim, cancellationToken).ConfigureAwait(false);
```

Implementação (novo método privado):

```csharp
private async Task GarantirCapacidadeFilialAsync(
    Guid filialId,
    DateTime inicioUtc,
    DateTime fimUtc,
    CancellationToken cancellationToken)
{
    // RN009 — celulas_ativas é o teto de agendamentos simultâneos de uma filial
    // (RF008). Estratégia best-effort no MVP: contamos sobreposições com status
    // 'agendado' na mesma filial e na mesma janela [inicio, fim) e comparamos com
    // celulas_ativas. A race condition residual entre o pré-check e o INSERT é
    // aceita no MVP — o impacto é, no pior caso, uma simultaneidade igual a
    // celulas_ativas + 1 em poucas ms (sem perda de dado, sem corrupção). A
    // versão "hard" (constraint EXCLUDE com count) está mapeada como evolução
    // pós-MVP. Mais detalhes em /docs/adrs/rf018-celulas-ativas-filial.md §9.4.

    var celulasAtivas = await _catalogo
        .ObterCelulasAtivasFilialAsync(filialId, cancellationToken)
        .ConfigureAwait(false);

    if (celulasAtivas is null or 0)
    {
        // 0 não deveria existir (CHECK 1..100) — defesa em profundidade.
        throw new CapacidadeFilialEsgotadaException();
    }

    var simultaneos = await _catalogo
        .ContarSobreposicoesNaFilialAsync(filialId, inicioUtc, fimUtc, cancellationToken)
        .ConfigureAwait(false);

    if (simultaneos >= celulasAtivas.Value)
    {
        throw new CapacidadeFilialEsgotadaException();
    }
}
```

### 9.2 Nova exceção

Caminho: `Application/Agendamentos/Common/CapacidadeFilialEsgotadaException.cs`.

```csharp
using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Agendamentos.Common;

/// <summary>
/// RF008: a filial atingiu o teto de simultâneos (celulas_ativas, RN009) na janela
/// solicitada. Herda de ConflictException → 409 + slug "capacidade-filial-esgotada"
/// via ExceptionHandlingMiddleware (caminho genérico ConflictException já existente).
/// </summary>
public sealed class CapacidadeFilialEsgotadaException : ConflictException
{
    public const string MensagemPadrao =
        "Capacidade da filial esgotada para o horário solicitado.";
    public const string SlugPadrao = "capacidade-filial-esgotada";

    public CapacidadeFilialEsgotadaException() : base(MensagemPadrao, SlugPadrao) { }
}
```

> **Importante**: como `ConflictException` já é tratado pelo `catch (ConflictException ex)` no `ExceptionHandlingMiddleware` e devolve 409 com o `slug` da exceção, **não é necessário** adicionar um novo `catch` no middleware — herdar de `ConflictException` é suficiente. Isso é exatamente o mesmo padrão de `AgendamentoConflitanteException` e `EmailJaExisteException`.

### 9.3 Assinaturas adicionadas em `IAgendamentoCatalogoRepository`

Editar `Application/Agendamentos/Persistence/IAgendamentoCatalogoRepository.cs`:

```csharp
/// <summary>
/// Retorna `celulas_ativas` da filial (RN009/RF018). Null se a filial não existir.
/// AsNoTracking. Reaproveitado aqui (em vez de chamar IFilialRepository) para
/// manter o slice de Agendamentos auto-suficiente em suas leituras de validação.
/// </summary>
Task<int?> ObterCelulasAtivasFilialAsync(Guid filialId, CancellationToken cancellationToken);

/// <summary>
/// Conta agendamentos com status 'agendado' na filial cuja janela
/// [inicio_existente, fim_existente) se sobrepõe a [inicio, fim). Suporta a
/// validação de capacidade do RF008 (best-effort no MVP — ver ADR RF018 §9).
/// </summary>
Task<int> ContarSobreposicoesNaFilialAsync(
    Guid filialId,
    DateTime inicio,
    DateTime fim,
    CancellationToken cancellationToken);
```

Implementação em `Infrastructure/Persistence/Repositories/AgendamentoCatalogoRepository.cs`:

```csharp
public Task<int?> ObterCelulasAtivasFilialAsync(Guid filialId, CancellationToken ct) =>
    _db.Filiais.AsNoTracking()
        .Where(f => f.Id == filialId)
        .Select(f => (int?)f.CelulasAtivas)
        .FirstOrDefaultAsync(ct);

public Task<int> ContarSobreposicoesNaFilialAsync(
    Guid filialId, DateTime inicio, DateTime fim, CancellationToken ct) =>
    _db.Agendamentos.AsNoTracking()
        .Where(a => a.FilialId == filialId
                 && a.StatusRaw == "agendado"
                 && a.Inicio < fim
                 && a.Fim > inicio)
        .CountAsync(ct);
```

> O índice `Agendamento(FilialId, Inicio)` já está coberto pelas migrações existentes (ver `Agendamento` no DB). Se não estiver, abrir card de performance específico — sem migration nova nesta entrega (instrução do refinamento).

### 9.4 Trade-off da race condition (documentado)

A validação acima é **best-effort**. Dois INSERTs concorrentes podem passar pelo pré-check antes de qualquer um salvar, resultando em até `N+1` agendamentos simultâneos. Aceito no MVP porque:

- O custo de uma constraint dura no banco para "count overlap por filial ≤ N" exigiria função PL/pgSQL + trigger ou EXCLUDE com `tstzrange` agregado — complexidade que YAGNI ataca.
- O impacto operacional é mínimo (raríssimo passar de N+1 com tráfego do MVP).
- Caminho de evolução: card pós-MVP adiciona advisory lock por `filial_id` ao iniciar a transação de criação (`pg_advisory_xact_lock(hashtext('filial:'||id))`). Suficiente para sequencializar o caminho crítico sem mudar schema.

Esta nota deve ser repetida como comentário inline em `GarantirCapacidadeFilialAsync` (já incluída acima).

---

## 10. Mapeamento de exceções no `ExceptionHandlingMiddleware`

**Nenhuma mudança obrigatória no middleware.** Justificativa:

| Cenário | Exceção lançada | Tratamento atual no middleware | Status HTTP |
|---|---|---|---|
| Faixa fora 1..100 (validator) | `ValidationException` | `catch (ValidationException)` existente | 400 |
| Nome duplicado no POST | `NomeFilialJaExisteException : ConflictException` | `catch (ConflictException)` existente | 409 |
| Filial não encontrada | `NotFoundException` | `catch (NotFoundException)` existente | 404 |
| Capacidade esgotada RF008 | `CapacidadeFilialEsgotadaException : ConflictException` | `catch (ConflictException)` existente | 409 |
| 401 sem token | (não chega à pipeline) | `JwtBearerEvents.OnChallenge` (novo) | 401 |
| 403 sem permissão | (não chega à pipeline) | `JwtBearerEvents.OnForbidden` (novo) | 403 |
| `DomainException` por algum invariant inesperado | `DomainException` | `catch (DomainException)` existente | 400 |
| Falha não tratada | `Exception` | `catch (Exception)` → "Não foi possível concluir a operação..." | 500 |

A mensagem 500 já é exatamente "Não foi possível concluir a operação no momento. Tente novamente." (constante `MensagemErroInterno` em `ExceptionHandlingMiddleware`). 100% aderente ao card.

---

## 11. Lista consolidada de arquivos

### 11.1 Novos (criar)

```
backend/src/CarWash.Application/Filiais/Common/FilialResponse.cs
backend/src/CarWash.Application/Filiais/Common/NomeFilialJaExisteException.cs
backend/src/CarWash.Application/Filiais/Persistence/IFilialRepository.cs
backend/src/CarWash.Application/Filiais/CriarFilial/CriarFilialRequest.cs
backend/src/CarWash.Application/Filiais/CriarFilial/CriarFilialCommand.cs
backend/src/CarWash.Application/Filiais/CriarFilial/CriarFilialCommandValidator.cs
backend/src/CarWash.Application/Filiais/CriarFilial/CriarFilialHandler.cs
backend/src/CarWash.Application/Filiais/AlterarCelulasAtivas/AlterarCelulasAtivasRequest.cs
backend/src/CarWash.Application/Filiais/AlterarCelulasAtivas/AlterarCelulasAtivasCommand.cs
backend/src/CarWash.Application/Filiais/AlterarCelulasAtivas/AlterarCelulasAtivasCommandValidator.cs
backend/src/CarWash.Application/Filiais/AlterarCelulasAtivas/AlterarCelulasAtivasHandler.cs
backend/src/CarWash.Application/Filiais/ObterFilialPorId/ObterFilialPorIdQuery.cs
backend/src/CarWash.Application/Filiais/ObterFilialPorId/ObterFilialPorIdHandler.cs

backend/src/CarWash.Application/Agendamentos/Common/CapacidadeFilialEsgotadaException.cs

backend/src/CarWash.Infrastructure/Persistence/Repositories/FilialRepository.cs

backend/src/CarWash.Api/Endpoints/Filiais/FiliaisEndpoints.cs
backend/src/CarWash.Api/Extensions/AuthorizationPoliciesExtensions.cs
```

Caminhos absolutos: prefixar todos com `/home/gbrogio/university/carwash/`.

### 11.2 Existentes (editar)

| Arquivo | Mudança | Motivo |
|---|---|---|
| `/home/gbrogio/university/carwash/backend/src/CarWash.Infrastructure/DependencyInjection.cs` | acrescentar `using CarWash.Application.Filiais.Persistence;` e `services.AddScoped<IFilialRepository, FilialRepository>();` | DI do repositório novo |
| `/home/gbrogio/university/carwash/backend/src/CarWash.Api/Endpoints/EndpointRouteBuilderExtensions.cs` | `using CarWash.Api.Endpoints.Filiais;` + `app.MapFiliais();` | registrar endpoints |
| `/home/gbrogio/university/carwash/backend/src/CarWash.Api/Program.cs` | trocar `AddAuthorization()` por `AddCarWashAuthorization()`; acrescentar `options.Events = new JwtBearerEvents { OnChallenge = ..., OnForbidden = ... }` no `AddJwtBearer` | policy `"Admin"` + mensagens 401/403 exatas do card |
| `/home/gbrogio/university/carwash/backend/src/CarWash.Application/Agendamentos/Persistence/IAgendamentoCatalogoRepository.cs` | adicionar `ObterCelulasAtivasFilialAsync` e `ContarSobreposicoesNaFilialAsync` | RF008 |
| `/home/gbrogio/university/carwash/backend/src/CarWash.Infrastructure/Persistence/Repositories/AgendamentoCatalogoRepository.cs` | implementar os dois métodos novos | RF008 |
| `/home/gbrogio/university/carwash/backend/src/CarWash.Application/Agendamentos/Common/CalculadoraResumoAgendamento.cs` | chamar `GarantirCapacidadeFilialAsync` após `GarantirFilialAsync` | RF008 — defesa server-side |

**NÃO** editar:
- `Domain/Entities/Filial.cs` (já pronto).
- `Infrastructure/Persistence/Configurations/FilialConfiguration.cs` (já pronto).
- migrations (já contemplam o schema).
- `ExceptionHandlingMiddleware.cs` (cobertura por `ConflictException` é suficiente).

---

## 12. Ordem de implementação recomendada (commits semânticos)

Sequência projetada para commits pequenos, cada um compilando e com seus testes:

1. **`feat(filiais): porta de persistência e dto de saída`**
   `IFilialRepository.cs`, `FilialResponse.cs`, `NomeFilialJaExisteException.cs`. Sem mudanças visíveis fora.

2. **`feat(filiais): repositório EF + DI`**
   `FilialRepository.cs` + edit `Infrastructure/DependencyInjection.cs`. Testes de integração mínimos (Testcontainers) para `ObterPorId`, `ExisteComNome`, `Salvar` com violação de UK.

3. **`feat(filiais): use case obter por id`**
   Slice `ObterFilialPorId/` + endpoint GET registrado (e `MapFiliais` em `EndpointRouteBuilderExtensions`). 200/404/401. Não precisa Admin.

4. **`feat(filiais): policy admin + mensagens 401/403`**
   `AuthorizationPoliciesExtensions.cs` + edit `Program.cs` (policy + JwtBearer events). Testes E2E:
   - sem token → 401 com title "Autenticação obrigatória para executar esta operação.";
   - token de Funcionario em rota Admin → 403 com title "Você não possui permissão para alterar configuração da filial.".

5. **`feat(filiais): criar filial (POST)`**
   Slice `CriarFilial/` + endpoint POST com `ValidationFilter<CriarFilialCommand>`. Testes:
   - 201 com Admin, body válido (CelulasAtivas=10);
   - 400 faixa: mensagem EXATA "Valor de células ativas inválido. Informe um número inteiro entre 1 e 100.";
   - 400 body vazio / tipo errado em `celulasAtivas`;
   - 409 nome duplicado;
   - 401 sem token, 403 com Funcionario.

6. **`feat(filiais): alterar células ativas (PATCH)`**
   Slice `AlterarCelulasAtivas/` + endpoint PATCH (validator inline). Testes 200/400/401/403/404 + idempotência (mesmo valor → 200 sem audit). Verificar que `audit_logs` tem evento `FilialCelulasAlteradas` com `before/after`.

7. **`feat(agendamentos): validar capacidade de filial (RF008)`**
   - editar `IAgendamentoCatalogoRepository` + impl;
   - `CapacidadeFilialEsgotadaException.cs`;
   - editar `CalculadoraResumoAgendamento.CalcularAsync` para invocar `GarantirCapacidadeFilialAsync`;
   - testes de integração: criar filial com `CelulasAtivas=1`, criar 1 agendamento na janela e validar que o 2º falha com 409 + mensagem "Capacidade da filial esgotada para o horário solicitado.";
   - teste regressivo: PATCH reduzindo `celulas_ativas` afeta novas tentativas (CA5/CA6 do RF018).

---

## 13. Checklist final (gates de aceite para PR)

- [ ] Filial não foi modificada (Domain/Entities/Filial.cs intacta).
- [ ] Nenhuma migration nova.
- [ ] `IAuditLogger.LogAsync` chamado nos dois fluxos de escrita com `evento = "FilialCriada"` / `"FilialCelulasAlteradas"`.
- [ ] `ICurrentRequestContext.DefinirEvento(...)` chamado antes do `SaveChanges` para que o interceptor capture o diff.
- [ ] Mensagem 400 (faixa), 401, 403, 404, 500 EXATAS conforme o card.
- [ ] Endpoint PATCH usa validação inline (id da rota + body) — mesmo padrão de `AlterarUsuarioAsync`.
- [ ] Endpoint POST usa `ValidationFilter<CriarFilialCommand>` — mesmo padrão de `CriarUsuario`.
- [ ] `CalculadoraResumoAgendamento` invoca a verificação de capacidade após validar filial.
- [ ] `CapacidadeFilialEsgotadaException` herda de `ConflictException` (não precisa `catch` novo no middleware).
- [ ] Testes E2E cobrem 200/201/400/401/403/404/409 — em especial 401/403 com as mensagens custom (JwtBearerEvents).
- [ ] Comentário inline com o trade-off da race condition no `GarantirCapacidadeFilialAsync` (best-effort no MVP).
