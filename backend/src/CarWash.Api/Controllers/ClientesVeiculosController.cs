using System.Diagnostics;
using CarWash.Application.DTOs;
using CarWash.Application.Exceptions;
using CarWash.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarWash.Api.Controllers;

[ApiController]
[Route("api/v1/clientes/{clienteId}/veiculos")]
public class ClientesVeiculosController : ControllerBase
{
    private readonly IVeiculoService _veiculoService;
    private readonly ILogger<ClientesVeiculosController> _logger;

    public ClientesVeiculosController(
        IVeiculoService veiculoService,
        ILogger<ClientesVeiculosController> logger)
    {
        _veiculoService = veiculoService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Policy = "CanCreateVehicle")]
    public async Task<IActionResult> Criar(
        [FromRoute] string clienteId,
        [FromBody] CriarVeiculoRequest? request,
        CancellationToken cancellationToken)
    {
        string traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "VEICULO_CADASTRO_RECEBIDO: ClienteId {ClienteId} TraceId {TraceId}",
            clienteId,
            traceId);

        if (!Guid.TryParse(clienteId, out Guid clienteGuid))
        {
            _logger.LogWarning(
                "VEICULO_CADASTRO_CLIENTE_INVALIDO: ClienteId {ClienteId} TraceId {TraceId}",
                clienteId,
                traceId);

            throw new ApiException(
                400,
                "VEICULO_VALIDATION_ERROR",
                "Dados do veículo inválidos. Verifique os campos e tente novamente.",
                new Dictionary<string, string[]>
                {
                    ["clienteId"] = new[] { "Cliente deve ser um UUID válido." }
                });
        }

        Guid veiculoId = await _veiculoService.CriarVeiculoAsync(
            clienteGuid,
            request ?? new CriarVeiculoRequest(),
            traceId,
            cancellationToken);

        return Created(
            $"/api/v1/clientes/{clienteId}/veiculos/{veiculoId}",
            new VeiculoResponse
            {
                Id = veiculoId,
                Message = "Veículo cadastrado com sucesso.",
                TraceId = traceId
            });
    }
}
