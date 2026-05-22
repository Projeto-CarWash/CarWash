namespace CarWash.Application.Agenda.Common;

/// <summary>
/// Formato de visualização da agenda (RF009). <see cref="Simples"/> devolve um
/// resumo compacto por evento; <see cref="Detalhado"/> devolve cliente, veículo
/// e serviços completos.
/// </summary>
public enum FormatoAgenda
{
    /// <summary>Resumo compacto (8 campos por evento).</summary>
    Simples,

    /// <summary>Visão completa com cliente, veículo e serviços.</summary>
    Detalhado,
}
