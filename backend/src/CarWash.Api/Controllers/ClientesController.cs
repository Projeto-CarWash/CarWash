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
        "Tentativa inválida de cadastro de cliente. TraceId: {TraceId}",
        traceId);

    return BadRequest(new ApiErrorResponse
    {
        Code = "CLIENTE_VALIDATION_ERROR",
        Message = "Dados inválidos. Verifique os campos do cliente e dos veículos.",
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
        catch (VeiculoPlacaDuplicadaException)
        {
            logger.LogWarning(
                "Tentativa de cadastro com placa duplicada. TraceId: {TraceId}",
                traceId);

            return Conflict(new ApiErrorResponse
            {
                Code = "VEICULO_PLACA_DUPLICADA",
                Message = "Já existe veículo cadastrado com uma das placas informadas.",
                TraceId = traceId,
            });
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Falha inesperada ao cadastrar cliente e veículos. TraceId: {TraceId}",
                traceId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse
            {
                Code = "INTERNAL_SERVER_ERROR",
                Message = "Não foi possível concluir o cadastro no momento. Tente novamente.",
                TraceId = traceId,
            });
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiDataResponse<ClienteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ObterPorId(string id, CancellationToken cancellationToken)
    {
        string traceId = HttpContext.TraceIdentifier;

        if (!Guid.TryParse(id, out Guid clienteId))
        {
            return BadRequest(new ApiErrorResponse
            {
                Code = "CLIENTE_INVALID_ID",
                Message = "Identificador de cliente inválido.",
                TraceId = traceId,
            });
        }

        try
        {
            ClienteResponse? cliente = await clienteService.ObterPorIdAsync(clienteId, cancellationToken);

            if (cliente is null)
            {
                return NotFound(new ApiErrorResponse
                {
                    Code = "CLIENTE_NOT_FOUND",
                    Message = "Cliente não encontrado.",
                    TraceId = traceId,
                });
            }

            return Ok(new ApiDataResponse<ClienteResponse>
            {
                Data = cliente,
                TraceId = traceId,
            });
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Falha inesperada ao consultar detalhe do cliente. TraceId: {TraceId}. ClienteId: {ClienteId}",
                traceId,
                id);

            return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse
            {
                Code = "INTERNAL_SERVER_ERROR",
                Message = "Não foi possível concluir a consulta no momento. Tente novamente.",
                TraceId = traceId,
            });
        }
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
