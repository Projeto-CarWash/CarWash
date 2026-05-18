using CarWash.Domain.Entities;

namespace CarWash.Application.Clientes.Common;

/// <summary>
/// Representação compacta usada na listagem paginada.
/// </summary>
public class ClienteResumoResponse
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Cpf { get; set; }

    public string? Cnpj { get; set; }

    public string Celular { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string Cidade { get; set; } = string.Empty;

    public string Uf { get; set; } = string.Empty;

    public bool Ativo { get; set; }

    public DateTime CriadoEm { get; set; }

    public static ClienteResumoResponse FromEntity(Cliente cliente)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        return new ClienteResumoResponse
        {
            Id = cliente.Id,
            Nome = cliente.Nome,
            Cpf = cliente.Cpf,
            Cnpj = cliente.Cnpj,
            Celular = cliente.Celular,
            Email = cliente.Email,
            Cidade = cliente.EnderecoCidade,
            Uf = cliente.EnderecoUf,
            Ativo = cliente.Ativo,
            CriadoEm = cliente.CriadoEm,
        };
    }
}
