using CarWash.Application.DTOs.Clientes;
using CarWash.Application.DTOs.Common;
using CarWash.Application.Exceptions;
using CarWash.Application.Services.Clientes;
using FluentValidation;
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
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Criar(
        [FromBody] CreateClienteRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;

        try
        {
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
        catch (ValidationException ex)
        {
            logger.LogWarning(
                ex,
                "Erro de validação ao cadastrar cliente. TraceId: {TraceId}",
                traceId);

            return BadRequest(new ApiErrorResponse
            {
                Code = "CLIENTE_VALIDATION_ERROR",
                Message = "Dados do cliente inválidos. Verifique os campos obrigatórios e tente novamente.",
                TraceId = traceId,
                Details = ex.Errors
                    .Select(error => new ApiErrorDetail
                    {
                        Field = error.PropertyName,
                        Message = error.ErrorMessage,
                    })
                    .ToList(),
            });
        }
        catch (ClienteDocumentoDuplicadoException)
        {
            logger.LogWarning(
                "Tentativa de cadastro com documento duplicado. TraceId: {TraceId}",
                traceId);

            return Conflict(new ApiErrorResponse
            {
                Code = "CLIENTE_DOCUMENTO_DUPLICADO",
                Message = "Já existe cliente cadastrado com este documento.",
                TraceId = traceId,
            });
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Falha inesperada ao cadastrar cliente. TraceId: {TraceId}",
                traceId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse
            {
                Code = "INTERNAL_SERVER_ERROR",
                Message = "Não foi possível concluir o cadastro no momento. Tente novamente.",
                TraceId = traceId,
            });
        }
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
