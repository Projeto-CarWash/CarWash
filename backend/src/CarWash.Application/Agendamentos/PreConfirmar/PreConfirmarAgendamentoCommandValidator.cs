using CarWash.Application.Agendamentos.Common;
using FluentValidation;

namespace CarWash.Application.Agendamentos.PreConfirmar;

/// <summary>
/// Validador estrutural da pré-confirmação (RF015). Mesmas regras do RF007/RF024:
/// filial (RF019/RN010/CA007), cliente, veículo, responsável obrigatório (RF024),
/// início futuro e ao menos um serviço sem duplicatas. Estado (recursos ativos,
/// conflito RN011) é verificado no handler.
/// </summary>
public sealed class PreConfirmarAgendamentoCommandValidator : AbstractValidator<PreConfirmarAgendamentoCommand>
{
    public PreConfirmarAgendamentoCommandValidator()
    {
        // RF019: filial obrigatória — mensagem do card 142.
        RuleFor(x => x.FilialId)
            .NotEmpty().WithMessage(MensagensFilialAgendamento.Obrigatoria);

        RuleFor(x => x.ClienteId)
            .NotEmpty().WithMessage("Cliente é obrigatório para o agendamento.");

        RuleFor(x => x.VeiculoId)
            .NotEmpty().WithMessage("Veículo é obrigatório para o agendamento.");

        RuleFor(x => x.ResponsavelId)
            .NotEmpty().WithMessage("Selecione um responsável para prosseguir.");

        RuleFor(x => x.Inicio)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Data e hora de início são obrigatórias.")
            .Must(inicio => inicio!.Value.ToUniversalTime() >= DateTime.UtcNow.AddMinutes(-1))
            .When(x => x.Inicio.HasValue, ApplyConditionTo.CurrentValidator)
            .WithMessage("A data/hora de início não pode estar no passado.");

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

    private static bool SemDuplicatas(IReadOnlyList<Guid>? servicos)
    {
        if (servicos is null)
        {
            return true;
        }

        return servicos.Distinct().Count() == servicos.Count;
    }
}
