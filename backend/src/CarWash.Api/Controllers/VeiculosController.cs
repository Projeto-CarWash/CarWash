using System.Diagnostics;
using CarWash.Application.DTOs;
using CarWash.Application.Exceptions;
using CarWash.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarWash.Api.Controllers;

[ApiController]
[Route("api/v1/veiculos")]
public class VeiculosController : ControllerBase
{
    private readonly IVeiculoService _veiculoService;
    private readonly ILogger<VeiculosController> _logger;

    public VeiculosController(IVeiculoService veiculoService, ILogger<VeiculosController> logger)
    {
        _veiculoService = veiculoService;
        _logger = logger;
    }

    [HttpPatch("{veiculoId}")]
    [Authorize]
    public async Task<IActionResult> Atualizar(
        [FromRoute] string veiculoId,
        [FromBody] CriarVeiculoRequest? request,
        CancellationToken cancellationToken)
    {
        string traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "VEICULO_ATUALIZACAO_RECEBIDA: VeiculoId {VeiculoId} TraceId {TraceId}",
            veiculoId,
            traceId);

        if (!Guid.TryParse(veiculoId, out Guid veiculoGuid))
        {
            _logger.LogWarning(
                "VEICULO_ATUALIZACAO_ID_INVALIDO: VeiculoId {VeiculoId} TraceId {TraceId}",
                veiculoId,
                traceId);

            throw new ApiException(
                400,
                "VEICULO_VALIDATION_ERROR",
                "Dados do veículo inválidos. Verifique os campos e tente novamente.",
                new Dictionary<string, string[]>
                {
                    ["veiculoId"] = new[] { "Veículo deve ser um UUID válido." }
                });
        }

        Guid updatedId = await _veiculoService.AtualizarVeiculoAsync(
            veiculoGuid,
            request ?? new CriarVeiculoRequest(),
            traceId,
            cancellationToken);

        return Ok(new VeiculoResponse
        {
            Id = updatedId,
            Message = "Veículo atualizado com sucesso.",
            TraceId = traceId
        });
    }
}
