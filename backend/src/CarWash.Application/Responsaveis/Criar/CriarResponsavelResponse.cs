namespace CarWash.Application.Responsaveis.Criar;

public sealed class CriarResponsavelResponse
{
    public string Message { get; init; } = "Responsável cadastrado com sucesso.";

    public CriarResponsavelData Data { get; init; } = new();

    public string TraceId { get; init; } = string.Empty;
}

public sealed class CriarResponsavelData
{
    public Guid ResponsavelId { get; init; }

    public Guid ClienteTitularId { get; init; }

    public string Nome { get; init; } = string.Empty;

    public string Documento { get; init; } = string.Empty;

    public string? Telefone { get; init; }

    public string? Email { get; init; }

    public string? GrauVinculo { get; init; }
}
