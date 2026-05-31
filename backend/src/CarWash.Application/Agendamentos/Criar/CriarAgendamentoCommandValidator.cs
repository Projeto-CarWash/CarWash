using FluentValidation;

namespace CarWash.Application.Agendamentos.Criar;

/// <summary>
/// Validador estrutural do RF007/RF019/RF020. Garante filial (RF019/RN010/CA007),
/// veículo, ao menos um serviço sem duplicatas e início futuro. As regras de
/// estado (recursos ativos, conflito RN011) são verificadas no handler/banco.
/// </summary>
public sealed class CriarAgendamentoCommandValidator : AbstractValidator<CriarAgendamentoCommand>
{
    public CriarAgendamentoCommandValidator()
    {
        RuleFor(x => x.FilialId)
            .NotEmpty().WithMessage("Filial é obrigatória para o agendamento (RF019).");

        RuleFor(x => x.ClienteId)
            .NotEmpty().WithMessage("Cliente é obrigatório para o agendamento.");

        RuleFor(x => x.VeiculoId)
            .NotEmpty().WithMessage("Veículo é obrigatório para o agendamento.");

        RuleFor(x => x.ResponsavelId)
            .Must(id => id != Guid.Empty)
            .When(x => x.ResponsavelId.HasValue)
            .WithMessage("Responsável informado é inválido.");

        RuleFor(x => x.Inicio)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Data e hora de início são obrigatórias.")
            .Must(inicio => inicio!.Value.ToUniversalTime() > DateTime.UtcNow)
            .When(x => x.Inicio.HasValue, ApplyConditionTo.CurrentValidator)
            .WithMessage("O início do agendamento deve ser uma data futura.");

        RuleFor(x => x.ServicoIds)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Informe ao menos um serviço para o agendamento.")
            .Must(servicos => servicos!.Count > 0)
            .When(x => x.ServicoIds is not null, ApplyConditionTo.CurrentValidator)
            .WithMessage("Informe ao menos um serviço para o agendamento.")
            .Must(servicos => servicos!.All(id => id != Guid.Empty))
            .When(x => x.ServicoIds is { Count: > 0 }, ApplyConditionTo.CurrentValidator)
            .WithMessage("Há serviço inválido na lista informada.")
            .Must(SemDuplicatas)
            .When(x => x.ServicoIds is { Count: > 0 }, ApplyConditionTo.CurrentValidator)
            .WithMessage("Não é permitido repetir o mesmo serviço no agendamento (CA007).");

        RuleFor(x => x.Observacoes)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Observacoes))
            .WithMessage("Observações devem ter no máximo 500 caracteres.");
    }

    /// <summary>
    /// Verifica que não há serviço repetido na lista (CA007). A condição
    /// <c>.When</c> garante que a lista é não-nula quando esta regra executa.
    /// </summary>
    private static bool SemDuplicatas(IReadOnlyList<Guid>? servicos)
    {
        if (servicos is null)
        {
            return true;
        }

        return servicos.Distinct().Count() == servicos.Count;
    }
}
