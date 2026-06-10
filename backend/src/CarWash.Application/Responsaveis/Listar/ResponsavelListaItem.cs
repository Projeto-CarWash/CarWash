namespace CarWash.Application.Responsaveis.Listar;

/// <summary>
/// Item da listagem de responsáveis de um cliente. O campo <c>Id</c> é o
/// identificador do responsável (consumido pelo frontend como <c>id</c>).
/// </summary>
public sealed class ResponsavelListaItem
{
    public Guid Id { get; init; }

    public string Nome { get; init; } = string.Empty;

    public string Documento { get; init; } = string.Empty;

    public string? Telefone { get; init; }

    public string? Email { get; init; }

    public string GrauVinculo { get; init; } = string.Empty;

    public bool Ativo { get; init; }

    public DateTime CriadoEm { get; init; }
}
