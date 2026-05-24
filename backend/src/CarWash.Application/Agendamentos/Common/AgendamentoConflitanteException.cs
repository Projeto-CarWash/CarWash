using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Agendamentos.Common;

/// <summary>
/// Indica que o veículo já possui um agendamento ativo que se sobrepõe à janela
/// solicitada — RN011 / RF020 / CA006. O conflito vale para a mesma filial ou
/// filiais diferentes. Disparada pelo <see cref="Persistence.IAgendamentoRepository"/>
/// quando o pré-check falha ou quando o banco rejeita a gravação via a constraint
/// EXCLUDE <c>ex_ag_veiculo_janela</c> (race condition). Herda de
/// <see cref="ConflictException"/> para reaproveitar o status 409 + slug no
/// middleware global.
/// </summary>
#pragma warning disable RCS1194 // Exceção semântica; construtores cobrem os usos reais.
public sealed class AgendamentoConflitanteException : ConflictException
#pragma warning restore RCS1194
{
    public const string MensagemPadrao =
        "O veículo já possui um agendamento neste horário. Escolha outro horário ou veículo.";

    /// <summary>
    /// Mensagem específica do RF015: conflito detectado no momento da confirmação,
    /// indicando ao usuário que o horário foi tomado entre a prévia e a confirmação.
    /// </summary>
    public const string MensagemConfirmacao =
        "O horário não está mais disponível. Atualize e confirme novamente.";

    public const string SlugPadrao = "agendamento-conflito-veiculo";

    public AgendamentoConflitanteException()
        : base(MensagemPadrao, SlugPadrao)
    {
    }

    public AgendamentoConflitanteException(Exception innerException)
        : base(MensagemPadrao, SlugPadrao, innerException)
    {
    }

    public AgendamentoConflitanteException(string mensagem)
        : base(mensagem, SlugPadrao)
    {
    }

    public AgendamentoConflitanteException(string mensagem, Exception innerException)
        : base(mensagem, SlugPadrao, innerException)
    {
    }
}
