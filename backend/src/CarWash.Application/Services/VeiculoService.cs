using System.Linq;
using System.Text.RegularExpressions;
using CarWash.Application.DTOs;
using CarWash.Application.Exceptions;
using CarWash.Application.Interfaces;
using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Services;

/// <summary>
/// Servico de cadastro de veiculos.
/// </summary>
public class VeiculoService : IVeiculoService
{
    private static readonly Regex PlacaRegex = new("^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$", RegexOptions.Compiled);

    private readonly ICarWashDbContext _context;
    private readonly ILogger<VeiculoService> _logger;

    public VeiculoService(ICarWashDbContext context, ILogger<VeiculoService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid> CriarVeiculoAsync(
        Guid clienteId,
        CriarVeiculoRequest request,
        string traceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalized = Normalize(request);

            var validationErrors = Validate(normalized);
            if (validationErrors.Count > 0)
            {
                _logger.LogWarning(
                    "VEICULO_CADASTRO_VALIDACAO_FALHA: ClienteId {ClienteId} TraceId {TraceId}",
                    clienteId,
                    traceId);

                throw new ApiException(
                    400,
                    "VEICULO_VALIDATION_ERROR",
                    "Dados do veículo inválidos. Verifique os campos e tente novamente.",
                    validationErrors);
            }

            _logger.LogInformation(
                "VEICULO_CADASTRO_VALIDACAO_OK: ClienteId {ClienteId} TraceId {TraceId}",
                clienteId,
                traceId);

            bool clienteExiste = await _context.Clientes
                .AnyAsync(c => c.Id == clienteId, cancellationToken);

            if (!clienteExiste)
            {
                _logger.LogWarning(
                    "VEICULO_CADASTRO_CLIENTE_NAO_ENCONTRADO: ClienteId {ClienteId} TraceId {TraceId}",
                    clienteId,
                    traceId);

                throw new ApiException(
                    404,
                    "VEICULO_CLIENTE_NAO_ENCONTRADO",
                    "Cliente não encontrado para vincular o veículo.");
            }

            bool placaJaExiste = await _context.Veiculos
                .AnyAsync(v => v.Placa == normalized.Placa, cancellationToken);

            if (placaJaExiste)
            {
                _logger.LogWarning(
                    "VEICULO_CADASTRO_PLACA_DUPLICADA: Placa {Placa} ClienteId {ClienteId} TraceId {TraceId}",
                    normalized.Placa,
                    clienteId,
                    traceId);

                throw new ApiException(
                    409,
                    "VEICULO_PLACA_DUPLICADA",
                    "Já existe veículo cadastrado com esta placa.");
            }

            var veiculo = new Veiculo
            {
                Id = Guid.NewGuid(),
                Placa = normalized.Placa,
                Modelo = normalized.Modelo,
                Fabricante = normalized.Fabricante,
                Cor = normalized.Cor,
                ClienteId = clienteId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Veiculos.Add(veiculo);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "VEICULO_CADASTRO_SUCESSO: VeiculoId {VeiculoId} ClienteId {ClienteId} TraceId {TraceId}",
                veiculo.Id,
                clienteId,
                traceId);

            return veiculo.Id;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _logger.LogWarning(
                "VEICULO_CADASTRO_PLACA_DUPLICADA_DB: ClienteId {ClienteId} TraceId {TraceId}",
                clienteId,
                traceId);

            throw new ApiException(
                409,
                "VEICULO_PLACA_DUPLICADA",
                "Já existe veículo cadastrado com esta placa.");
        }
        catch (ApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "VEICULO_CADASTRO_ERRO_INTERNO: ClienteId {ClienteId} TraceId {TraceId}",
                clienteId,
                traceId);

            throw new ApiException(
                500,
                "VEICULO_CADASTRO_FALHA",
                "Não foi possível concluir a operação no momento. Tente novamente.");
        }
    }

    public async Task<Guid> AtualizarVeiculoAsync(
        Guid veiculoId,
        CriarVeiculoRequest request,
        string traceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalized = Normalize(request);

            var validationErrors = Validate(normalized);
            if (validationErrors.Count > 0)
            {
                _logger.LogWarning(
                    "VEICULO_ATUALIZACAO_VALIDACAO_FALHA: VeiculoId {VeiculoId} TraceId {TraceId}",
                    veiculoId,
                    traceId);

                throw new ApiException(
                    400,
                    "VEICULO_VALIDATION_ERROR",
                    "Dados do veículo inválidos. Verifique os campos e tente novamente.",
                    validationErrors);
            }

            _logger.LogInformation(
                "VEICULO_ATUALIZACAO_VALIDACAO_OK: VeiculoId {VeiculoId} TraceId {TraceId}",
                veiculoId,
                traceId);

            var veiculo = await _context.Veiculos
                .FirstOrDefaultAsync(v => v.Id == veiculoId, cancellationToken);

            if (veiculo == null)
            {
                _logger.LogWarning(
                    "VEICULO_ATUALIZACAO_NAO_ENCONTRADO: VeiculoId {VeiculoId} TraceId {TraceId}",
                    veiculoId,
                    traceId);

                throw new ApiException(
                    404,
                    "VEICULO_NAO_ENCONTRADO",
                    "Veículo não encontrado.");
            }

            bool placaJaExiste = await _context.Veiculos
                .AnyAsync(v => v.Placa == normalized.Placa && v.Id != veiculoId, cancellationToken);

            if (placaJaExiste)
            {
                _logger.LogWarning(
                    "VEICULO_ATUALIZACAO_PLACA_DUPLICADA: Placa {Placa} VeiculoId {VeiculoId} TraceId {TraceId}",
                    normalized.Placa,
                    veiculoId,
                    traceId);

                throw new ApiException(
                    409,
                    "VEICULO_PLACA_DUPLICADA",
                    "Já existe veículo cadastrado com esta placa.");
            }

            veiculo.Placa = normalized.Placa;
            veiculo.Modelo = normalized.Modelo;
            veiculo.Fabricante = normalized.Fabricante;
            veiculo.Cor = normalized.Cor;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "VEICULO_ATUALIZACAO_SUCESSO: VeiculoId {VeiculoId} TraceId {TraceId}",
                veiculoId,
                traceId);

            return veiculo.Id;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _logger.LogWarning(
                "VEICULO_ATUALIZACAO_PLACA_DUPLICADA_DB: VeiculoId {VeiculoId} TraceId {TraceId}",
                veiculoId,
                traceId);

            throw new ApiException(
                409,
                "VEICULO_PLACA_DUPLICADA",
                "Já existe veículo cadastrado com esta placa.");
        }
        catch (ApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "VEICULO_ATUALIZACAO_ERRO_INTERNO: VeiculoId {VeiculoId} TraceId {TraceId}",
                veiculoId,
                traceId);

            throw new ApiException(
                500,
                "VEICULO_ATUALIZACAO_FALHA",
                "Não foi possível concluir a operação no momento. Tente novamente.");
        }
    }

    private static CriarVeiculoRequest Normalize(CriarVeiculoRequest request)
    {
        string placaOriginal = request.Placa ?? string.Empty;
        string placaSemEspacos = string.Concat(placaOriginal.Where(c => !char.IsWhiteSpace(c)));
        string placaNormalizada = placaSemEspacos.Replace("-", string.Empty).ToUpperInvariant();

        return new CriarVeiculoRequest
        {
            Placa = placaNormalizada,
            Modelo = (request.Modelo ?? string.Empty).Trim(),
            Fabricante = (request.Fabricante ?? string.Empty).Trim(),
            Cor = (request.Cor ?? string.Empty).Trim()
        };
    }

    private static Dictionary<string, string[]> Validate(CriarVeiculoRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Placa))
        {
            errors["placa"] = new[] { "Placa é obrigatória." };
        }
        else if (request.Placa.Length != 7)
        {
            errors["placa"] = new[] { "Placa deve conter 7 caracteres válidos." };
        }
        else if (!PlacaRegex.IsMatch(request.Placa))
        {
            errors["placa"] = new[] { "Placa inválida. Formatos aceitos: AAA0000 ou AAA0A00." };
        }

        AddTextErrors(errors, "modelo", request.Modelo, 2, 80, "Modelo");
        AddTextErrors(errors, "fabricante", request.Fabricante, 2, 80, "Fabricante");
        AddTextErrors(errors, "cor", request.Cor, 2, 40, "Cor");

        return errors;
    }

    private static void AddTextErrors(
        IDictionary<string, string[]> errors,
        string field,
        string value,
        int minLength,
        int maxLength,
        string displayName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            string requiredMessage = displayName == "Cor"
                ? $"{displayName} é obrigatória."
                : $"{displayName} é obrigatório.";

            errors[field] = new[] { requiredMessage };
            return;
        }

        if (value.Length < minLength || value.Length > maxLength)
        {
            errors[field] = new[] { $"{displayName} deve ter entre {minLength} e {maxLength} caracteres." };
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        if (exception.InnerException == null)
        {
            return false;
        }

        string message = exception.InnerException.Message;
        return message.Contains("23505", StringComparison.Ordinal)
            || message.Contains("unique", StringComparison.OrdinalIgnoreCase);
    }
}
