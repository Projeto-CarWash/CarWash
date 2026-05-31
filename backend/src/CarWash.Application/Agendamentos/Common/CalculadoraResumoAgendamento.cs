using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CarWash.Application.Agendamentos.Persistence;
using CarWash.Application.Common;
using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Agendamentos.Common;

/// <summary>
/// Serviço de domínio que valida as dependências de um agendamento (filial,
/// veículo, cliente, responsável, serviços — RF019/RN010/CA007/CA009), calcula os
/// totais denormalizados (RN006), monta o <see cref="ResumoConfirmacaoResponse"/>
/// e deriva o <c>hashResumo</c>. É a lógica comum a RF007 (criação), à
/// pré-confirmação e à confirmação do RF015 — extraída para evitar divergência
/// entre os três fluxos.
/// </summary>
public sealed class CalculadoraResumoAgendamento
{
    private const string MensagemPayloadInvalido =
        "Dados do agendamento inválidos. Verifique os campos e tente novamente.";

    private readonly IAgendamentoCatalogoRepository _catalogo;

    public CalculadoraResumoAgendamento(IAgendamentoCatalogoRepository catalogo)
    {
        _catalogo = catalogo;
    }

    /// <summary>
    /// Valida e calcula o resumo de um agendamento. Lança
    /// <see cref="NotFoundException"/> (recurso inexistente),
    /// <see cref="RecursoInativoException"/> (recurso inativo) ou
    /// <see cref="ValidationException"/> (vínculo inconsistente — RN002/CA009).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<ResumoAgendamentoCalculado> CalcularAsync(
        Guid filialId,
        Guid clienteId,
        Guid veiculoId,
        Guid? responsavelId,
        DateTime inicio,
        IReadOnlyList<Guid> servicoIds,
        string? observacoes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(servicoIds);

