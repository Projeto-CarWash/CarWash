namespace CarWash.Application.Responsaveis.Criar;

public class CriarResponsavelResponse
{
    public Guid Id { get; set; }

    public Guid ClienteTitularId { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Documento { get; set; } = string.Empty;

    public string? Telefone { get; set; }

    public string? Email { get; set; }

    public string? GrauVinculo { get; set; }

    public string Mensagem { get; set; } = "Responsável cadastrado com sucesso.";

    public string TraceId { get; set; } = string.Empty;
}
