using CarWash.Domain.Common;
using CarWash.Domain.Enums;

namespace CarWash.Domain.Entities;

/// <summary>
/// Agenda operacional. Carrega as regras críticas RN004/RN006/RN010/RN011.
/// Concorrência otimista por <c>Versao</c> (decisão P15).
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

    public int Versao { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public DateTime AtualizadoEm { get; private set; }

    public static Agendamento Criar(
        Guid id,
        Guid filialId,
        Guid clienteId,
        Guid veiculoId,
        Guid criadoPor,
        DateTime inicio,
        DateTime fim,
        Guid? responsavelId = null,
        string? observacoes = null)
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
            Versao = 1,
            CriadoEm = agora,
            AtualizadoEm = agora,
        };
    }

    public void Cancelar()
    {
        GarantirEstadoEditavel();
        StatusRaw = StatusAgendamento.Cancelado.ToDbValue();
        Versao++;
    }

    public void Finalizar()
    {
        GarantirEstadoEditavel();
        StatusRaw = StatusAgendamento.Finalizado.ToDbValue();
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
            throw new DomainException("Agendamento finalizado não pode ser alterado (RN004).");
        }

        if (Status is StatusAgendamento.Cancelado)
        {
            throw new DomainException("Agendamento cancelado não pode ser alterado.");
        }
    }

    void IAuditableSetter.SetCriadoEm(DateTime valor) => CriadoEm = valor;

    void IAuditableSetter.SetAtualizadoEm(DateTime valor) => AtualizadoEm = valor;
}
