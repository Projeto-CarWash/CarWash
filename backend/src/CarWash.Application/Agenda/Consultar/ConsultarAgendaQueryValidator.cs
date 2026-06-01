using System.Globalization;
using CarWash.Application.Agenda.Common;
using FluentValidation;

namespace CarWash.Application.Agenda.Consultar;

/// <summary>
/// Validador estrutural da consulta de agenda (RF009). Recebe os parâmetros crus
/// (<see cref="string"/>) e garante: formato válido, datas ISO-8601 parseáveis
/// com <c>inicio &lt; fim</c> e janela &lt;= 31 dias, GUIDs sintaticamente válidos
/// e status pertencente aos 4 valores do contrato.
/// <para>
/// Observação (ADR 0004 — L1): <c>EM_ANDAMENTO</c> é aceito como filtro válido —
/// não causa 400. Como não há status persistido correspondente, o handler
/// curto-circuita para <c>data: []</c>.
/// </para>
/// </summary>
public sealed class ConsultarAgendaQueryValidator : AbstractValidator<ConsultarAgendaQuery>
{
    /// <summary>Janela máxima de consulta, em dias (RF009).</summary>
    public const int JanelaMaximaDias = 31;

    public ConsultarAgendaQueryValidator()
    {
        RuleFor(x => x.Formato)
            .Must(EhFormatoValido)
            .WithMessage("Formato é obrigatório e deve ser 'simples' ou 'detalhado'.");

        RuleFor(x => x.Inicio)
            .Must(valor => TentarParsearData(valor, out _))
            .WithMessage("Início é obrigatório e deve estar em formato ISO-8601 UTC.");

        RuleFor(x => x.Fim)
            .Cascade(CascadeMode.Stop)
            .Must(valor => TentarParsearData(valor, out _))
            .WithMessage("Fim é obrigatório e deve estar em formato ISO-8601 UTC.")
            .Must((query, _) => InicioAnteriorAoFim(query))
            .When(x => TentarParsearData(x.Inicio, out _), ApplyConditionTo.CurrentValidator)
            .WithMessage("O início deve ser anterior ao fim.")
            .Must((query, _) => JanelaDentroDoLimite(query))
            .When(x => TentarParsearData(x.Inicio, out _), ApplyConditionTo.CurrentValidator)
            .WithMessage($"A janela de consulta não pode exceder {JanelaMaximaDias} dias.");

        RuleFor(x => x.FilialId)
            .Must(valor => Guid.TryParse(valor, out _))
            .WithMessage("Filial é obrigatória e deve ser um identificador válido.");

        RuleFor(x => x.ClienteId)
            .Must(valor => Guid.TryParse(valor, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.ClienteId))
            .WithMessage("Cliente informado é inválido.");

        RuleFor(x => x.UsuarioId)
            .Must(valor => Guid.TryParse(valor, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.UsuarioId))
            .WithMessage("Responsável informado é inválido.");

        RuleFor(x => x.Status)
            .Must(StatusAgendaMapper.EhStatusApiValido)
            .When(x => !string.IsNullOrWhiteSpace(x.Status))
            .WithMessage("Status informado é inválido.");
    }

    /// <summary>
    /// Parseia uma data ISO-8601 com semântica de roundtrip e a converte para UTC.
    /// Centralizado aqui para que o handler reaproveite o mesmo parse após a
    /// validação ter garantido o sucesso.
    /// </summary>
    /// <returns></returns>
    public static bool TentarParsearData(string? valor, out DateTime utc)
    {
        utc = default;
        if (string.IsNullOrWhiteSpace(valor))
        {
            return false;
        }

        if (!DateTimeOffset.TryParse(
                valor,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var offset))
        {
            return false;
        }

        utc = offset.UtcDateTime;
        return true;
    }

    private static bool EhFormatoValido(string? formato)
    {
        if (string.IsNullOrWhiteSpace(formato))
        {
            return false;
        }

        string normalizado = formato.Trim().ToLowerInvariant();
        return normalizado is "simples" or "detalhado";
    }

    private static bool InicioAnteriorAoFim(ConsultarAgendaQuery query)
    {
        if (!TentarParsearData(query.Inicio, out var inicio) || !TentarParsearData(query.Fim, out var fim))
        {
            return true;
        }

        return inicio < fim;
    }

    private static bool JanelaDentroDoLimite(ConsultarAgendaQuery query)
    {
        if (!TentarParsearData(query.Inicio, out var inicio) || !TentarParsearData(query.Fim, out var fim))
        {
            return true;
        }

        return (fim - inicio) <= TimeSpan.FromDays(JanelaMaximaDias);
    }
}
