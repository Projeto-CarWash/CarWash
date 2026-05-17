namespace CarWash.Application.DTOs.Clientes;

public class CreateClienteRequest
{
    public string? Nome { get; set; }

    public DateOnly? DataNascimento { get; set; }

    public string? Cpf { get; set; }

    public string? Cnpj { get; set; }

    public string? Telefone { get; set; }

    public string? Celular { get; set; }

    public string? Email { get; set; }

    public EnderecoRequest? Endereco { get; set; }
}

public class EnderecoRequest
{
    public string? Cep { get; set; }

    public string? Logradouro { get; set; }

    public string? Numero { get; set; }

    public string? Complemento { get; set; }

    public string? Bairro { get; set; }

    public string? Cidade { get; set; }

    public string? Uf { get; set; }
}

public class UpdateClienteRequest
{
    public string? Nome { get; set; }

    public DateOnly? DataNascimento { get; set; }

    public string? Telefone { get; set; }

    public string? Celular { get; set; }

    public string? Email { get; set; }

    public EnderecoRequest? Endereco { get; set; }
}

public class AlterarStatusClienteRequest
{
    public bool Ativo { get; set; }
}