        if (servicoIds.Count == 0)
        {
            throw new ValidationException(
                MensagemPayloadInvalido,
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["servicoIds"] = ["Informe ao menos um serviço."],
                });
        }

        var inicioUtc = DateTime.SpecifyKind(inicio.ToUniversalTime(), DateTimeKind.Utc);
        string? observacoesNormalizadas = InputNormalizer.SanitizeTextOrNull(observacoes);

        var filial = await GarantirFilialAsync(filialId, cancellationToken).ConfigureAwait(false);
        var veiculo = await GarantirVeiculoAsync(veiculoId, cancellationToken).ConfigureAwait(false);
        var cliente = await GarantirClienteAsync(clienteId, cancellationToken).ConfigureAwait(false);
        GarantirVinculoVeiculoCliente(veiculo, clienteId);
        await GarantirResponsavelAsync(responsavelId, clienteId, cancellationToken).ConfigureAwait(false);

        var servicos = await GarantirServicosAsync(servicoIds, cancellationToken).ConfigureAwait(false);

        int duracaoTotal = servicos.Sum(s => s.DuracaoMin);
        decimal valorTotal = servicos.Sum(s => s.Preco);
        var fim = inicioUtc.AddMinutes(duracaoTotal);

        string hashResumo = CalcularHashResumo(
            filialId,
            clienteId,
            veiculoId,
            responsavelId,
            servicoIds,
            inicioUtc,
            duracaoTotal,
            valorTotal,
            observacoesNormalizadas);

        var resumo = new ResumoConfirmacaoResponse
        {
            Filial = new ResumoFilial { Id = filial.Id, Nome = filial.Nome },
            Cliente = new ResumoCliente
            {
                Id = cliente.Id,
                Nome = cliente.Nome,
                Documento = cliente.Documento,
            },
            Veiculo = new ResumoVeiculo
            {
                Id = veiculo.Id,
                Placa = veiculo.Placa,
                Modelo = veiculo.Modelo,
                Cor = veiculo.Cor,
            },
            Servicos = servicos
                .Select(s => new ResumoServico
                {
                    Id = s.Id,
                    Nome = s.Nome,
                    DuracaoMin = s.DuracaoMin,
                    Preco = s.Preco,
                })
                .ToList(),
            Inicio = inicioUtc,
            Fim = fim,
            DuracaoTotalMin = duracaoTotal,
            ValorTotal = valorTotal,
            Observacoes = observacoesNormalizadas,
            HashResumo = hashResumo,
        };

        return new ResumoAgendamentoCalculado(
            resumo,
            servicos,
            inicioUtc,
            fim,
            duracaoTotal,
            valorTotal,
            observacoesNormalizadas);
    }

    /// <summary>
    /// Deriva o <c>hashResumo</c> (RF015 / ADR 0004): SHA-256 hex minúsculo sobre
    /// uma string canônica dos campos de negócio. O <c>fim</c> NÃO entra — é
    /// derivado de <c>inicio</c> + duração total. Determinístico e independente
    /// de cultura/ordenação de entrada.
    /// </summary>
    /// <returns></returns>
    public static string CalcularHashResumo(
        Guid filialId,
        Guid clienteId,
        Guid veiculoId,
        Guid? responsavelId,
        IReadOnlyList<Guid> servicoIds,
        DateTime inicioUtc,
        int duracaoTotalMin,
        decimal valorTotal,
        string? observacoes)
    {
        ArgumentNullException.ThrowIfNull(servicoIds);

        string inicioCanonico = DateTime
            .SpecifyKind(inicioUtc.ToUniversalTime(), DateTimeKind.Utc)
            .ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

        string servicosCanonicos = string.Join(
            ',',
            servicoIds
                .Select(id => id.ToString("D", CultureInfo.InvariantCulture))
                .OrderBy(s => s, StringComparer.Ordinal));

        string observacoesCanonicas = InputNormalizer.SanitizeTextOrNull(observacoes) ?? "null";

        string canonico = string.Join(
            '|',
            filialId.ToString("D", CultureInfo.InvariantCulture),
            clienteId.ToString("D", CultureInfo.InvariantCulture),
            veiculoId.ToString("D", CultureInfo.InvariantCulture),
            responsavelId is { } r ? r.ToString("D", CultureInfo.InvariantCulture) : "null",
            servicosCanonicos,
            inicioCanonico,
            duracaoTotalMin.ToString(CultureInfo.InvariantCulture),
            valorTotal.ToString("F2", CultureInfo.InvariantCulture),
            observacoesCanonicas);

        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonico));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static void GarantirVinculoVeiculoCliente(VeiculoResumoSnapshot veiculo, Guid clienteId)
    {
        // RN002: o veículo informado precisa pertencer ao cliente selecionado.
        if (veiculo.ClienteId != clienteId)
        {
            throw new ValidationException(
                MensagemPayloadInvalido,
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["veiculoId"] = ["O veículo informado não pertence ao cliente selecionado."],
                });
        }
    }

    private async Task<FilialResumoSnapshot> GarantirFilialAsync(Guid filialId, CancellationToken cancellationToken)
    {
        var filial = await _catalogo.ObterFilialResumoAsync(filialId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Filial informada não foi encontrada.");

        if (!filial.Ativa)
        {
            throw new RecursoInativoException("A filial selecionada está inativa e não aceita agendamentos.");
        }

        return filial;
    }

    private async Task<VeiculoResumoSnapshot> GarantirVeiculoAsync(Guid veiculoId, CancellationToken cancellationToken)
    {
        var veiculo = await _catalogo.ObterVeiculoResumoAsync(veiculoId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Veículo informado não foi encontrado.");

        if (!veiculo.Ativo)
        {
            throw new RecursoInativoException("O veículo selecionado está inativo e não pode ser agendado.");
        }

        return veiculo;
    }

    private async Task<ClienteResumoSnapshot> GarantirClienteAsync(Guid clienteId, CancellationToken cancellationToken)
    {
        var cliente = await _catalogo.ObterClienteResumoAsync(clienteId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Cliente informado não foi encontrado.");

        if (!cliente.Ativo)
        {
            throw new RecursoInativoException("O cliente selecionado está inativo e não pode ser agendado.");
        }

        return cliente;
    }

    private async Task GarantirResponsavelAsync(Guid? responsavelId, Guid clienteId, CancellationToken cancellationToken)
    {
        if (!responsavelId.HasValue)
        {
            return;
        }

        var responsavel = await _catalogo.ObterResponsavelAsync(responsavelId.Value, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Responsável informado não foi encontrado.");

        if (!responsavel.Ativo)
        {
            throw new RecursoInativoException("O responsável selecionado está inativo.");
        }

        // CA009: responsável só pode agendar em nome do seu próprio titular.
        if (responsavel.ClienteId != clienteId)
        {
            throw new ValidationException(
                MensagemPayloadInvalido,
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["responsavelId"] = ["O responsável não pertence ao titular do veículo."],
                });
        }
    }

    private async Task<IReadOnlyList<ServicoSnapshot>> GarantirServicosAsync(
        IReadOnlyList<Guid> servicoIds,
        CancellationToken cancellationToken)
    {
        var encontrados = await _catalogo.ObterServicosAsync(servicoIds, cancellationToken).ConfigureAwait(false);

        var ausentes = servicoIds
            .Where(id => encontrados.All(s => s.Id != id))
            .ToList();
        if (ausentes.Count > 0)
        {
            throw new NotFoundException("Um ou mais serviços informados não foram encontrados.");
        }

        var inativos = encontrados.Where(s => !s.Ativo).ToList();
        if (inativos.Count > 0)
        {
            throw new RecursoInativoException(
                "Um ou mais serviços selecionados estão inativos e não podem ser agendados.");
        }

        // Preserva a ordem informada pelo cliente.
        return servicoIds
            .Select(id => encontrados.First(s => s.Id == id))
            .ToList();
    }
}
