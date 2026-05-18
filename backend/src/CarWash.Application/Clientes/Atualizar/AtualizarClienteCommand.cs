using System.Text.Json;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Clientes.Common;

namespace CarWash.Application.Clientes.Atualizar;

/// <summary>
/// Comando de atualização cadastral de cliente (RF002 — campos editáveis).
/// CPF/CNPJ não são editáveis: presença no body é tolerada (warn) e ignorada.
/// </summary>
public sealed record AtualizarClienteCommand(
    Guid Id,
    string? Nome,
    DateOnly? DataNascimento,
    string? Telefone,
    string? Celular,
    string? Email,
    EnderecoRequest? Endereco,
    Dictionary<string, JsonElement>? CamposExtras,
    Guid? UsuarioId) : ICommand<ClienteResponse>;
