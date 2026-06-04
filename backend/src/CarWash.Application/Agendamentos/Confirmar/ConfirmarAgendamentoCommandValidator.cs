using FluentValidation;

namespace CarWash.Application.Agendamentos.Confirmar;

/// <summary>
/// Validador estrutural da confirmação (RF015 — etapa 2). Além das regras do
/// RF007/RF024 (filial/cliente/veículo/responsável/início/serviços), exige a
/// confirmação explícita, o <c>tokenConfirmacao</c> e a <c>idempotencyKey</c>.
/// As mensagens de <c>confirmar</c> e <c>tokenConfirmacao</c> são as do contrato
/// do card 133.
/// </summary>
public sealed class ConfirmarAgendamentoCommandValidator : AbstractValidator<ConfirmarAgendamentoCommand>
{
    public const string MensagemConfirmacaoObrigatoria =
        "Confirmação explícita é obrigatória para concluir o agendamento.";

    public const string MensagemTokenObrigatorio =
        "Token de confirmação é obrigatório.";

    public ConfirmarAgendamentoCommandValidator()
    {
        RuleFor(x => x.FilialId)
            .NotEmpty().WithMessage("Filial é obrigatória para o agendamento (RF019).");

        RuleFor(x => x.ClienteId)
            .NotEmpty().WithMessage("Cliente é obrigatório para o agendamento.");

        RuleFor(x => x.VeiculoId)
            .NotEmpty().WithMessage("Veículo é obrigatório para o agendamento.");

        RuleFor(x => x.ResponsavelId)
            .NotEmpty().WithMessage("Selecione um responsável para prosseguir.");

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

        // RF015: confirmar ausente OU false → 400 com a chave `confirmar`.
        RuleFor(x => x.Confirmar)
            .Must(c => c == true)
            .WithMessage(MensagemConfirmacaoObrigatoria);

        // RF015: token ausente/vazio → 400 com a chave `tokenConfirmacao`.
        // O token presente mas inválido (assinatura/formato) é tratado no
        // ITokenConfirmacaoService — não dá para detectar isso aqui.
        RuleFor(x => x.TokenConfirmacao)
            .NotEmpty().WithMessage(MensagemTokenObrigatorio);

        RuleFor(x => x.IdempotencyKey)
            .NotNull().WithMessage("Chave de idempotência é obrigatória.")
            .Must(key => key != Guid.Empty)
            .When(x => x.IdempotencyKey.HasValue)
            .WithMessage("Chave de idempotência informada é inválida.");
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
