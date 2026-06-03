using CarWash.Application.Common;
using FluentValidation;

namespace CarWash.Application.Agendamentos.Observacoes.Criar;

public sealed class CriarObservacaoAgendamentoCommandValidator
    : AbstractValidator<CriarObservacaoAgendamentoCommand>
{
    public CriarObservacaoAgendamentoCommandValidator()
    {
        RuleFor(x => x.AgendamentoId)
            .NotEmpty()
            .WithMessage("Agendamento é obrigatório.");

        RuleFor(x => x.UsuarioId)
            .NotEmpty()
            .WithMessage("Usuário autenticado é obrigatório.");

        RuleFor(x => x.Texto)
            .Cascade(CascadeMode.Stop)
            .Must(texto => !string.IsNullOrWhiteSpace(InputNormalizer.SanitizeTextOrNull(texto)))
            .WithMessage("Texto da observação é obrigatório.")
            .Must(texto => InputNormalizer.SanitizeTextOrNull(texto)!.Length >= 3)
            .WithMessage("Texto da observação deve ter no mínimo 3 caracteres.")
            .Must(texto => InputNormalizer.SanitizeTextOrNull(texto)!.Length <= 1000)
            .WithMessage("Texto da observação deve ter no máximo 1000 caracteres.");
    }
}
