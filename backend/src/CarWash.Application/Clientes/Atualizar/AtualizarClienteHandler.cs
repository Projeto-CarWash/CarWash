using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Clientes.Common;
using CarWash.Application.Clientes.Persistence;
using CarWash.Application.Common;
using CarWash.Application.Common.Exceptions;
using CarWash.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Clientes.Atualizar;

/// <summary>
/// Use case de atualização cadastral. <c>PUT /api/v1/clientes/{id}</c>.
/// CPF/CNPJ no body são tolerados como warn (Opção B do GAP-CW-CLI-PUT-CPF).
/// </summary>
public sealed class AtualizarClienteHandler : ICommandHandler<AtualizarClienteCommand, ClienteResponse>
{
    private readonly IClienteRepository _repositorio;
    private readonly ILogger<AtualizarClienteHandler> _log;

    public AtualizarClienteHandler(
        IClienteRepository repositorio,
        ILogger<AtualizarClienteHandler> log)
    {
        _repositorio = repositorio;
        _log = log;
    }

    public async Task<ClienteResponse> HandleAsync(AtualizarClienteCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // GAP-CW-CLI-PUT-CPF (Opção B em .NET 8): se o body trouxer cpf/cnpj/ativo,
        // não falhamos a requisição — apenas logamos warning. Esses campos não são
        // editáveis via PUT (cpf/cnpj: decisão de produto; ativo: mudança via
        // PATCH /clientes/{id}/status). Campos extras são descartados pelo binder.
        if (command.CamposExtras is { Count: > 0 })
        {
            var camposNaoEditaveis = command.CamposExtras.Keys
                .Where(k => string.Equals(k, "cpf", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(k, "cnpj", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(k, "ativo", StringComparison.OrdinalIgnoreCase));

            foreach (var campo in camposNaoEditaveis)
            {
                _log.LogWarning(
                    "PUT /clientes/{ClienteId} recebeu campo não editável '{Campo}' — ignorado. UsuarioId={UsuarioId}",
                    command.Id,
                    campo,
                    command.UsuarioId);
            }
        }

        // Defesa em profundidade: validator já exige NotNull em DataNascimento.
        // Bloqueio de fallback para nunca cair em InvalidOperationException no AtualizarDados.
        if (!command.DataNascimento.HasValue)
        {
            throw new ValidationException(
                "Dados do cliente inválidos. Verifique os campos e tente novamente.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["dataNascimento"] = ["Data de nascimento é obrigatória."],
                });
        }

        var cliente = await _repositorio.ObterPorIdAsync(command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Cliente não encontrado.");

        var nome = InputNormalizer.SanitizeTextOrNull(command.Nome)!;
        var telefoneDigits = InputNormalizer.OnlyDigitsOrNull(command.Telefone);
        var celularDigits = InputNormalizer.OnlyDigitsOrNull(command.Celular)!;
        var emailNormalizado = InputNormalizer.EmailOrNull(command.Email);
        var endereco = new Endereco(
            cep: InputNormalizer.OnlyDigitsOrNull(command.Endereco!.Cep) ?? string.Empty,
            logradouro: command.Endereco.Logradouro ?? string.Empty,
            numero: command.Endereco.Numero ?? string.Empty,
            complemento: command.Endereco.Complemento,
            bairro: command.Endereco.Bairro ?? string.Empty,
            cidade: command.Endereco.Cidade ?? string.Empty,
            uf: command.Endereco.Uf ?? string.Empty);

        // GAP-CW-CLI-PUT-EML: e-mail deve continuar único entre clientes,
        // ignorando o próprio cliente (permite manter o mesmo valor).
        if (emailNormalizado is not null
            && await _repositorio.ExisteEmailAsync(emailNormalizado, ignoreClienteId: command.Id, cancellationToken).ConfigureAwait(false))
        {
            throw new EmailClienteJaExisteException();
        }

        cliente.AtualizarDados(
            nome: nome,
            dataNascimento: command.DataNascimento.Value,
            celular: new Telefone(celularDigits),
            endereco: endereco,
            telefone: telefoneDigits is null ? null : new Telefone(telefoneDigits),
            email: emailNormalizado is null ? null : new Email(emailNormalizado));

        // GAP-CW-CLI-AUDIT: ator da última alteração.
        cliente.RegistrarAtualizadoPor(command.UsuarioId);

        await _repositorio.SalvarAsync(cancellationToken).ConfigureAwait(false);

        return ClienteResponse.FromEntity(cliente);
    }
}
