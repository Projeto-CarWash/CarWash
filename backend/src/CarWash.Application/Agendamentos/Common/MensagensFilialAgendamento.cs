namespace CarWash.Application.Agendamentos.Common;

/// <summary>
/// Mensagens HTTP exatas do card 142 (RF019) relacionadas à filial obrigatória no
/// agendamento. Centralizadas para evitar string mágica e facilitar o teste.
/// </summary>
public static class MensagensFilialAgendamento
{
    /// <summary>
    /// 400 (presença/vazio) — usada nos três validators (RF019). Cobre tanto a
    /// ausência quanto <c>Guid.Empty</c>.
    /// </summary>
    public const string Obrigatoria = "Selecione uma filial válida para prosseguir.";

    /// <summary>
    /// 404 (inexistente) — usada em <c>GarantirFilialAsync</c>. Mesma frase já
    /// consolidada no RF018 (<c>AlterarCelulasAtivasHandler.MensagemNaoEncontrado</c>).
    /// </summary>
    public const string NaoEncontrada = "Filial não encontrada.";

    /// <summary>
    /// 409 (inativa) — usada em <see cref="FilialInativaException"/>.
    /// </summary>
    public const string Inativa =
        "A filial selecionada está inativa e não pode receber novos agendamentos.";
}
