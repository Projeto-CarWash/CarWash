using CarWash.Application.DTOs.Clientes;
using CarWash.Application.Services.Clientes;
using Microsoft.AspNetCore.Mvc;

namespace CarWash.Api.Controllers;

[ApiController]
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

        var response = await clienteService.CriarAsync(
            request,
            traceId,
            usuarioId,
            cancellationToken);

        logger.LogInformation(
            "Cliente cadastrado com sucesso. TraceId: {TraceId}. ClienteId: {ClienteId}. UsuarioId: {UsuarioId}",
            traceId,
            response.Id,
            usuarioId);

        return CreatedAtAction(
            nameof(ObterPorId),
            new { id = response.Id },
            response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var cliente = await clienteService.ObterPorIdAsync(id, cancellationToken);

        if (cliente is null)
        {
            return NotFound();
        }

        return Ok(cliente);
    }

    private Guid? ObterUsuarioId()
    {
        var usuarioIdClaim = User.FindFirst("sub")?.Value;

        if (Guid.TryParse(usuarioIdClaim, out var usuarioId))
        {
            return usuarioId;
        }

        return null;
    }
}
