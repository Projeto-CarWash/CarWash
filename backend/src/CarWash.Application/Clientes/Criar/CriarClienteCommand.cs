using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Clientes.Common;

namespace CarWash.Application.Clientes.Criar;

/// <summary>
/// Comando de cadastro de cliente (RF002 + RF003). CPF/CNPJ exclusivos,
/// celular obrigatório, endereço estruturado. <c>TraceId</c> e <c>UsuarioId</c>
/// são preenchidos pelo endpoint a partir do <see cref="HttpContext"/>.
/// </summary>
public sealed record CriarClienteCommand(
    string? Nome,
    DateOnly? DataNascimento,
    string? Cpf,
    string? Cnpj,
    string? Telefone,
    string? Celular,
    string? Email,
    EnderecoRequest? Endereco,
    string? Observacoes,
    string TraceId,
    Guid? UsuarioId) : ICommand<CriarClienteResponse>;
