using FluentValidation;

namespace CarWash.Application.Clientes.HistoricoAtendimentos.Consultar;

public sealed class ConsultarHistoricoAtendimentosClienteQueryValidator
    : AbstractValidator<ConsultarHistoricoAtendimentosClienteQuery>
{
    private static readonly string[] StatusPermitidos =
    [
        "AGENDADO",
        "EM_ANDAMENTO",
        "CONCLUIDO",
        "CANCELADO",
    ];

    public ConsultarHistoricoAtendimentosClienteQueryValidator()
    {
        RuleFor(x => x.ClienteId)
            .NotEmpty()
            .WithMessage("Cliente é obrigatório.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page deve ser maior ou igual a 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize deve estar entre 1 e 100.");

        RuleFor(x => x.UltimosDias)
            .GreaterThan(0)
            .When(x => x.UltimosDias.HasValue)
            .WithMessage("UltimosDias deve ser inteiro positivo.");

        RuleFor(x => x)
            .Must(x => !(x.UltimosDias.HasValue && (x.DataInicio.HasValue || x.DataFim.HasValue)))
            .WithMessage("Não é permitido combinar ultimosDias com dataInicio/dataFim.");

        RuleFor(x => x)
            .Must(x => (!x.DataInicio.HasValue && !x.DataFim.HasValue)
                || (x.DataInicio.HasValue && x.DataFim.HasValue))
            .WithMessage("Se dataInicio for informado, dataFim também deve ser informado.");

        RuleFor(x => x)
            .Must(x => !x.DataInicio.HasValue
                || !x.DataFim.HasValue
                || x.DataInicio.Value <= x.DataFim.Value)
            .WithMessage("dataInicio deve ser menor ou igual a dataFim.");

        RuleFor(x => x)
            .Must(x =>
            {
                if (!x.DataInicio.HasValue || !x.DataFim.HasValue)
                {
                    return true;
                }

                return (x.DataFim.Value - x.DataInicio.Value).TotalDays <= 365;
            })
            .WithMessage("A janela máxima de consulta é de 365 dias.");

        RuleFor(x => x.Status)
            .Must(status => string.IsNullOrWhiteSpace(status)
                || StatusPermitidos.Contains(status.Trim().ToUpperInvariant()))
            .WithMessage("Status inválido. Valores aceitos: AGENDADO, EM_ANDAMENTO, CONCLUIDO, CANCELADO.");
    }
}
