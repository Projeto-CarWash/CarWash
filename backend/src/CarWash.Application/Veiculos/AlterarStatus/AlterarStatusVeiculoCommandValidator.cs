using FluentValidation;

namespace CarWash.Application.Veiculos.AlterarStatus;

public sealed class AlterarStatusVeiculoCommandValidator : AbstractValidator<AlterarStatusVeiculoCommand>
{
    public AlterarStatusVeiculoCommandValidator()
    {
        RuleFor(x => x.ClienteId)
            .NotEqual(Guid.Empty).WithMessage("Identificador do cliente é obrigatório.");

        RuleFor(x => x.VeiculoId)
            .NotEqual(Guid.Empty).WithMessage("Identificador do veículo é obrigatório.");

        RuleFor(x => x.Ativo)
            .NotNull().WithMessage("Campo 'ativo' é obrigatório.");
    }
}
