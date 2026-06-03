using CarWash.Application.Responsaveis.Common;
using CarWash.Domain.Entities;

namespace CarWash.Application.Clientes.Common;

/// <summary>
/// DTO de saída de cliente (GET / PUT / PATCH de status).
/// </summary>
public class ClienteResponse
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public DateOnly DataNascimento { get; set; }

    public string? Cpf { get; set; }

    public string? Cnpj { get; set; }

    public string? Telefone { get; set; }

    public string Celular { get; set; } = string.Empty;

    public string? Email { get; set; }

    public EnderecoResponse Endereco { get; set; } = null!;

    public string? Observacoes { get; set; }

    public bool Ativo { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime AtualizadoEm { get; set; }

    public List<ResponsavelResponse> Responsaveis { get; set; } = [];

    public static ClienteResponse FromEntity(Cliente cliente)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        return new ClienteResponse
        {
            Id = cliente.Id,
            Nome = cliente.Nome,
            DataNascimento = cliente.DataNascimento,
            Cpf = cliente.Cpf,
            Cnpj = cliente.Cnpj,
            Telefone = cliente.Telefone,
            Celular = cliente.Celular,
            Email = cliente.Email,
            Endereco = new EnderecoResponse
            {
                Cep = cliente.EnderecoCep,
                Logradouro = cliente.EnderecoLogradouro,
                Numero = cliente.EnderecoNumero,
                Complemento = cliente.EnderecoComplemento,
                Bairro = cliente.EnderecoBairro,
                Cidade = cliente.EnderecoCidade,
                Uf = cliente.EnderecoUf,
            },
            Observacoes = cliente.Observacoes,
            Ativo = cliente.Ativo,
            CriadoEm = cliente.CriadoEm,
            AtualizadoEm = cliente.AtualizadoEm,
        };
    }

    public List<ClienteVeiculoResponse> Veiculos { get; set; } = new();

    public class ClienteVeiculoResponse
    {
        public Guid Id { get; set; }
        public string Placa { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public string Fabricante { get; set; } = string.Empty;
        public string Cor { get; set; } = string.Empty;
    }
}

