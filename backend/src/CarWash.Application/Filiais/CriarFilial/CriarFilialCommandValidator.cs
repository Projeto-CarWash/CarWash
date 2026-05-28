using CarWash.Domain.Entities;
using FluentValidation;

namespace CarWash.Application.Filiais.CriarFilial;

/// <summary>
/// Valida o command de criação de filial. Mensagem da faixa de células é
/// EXATA conforme o card do RF018 — testes E2E dependem dessa string.
/// </summary>
public sealed class CriarFilialCommandValidator : AbstractValidator<CriarFilialCommand>
{
    public const string MensagemFaixa =
        "Valor de células ativas inválido. Informe um número inteiro entre 1 e 100.";

    public const string MensagemNomeObrigatorio = "Nome da filial é obrigatório.";
    public const string MensagemNomeMaximo = "Nome da filial excede 120 caracteres.";
    public const string MensagemCelulasObrigatorio = "Campo 'celulasAtivas' é obrigatório.";
    public const string MensagemTimezoneMaximo = "Timezone excede 64 caracteres.";

    public CriarFilialCommandValidator()
    {
        RuleFor(x => x.Nome)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(MensagemNomeObrigatorio)
            .MaximumLength(120).WithMessage(MensagemNomeMaximo);

        RuleFor(x => x.CelulasAtivas)
            .NotNull().WithMessage(MensagemCelulasObrigatorio)
            .Must(v => v is >= Filial.MinCelulasAtivas and <= Filial.MaxCelulasAtivas)
                .WithMessage(MensagemFaixa);

        RuleFor(x => x.Timezone)
            .MaximumLength(64).WithMessage(MensagemTimezoneMaximo)
            .When(x => !string.IsNullOrWhiteSpace(x.Timezone));
    }
}
