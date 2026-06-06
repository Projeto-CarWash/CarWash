using FluentValidation;

namespace CarWash.Application.Dashboard.Metricas.Consultar;

public sealed class ConsultarDashboardMetricasQueryValidator
    : AbstractValidator<ConsultarDashboardMetricasQuery>
{
    private static readonly string[] StatusPermitidos =
    [
        "AGENDADO",
        "EM_ANDAMENTO",
        "CONCLUIDO",
        "CANCELADO",
        "FINALIZADO",
    ];

    public ConsultarDashboardMetricasQueryValidator()
    {
        RuleFor(x => x.DataInicio)
            .NotEmpty()
            .WithMessage("dataInicio é obrigatório.");

        RuleFor(x => x.DataFim)
            .NotEmpty()
            .WithMessage("dataFim é obrigatório.");

        RuleFor(x => x)
            .Must(x => x.DataInicio <= x.DataFim)
            .WithMessage("dataInicio deve ser menor ou igual a dataFim.");

        RuleFor(x => x)
            .Must(x => (x.DataFim - x.DataInicio).TotalDays <= 365)
            .WithMessage("O intervalo máximo permitido é de 365 dias.");

        RuleFor(x => x.Status)
            .Must(status => string.IsNullOrWhiteSpace(status)
                || StatusPermitidos.Contains(status.Trim().ToUpperInvariant()))
            .WithMessage("Status inválido.");
    }
}
