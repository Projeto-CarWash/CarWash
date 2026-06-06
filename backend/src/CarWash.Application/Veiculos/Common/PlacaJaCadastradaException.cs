using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Veiculos.Common;

/// <summary>
/// RN011 / RF005 / RF021 — placa única global. Lançada quando a constraint
/// <c>uk_veiculos_placa</c> é violada (pré-check ou race condition no banco).
/// Mapeada para 409 Conflict + ProblemDetails pelo middleware global.
/// </summary>
#pragma warning disable RCS1194 // Exceção semântica — construtores cobrem os usos reais.
public sealed class PlacaJaCadastradaException : ConflictException
#pragma warning restore RCS1194
{
    public const string SlugPadrao = "placa-ja-cadastrada";

    public PlacaJaCadastradaException()
        : base("Já existe um veículo cadastrado com a placa informada.", SlugPadrao)
    {
    }

    public PlacaJaCadastradaException(Exception innerException)
        : base("Já existe um veículo cadastrado com a placa informada.", SlugPadrao, innerException)
    {
    }
}
