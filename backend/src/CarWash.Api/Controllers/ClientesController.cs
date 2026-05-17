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
    public async Task<IActionResult> Listar(
        [FromQuery] string? busca,
        [FromQuery] bool? ativo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var resp = await clienteService.ListarAsync(busca, ativo, pagina, tamanhoPagina, cancellationToken);
        return Ok(resp);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var cliente = await clienteService.ObterPorIdAsync(id, cancellationToken);
        return cliente is null ? NotFound() : Ok(cliente);
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AlterarStatus(
        Guid id,
        [FromBody] AlterarStatusClienteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var usuarioId = ObterUsuarioId();
        var resp = await clienteService.AlterarStatusAsync(id, request.Ativo, usuarioId, cancellationToken);

        logger.LogInformation(
            "Status do cliente alterado. ClienteId: {ClienteId}. Ativo: {Ativo}. UsuarioId: {UsuarioId}",
            id,
            request.Ativo,
            usuarioId);

        return Ok(resp);
    }

    private Guid? ObterUsuarioId()
    {
        var usuarioIdClaim = User.FindFirst("sub")?.Value;
        return Guid.TryParse(usuarioIdClaim, out var id) ? id : null;
    }
}
