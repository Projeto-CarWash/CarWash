namespace CarWash.Domain.Entities;

public sealed class AgendamentoObservacao
{
    public Guid Id { get; private set; }

    public Guid AgendamentoId { get; private set; }

    public string Texto { get; private set; } = string.Empty;

    public bool Ativo { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }

    public Guid CriadoPor { get; private set; }

    public DateTimeOffset? AtualizadoEm { get; private set; }

    public Guid? AtualizadoPor { get; private set; }

    public DateTimeOffset? ExcluidoEm { get; private set; }

    public Guid? ExcluidoPor { get; private set; }

    private AgendamentoObservacao()
    {
    }

    private AgendamentoObservacao(
        Guid id,
        Guid agendamentoId,
        string texto,
        Guid criadoPor)
    {
        Id = id;
        AgendamentoId = agendamentoId;
        Texto = texto;
        CriadoPor = criadoPor;
        CriadoEm = DateTimeOffset.UtcNow;
        Ativo = true;
    }

    public static AgendamentoObservacao Criar(
        Guid agendamentoId,
        string texto,
        Guid criadoPor)
    {
        return new AgendamentoObservacao(
            Guid.NewGuid(),
            agendamentoId,
            texto,
            criadoPor);
    }

    public void AtualizarTexto(string texto, Guid atualizadoPor)
    {
        if (!Ativo)
        {
            throw new InvalidOperationException("A observação logística não pode ser alterada no estado atual.");
        }

        Texto = texto;
        AtualizadoPor = atualizadoPor;
        AtualizadoEm = DateTimeOffset.UtcNow;
    }

    public void Excluir(Guid excluidoPor)
    {
        if (!Ativo)
        {
            throw new InvalidOperationException("A observação logística não pode ser alterada no estado atual.");
        }

        Ativo = false;
        ExcluidoPor = excluidoPor;
        ExcluidoEm = DateTimeOffset.UtcNow;
    }
}
