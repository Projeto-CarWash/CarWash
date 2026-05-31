using CarWash.Domain.Common;
using CarWash.Domain.Enums;

namespace CarWash.Domain.Entities;

/// <summary>
/// Agenda operacional. Carrega as regras críticas RN004/RN006/RN010/RN011.
/// Concorrência otimista por <c>Versao</c> (decisão P15).
/// RF010: cancelamento exige motivo e usuário; edição bloqueada para
/// <c>Finalizado</c>, <c>Cancelado</c> e <c>EmAndamento</c>.
/// </summary>
public sealed class Agendamento : IAuditable, IAuditableSetter
{
    private Agendamento()
    {
        StatusRaw = null!;
    }

    public Guid Id { get; private set; }

    public Guid FilialId { get; private set; }

    public Guid ClienteId { get; private set; }

    public Guid VeiculoId { get; private set; }

    public Guid? ResponsavelId { get; private set; }

    public Guid CriadoPor { get; private set; }

    public string StatusRaw { get; private set; }

    public StatusAgendamento Status => StatusAgendamentoExtensions.FromDbValue(StatusRaw);

    public DateTime Inicio { get; private set; }

    public DateTime Fim { get; private set; }

    public string? Observacoes { get; private set; }

    /// <summary>
    /// Gets soma das durações dos serviços do agendamento, em minutos. Total denormalizado
    /// para consulta de agenda sem N+1 (CHECK <c>ck_ag_duracao_total</c> &gt;= 0).
    /// </summary>
    public int DuracaoTotalMin { get; private set; }

    /// <summary>
    /// Gets soma dos preços aplicados dos serviços do agendamento. Total denormalizado
    /// (CHECK <c>ck_ag_valor_total</c> &gt;= 0).
    /// </summary>
    public decimal ValorTotal { get; private set; }

    public int Versao { get; private set; }

    /// <inheritdoc/>
    public DateTime CriadoEm { get; private set; }

    /// <inheritdoc/>
    public DateTime AtualizadoEm { get; private set; }

    public DateTime? CanceladoEm { get; private set; }

    public Guid? CanceladoPor { get; private set; }

    public string? MotivoCancelamento { get; private set; }

