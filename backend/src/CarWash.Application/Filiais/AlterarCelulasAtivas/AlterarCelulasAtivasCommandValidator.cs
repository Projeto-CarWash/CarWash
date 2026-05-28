using CarWash.Domain.Entities;
using FluentValidation;

namespace CarWash.Application.Filiais.AlterarCelulasAtivas;

/// <summary>
/// Valida o command de alteração de células ativas. Mensagem da faixa é EXATA
/// conforme o card do RF018 — testes E2E dependem dessa string.
/// </summary>
public sealed class AlterarCelulasAtivasCommandValidator : AbstractValidator<AlterarCelulasAtivasCommand>
{
    public const string MensagemFaixa =
        "Valor de células ativas inválido. Informe um número inteiro entre 1 e 100.";

    public const string MensagemFilialIdInvalido = "Identificador da filial é obrigatório.";
    public const string MensagemCelulasObrigatorio = "Campo 'celulasAtivas' é obrigatório.";

    public AlterarCelulasAtivasCommandValidator()
    {
        RuleFor(x => x.FilialId)
            .NotEqual(Guid.Empty).WithMessage(MensagemFilialIdInvalido);

        RuleFor(x => x.CelulasAtivas)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(MensagemCelulasObrigatorio)
            .Must(v => v is >= Filial.MinCelulasAtivas and <= Filial.MaxCelulasAtivas)
                .WithMessage(MensagemFaixa);
    }
}
