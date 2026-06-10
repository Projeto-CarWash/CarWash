using CarWash.Application.Common;
using FluentValidation;

namespace CarWash.Application.Agendamentos.Observacoes.Atualizar;

public sealed class AtualizarObservacaoAgendamentoCommandValidator
    : AbstractValidator<AtualizarObservacaoAgendamentoCommand>
{
    public AtualizarObservacaoAgendamentoCommandValidator()
    {
        RuleFor(x => x.AgendamentoId)
            .NotEmpty()
            .WithMessage("Agendamento é obrigatório.");

        RuleFor(x => x.ObservacaoId)
            .NotEmpty()
            .WithMessage("Observação logística é obrigatória.");

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