    public static Agendamento Criar(
        Guid id,
        Guid filialId,
        Guid clienteId,
        Guid veiculoId,
        Guid criadoPor,
        DateTime inicio,
        DateTime fim,
        Guid? responsavelId = null,
        string? observacoes = null,
        int duracaoTotalMin = 0,
        decimal valorTotal = 0m)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Id do agendamento não pode ser vazio.");
        }

        if (filialId == Guid.Empty)
        {
            throw new DomainException("Agendamento exige filial (RN010).");
        }

        if (clienteId == Guid.Empty)
        {
            throw new DomainException("Agendamento exige cliente.");
        }

        if (veiculoId == Guid.Empty)
        {
            throw new DomainException("Agendamento exige veículo.");
        }

        if (criadoPor == Guid.Empty)
        {
            throw new DomainException("Agendamento exige usuário criador.");
        }

        if (inicio >= fim)
        {
            throw new DomainException("Início do agendamento deve ser anterior ao fim.");
        }

        if (duracaoTotalMin < 0)
        {
            throw new DomainException("Duração total do agendamento não pode ser negativa.");
        }

        if (valorTotal < 0m)
        {
            throw new DomainException("Valor total do agendamento não pode ser negativo.");
        }

        var agora = DateTime.UtcNow;
        return new Agendamento
        {
            Id = id,
            FilialId = filialId,
            ClienteId = clienteId,
            VeiculoId = veiculoId,
            ResponsavelId = responsavelId,
            CriadoPor = criadoPor,
            StatusRaw = StatusAgendamento.Agendado.ToDbValue(),
            Inicio = DateTime.SpecifyKind(inicio, DateTimeKind.Utc),
            Fim = DateTime.SpecifyKind(fim, DateTimeKind.Utc),
            Observacoes = observacoes,
            DuracaoTotalMin = duracaoTotalMin,
            ValorTotal = valorTotal,
            Versao = 1,
            CriadoEm = agora,
            AtualizadoEm = agora,
        };
    }

    /// <summary>
    /// Define os totais denormalizados (RN006) a partir dos serviços agregados.
    /// Mantém o agregado consistente após a montagem dos <see cref="AgendamentoItem"/>.
    /// </summary>
    public void DefinirTotais(int duracaoTotalMin, decimal valorTotal)
    {
        if (duracaoTotalMin < 0)
        {
            throw new DomainException("Duração total do agendamento não pode ser negativa.");
        }

        if (valorTotal < 0m)
        {
            throw new DomainException("Valor total do agendamento não pode ser negativo.");
        }

        DuracaoTotalMin = duracaoTotalMin;
        ValorTotal = valorTotal;
    }

    public void Cancelar(string motivoCancelamento, Guid canceladoPor)
    {
        if (Status is StatusAgendamento.Finalizado)
        {
            throw new DomainException("Agendamento finalizado não pode ser cancelado.");
        }

        if (Status is StatusAgendamento.Cancelado)
        {
            throw new DomainException("Agendamento já cancelado não pode ser cancelado novamente.");
        }

        if (Status is StatusAgendamento.EmAndamento)
        {
            throw new DomainException("Agendamento em andamento não pode ser cancelado.");
        }

        if (string.IsNullOrWhiteSpace(motivoCancelamento))
        {
            throw new DomainException("Motivo do cancelamento é obrigatório.");
        }

        motivoCancelamento = motivoCancelamento.Trim();
        if (motivoCancelamento.Length is < 5 or > 500)
        {
            throw new DomainException("Motivo do cancelamento deve ter entre 5 e 500 caracteres.");
        }

        if (canceladoPor == Guid.Empty)
        {
            throw new DomainException("Usuário responsável pelo cancelamento é obrigatório.");
        }

        StatusRaw = StatusAgendamento.Cancelado.ToDbValue();
        MotivoCancelamento = motivoCancelamento;
        CanceladoPor = canceladoPor;
        CanceladoEm = DateTime.UtcNow;
        Versao++;
    }

    public void Finalizar()
    {
        if (Status is not StatusAgendamento.EmAndamento)
        {
            throw new DomainException("Apenas agendamentos com status 'em_andamento' podem ser finalizados.");
        }

        StatusRaw = StatusAgendamento.Finalizado.ToDbValue();
        Versao++;
    }

    public void Iniciar()
    {
        if (Status is not StatusAgendamento.Agendado)
        {
            throw new DomainException("Apenas agendamentos com status 'agendado' podem ser iniciados.");
        }

        StatusRaw = StatusAgendamento.EmAndamento.ToDbValue();
        Versao++;
    }

    public void Reagendar(DateTime inicio, DateTime fim)
    {
        GarantirEstadoEditavel();

        if (inicio >= fim)
        {
            throw new DomainException("Início do agendamento deve ser anterior ao fim.");
        }

        Inicio = DateTime.SpecifyKind(inicio, DateTimeKind.Utc);
        Fim = DateTime.SpecifyKind(fim, DateTimeKind.Utc);
        Versao++;
    }

    private void GarantirEstadoEditavel()
    {
        if (Status is StatusAgendamento.Finalizado)
        {
            throw new DomainException("Agendamento finalizado não pode ser editado.");
        }

        if (Status is StatusAgendamento.Cancelado)
        {
            throw new DomainException("Agendamento cancelado não pode ser editado.");
        }

        if (Status is StatusAgendamento.EmAndamento)
        {
            throw new DomainException("Agendamento no status atual não permite edição.");
        }
    }

    /// <inheritdoc/>
    void IAuditableSetter.SetCriadoEm(DateTime valor) => CriadoEm = valor;

    /// <inheritdoc/>
    void IAuditableSetter.SetAtualizadoEm(DateTime valor) => AtualizadoEm = valor;
}
