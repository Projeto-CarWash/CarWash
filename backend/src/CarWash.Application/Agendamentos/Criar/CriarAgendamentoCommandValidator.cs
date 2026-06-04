using FluentValidation;

namespace CarWash.Application.Agendamentos.Criar;

public sealed class CriarAgendamentoCommandValidator : AbstractValidator<CriarAgendamentoCommand>
{
    public CriarAgendamentoCommandValidator()
    {
        RuleFor(x => x.FilialId)
            .NotEqual(Guid.Empty).WithMessage("O ID da filial é obrigatório e deve ser um UUID válido.");

        RuleFor(x => x.ClienteId)
            .NotEqual(Guid.Empty).WithMessage("O ID do cliente é obrigatório e deve ser um UUID válido.");

    RuleFor(x => x.VeiculoId)
        .NotEqual(Guid.Empty).WithMessage("O ID do veículo é obrigatório e deve ser um UUID válido.");

    RuleFor(x => x.ResponsavelId)
        .NotEqual(Guid.Empty).WithMessage("O ID do responsável deve ser um UUID válido.")
        .When(x => x.ResponsavelId.HasValue);

        RuleFor(x => x.Inicio)
            .NotEqual(default(DateTime)).WithMessage("A data/hora de início é obrigatória (ISO-8601 UTC).");

        RuleFor(x => x.ServicoIds!)
            .NotNull().WithMessage("A lista de serviços é obrigatória.")
            .Must(ids => ids.Count >= 1).WithMessage("Informe pelo menos um serviço.")
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Serviços duplicados não são permitidos.");

        RuleForEach(x => x.ServicoIds!)
            .NotEqual(Guid.Empty).WithMessage("ID do serviço inválido.")
            .When(x => x.ServicoIds is not null);

        RuleFor(x => x.Observacoes)
            .MaximumLength(1000).WithMessage("Observações devem ter no máximo 1000 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Observacoes));
    }
}
