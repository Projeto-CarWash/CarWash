namespace CarWash.Application.DTOs.Clientes;

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

    public bool Ativo { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime AtualizadoEm { get; set; }
}

public class EnderecoResponse
{
    public string Cep { get; set; } = string.Empty;

    public string Logradouro { get; set; } = string.Empty;

    public string Numero { get; set; } = string.Empty;

    public string? Complemento { get; set; }

    public string Bairro { get; set; } = string.Empty;

    public string Cidade { get; set; } = string.Empty;

    public string Uf { get; set; } = string.Empty;
}

public class ListaClientesResponse
{
    public IReadOnlyList<ClienteResumoResponse> Itens { get; set; } = [];

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanhoPagina { get; set; }
}

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
}
