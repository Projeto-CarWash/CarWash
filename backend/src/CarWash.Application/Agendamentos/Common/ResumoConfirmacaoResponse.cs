namespace CarWash.Application.Agendamentos.Common;

/// <summary>
/// Resumo de negócio de um agendamento, montado para a etapa de revisão do RF015.
/// É o conteúdo apresentado ao usuário antes de confirmar e a base do
/// <see cref="HashResumo"/> — qualquer alteração nos campos de negócio invalida
/// a confirmação.
/// </summary>
public sealed class ResumoConfirmacaoResponse
{
    public ResumoFilial Filial { get; init; } = new();

    public ResumoCliente Cliente { get; init; } = new();

    public ResumoVeiculo Veiculo { get; init; } = new();

    public IReadOnlyList<ResumoServico> Servicos { get; init; } = Array.Empty<ResumoServico>();

    public DateTime Inicio { get; init; }

    public DateTime Fim { get; init; }

    public int DuracaoTotalMin { get; init; }

    public decimal ValorTotal { get; init; }

    public string? Observacoes { get; init; }

    /// <summary>Gets sHA-256 (hex minúsculo) da forma canônica dos campos de negócio.</summary>
    public string HashResumo { get; init; } = string.Empty;
}

/// <summary>Filial exibida no resumo de confirmação (RF015).</summary>
public sealed class ResumoFilial
{
    public Guid Id { get; init; }

    public string Nome { get; init; } = string.Empty;
}

/// <summary>Cliente exibido no resumo de confirmação (RF015).</summary>
public sealed class ResumoCliente
{
    public Guid Id { get; init; }

    public string Nome { get; init; } = string.Empty;

    public string Documento { get; init; } = string.Empty;
}

/// <summary>Veículo exibido no resumo de confirmação (RF015).</summary>
public sealed class ResumoVeiculo
{
    public Guid Id { get; init; }

    public string Placa { get; init; } = string.Empty;

    public string Modelo { get; init; } = string.Empty;

    public string Cor { get; init; } = string.Empty;
}

/// <summary>Serviço exibido no resumo de confirmação (RF015) — preço/duração de catálogo.</summary>
public sealed class ResumoServico
{
    public Guid Id { get; init; }

    public string Nome { get; init; } = string.Empty;

    public int DuracaoMin { get; init; }

    public decimal Preco { get; init; }
}
