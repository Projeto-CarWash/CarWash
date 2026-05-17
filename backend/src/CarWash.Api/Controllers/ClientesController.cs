using CarWash.Application.Common.Exceptions;
using CarWash.Application.DTOs.Clientes;
using CarWash.Application.Services.Clientes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarWash.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/clientes")]
public class ClientesController : ControllerBase
{
    private const int TamanhoPaginaMaximo = 100;

    private readonly IClienteService clienteService;
    private readonly ILogger<ClientesController> logger;

    public ClientesController(
        IClienteService clienteService,
        ILogger<ClientesController> logger)
    {
        this.clienteService = clienteService;
        this.logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateClienteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Criar(
        [FromBody] CreateClienteRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var usuarioId = ObterUsuarioId();

        var response = await clienteService.CriarAsync(request, traceId, usuarioId, cancellationToken);

        logger.LogInformation(
            "Cliente cadastrado com sucesso. TraceId: {TraceId}. ClienteId: {ClienteId}. UsuarioId: {UsuarioId}",
            traceId,
            response.Id,
            usuarioId);

        return CreatedAtAction(nameof(ObterPorId), new { id = response.Id }, response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ListaClientesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busca,
        [FromQuery] bool? ativo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        // GAP-PAG-0 + GAP-CLAMP: validação explícita de paginação. Antes a página
        // <= 0 e o tamanho fora da faixa eram normalizados silenciosamente, levando
        // o cliente a achar que o filtro estava sendo respeitado.
        var erros = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (pagina < 1)
        {
            erros["pagina"] = ["Página deve ser maior ou igual a 1."];
        }

        if (tamanhoPagina < 1)
        {
            erros["tamanhoPagina"] = ["Tamanho da página deve ser maior ou igual a 1."];
        }
        else if (tamanhoPagina > TamanhoPaginaMaximo)
        {
            erros["tamanhoPagina"] = [$"Tamanho da página deve ser no máximo {TamanhoPaginaMaximo}."];
        }

        if (erros.Count > 0)
        {
            throw new ValidationException(
                "Parâmetros de paginação inválidos. Verifique os campos e tente novamente.",
                erros);
        }

        // BUG-CACHE-PII: lista expõe PII (nome, e-mail, telefone) — proibir cache.
        Response.Headers.CacheControl = "no-store";

        var resp = await clienteService.ListarAsync(busca, ativo, pagina, tamanhoPagina, cancellationToken);
        return Ok(resp);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        // BUG-CACHE-PII: detalhe contém PII completa — proibir cache em proxies/browser.
        Response.Headers.CacheControl = "no-store";

        var cliente = await clienteService.ObterPorIdAsync(id, cancellationToken);

        // BUG-CONTRATO-404-ROUTE: 404 via NotFoundException garante ProblemDetails
        // canônico do ExceptionHandlingMiddleware (type/title/status/correlationId),
        // ao invés do formato padrão MVC (tools.ietf.org + traceId).
        if (cliente is null)
        {
            throw new NotFoundException("Cliente não encontrado.");
        }

        return Ok(cliente);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(
        Guid id,
        [FromBody] UpdateClienteRequest request,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        var resp = await clienteService.AtualizarAsync(id, request, usuarioId, cancellationToken);

        logger.LogInformation(
            "Cliente atualizado. ClienteId: {ClienteId}. UsuarioId: {UsuarioId}", id, usuarioId);

        return Ok(resp);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AlterarStatus(
        Guid id,
        [FromBody] AlterarStatusClienteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // GAP-CW-CLI-STA-EMP: body {} ou ausência do campo "ativo" precisa
        // falhar com 400 — o tipo agora é bool? para distinguir "não informado"
        // de "false". Antes o framework caía em default(bool)=false silencioso.
        if (request.Ativo is null)
        {
            throw new ValidationException(
                "Dados inválidos. Verifique os campos e tente novamente.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ativo"] = ["Campo 'ativo' é obrigatório."],
                });
        }

        var usuarioId = ObterUsuarioId();
        var resp = await clienteService.AlterarStatusAsync(id, request.Ativo.Value, usuarioId, cancellationToken);

        logger.LogInformation(
            "Status do cliente alterado. ClienteId: {ClienteId}. Ativo: {Ativo}. UsuarioId: {UsuarioId}",
            id,
            request.Ativo.Value,
            usuarioId);

        return Ok(resp);
    }

    private Guid? ObterUsuarioId()
    {
        var usuarioIdClaim = User.FindFirst("sub")?.Value;
        return Guid.TryParse(usuarioIdClaim, out var id) ? id : null;
    }
}
