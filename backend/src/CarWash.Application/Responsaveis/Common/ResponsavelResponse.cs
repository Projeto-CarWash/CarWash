using CarWash.Domain.Entities;

namespace CarWash.Application.Responsaveis.Common;

public class ResponsavelResponse
{
    public Guid Id { get; set; }

    public Guid ClienteTitularId { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Documento { get; set; } = string.Empty;

    public string? Telefone { get; set; }

    public string? Email { get; set; }

    public string? GrauVinculo { get; set; }

    public bool Ativo { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime AtualizadoEm { get; set; }

    public static ResponsavelResponse FromEntity(Responsavel responsavel)
    {
        ArgumentNullException.ThrowIfNull(responsavel);
        return new ResponsavelResponse
        {
            Id = responsavel.Id,
            ClienteTitularId = responsavel.ClienteTitularId,
            Nome = responsavel.Nome,
            Documento = responsavel.Documento,
            Telefone = responsavel.Telefone,
            Email = responsavel.Email,
            GrauVinculo = responsavel.GrauVinculo,
            Ativo = responsavel.Ativo,
            CriadoEm = responsavel.CriadoEm,
            AtualizadoEm = responsavel.AtualizadoEm,
        };
    }
}
