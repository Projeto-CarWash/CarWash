using CarWash.Application.Clientes.Common;
using CarWash.Application.DTOs;

namespace CarWash.Application.Clientes.Criar;

/// <summary>
/// DTO de entrada do <c>POST /api/v1/clientes</c>. O <c>TraceId</c> e o
/// <c>UsuarioId</c> são preenchidos pelo endpoint — não pertencem ao body.
/// </summary>
public class CriarClienteRequest
{
    public string? Nome { get; set; }

    public DateOnly? DataNascimento { get; set; }

    public string? Cpf { get; set; }

    public string? Cnpj { get; set; }

    public string? Telefone { get; set; }

    public string? Celular { get; set; }

    public string? Email { get; set; }

    public EnderecoRequest? Endereco { get; set; }

    public List<CriarVeiculoRequest>? Veiculos { get; set; }

    public string? Observacoes { get; set; }
}
