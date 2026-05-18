using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Clientes.Common;
using CarWash.Application.Clientes.Persistence;
using CarWash.Application.Common;
using CarWash.Application.Common.Exceptions;
using CarWash.Domain.Entities;
using CarWash.Domain.ValueObjects;

namespace CarWash.Application.Clientes.Criar;

/// <summary>
/// Use case de cadastro de cliente (RF002 + RF003). Defesa em duas camadas para
/// unicidade de e-mail/CPF/CNPJ: pré-check + constraints UK no banco
/// (ConflictException emitida pelo repositório em violação concorrente).
/// </summary>
public sealed class CriarClienteHandler : ICommandHandler<CriarClienteCommand, CriarClienteResponse>
{
    private readonly IClienteRepository _repositorio;

    public CriarClienteHandler(IClienteRepository repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<CriarClienteResponse> HandleAsync(CriarClienteCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Defesa em profundidade: o validator já exige NotNull em DataNascimento,
        // mas se algum chamador interno bypassar a pipeline, falhamos com 400
        // estruturado em vez de 500 (NullReferenceException no Cliente.Criar).
        if (!command.DataNascimento.HasValue)
        {
            throw new ValidationException(
                "Dados do cliente inválidos. Verifique os campos e tente novamente.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["dataNascimento"] = ["Data de nascimento é obrigatória."],
                });
        }

        var nome = InputNormalizer.SanitizeTextOrNull(command.Nome)!;
        var cpfDigits = InputNormalizer.OnlyDigitsOrNull(command.Cpf);
        var cnpjDigits = InputNormalizer.OnlyDigitsOrNull(command.Cnpj);
        var telefoneDigits = InputNormalizer.OnlyDigitsOrNull(command.Telefone);
        var celularDigits = InputNormalizer.OnlyDigitsOrNull(command.Celular)!;
        var emailNormalizado = InputNormalizer.EmailOrNull(command.Email);
        var endereco = MontarEndereco(command.Endereco!);

        if (cpfDigits is not null && await _repositorio.ExisteCpfAsync(cpfDigits, cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException(
                "Já existe cliente cadastrado com este documento.",
                "cliente-documento-duplicado");
        }

        if (cnpjDigits is not null && await _repositorio.ExisteCnpjAsync(cnpjDigits, cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException(
                "Já existe cliente cadastrado com este documento.",
                "cliente-documento-duplicado");
        }

        // GAP-CW-CLI-EMAIL-1: e-mail deve ser único entre os clientes ativos
        // (índice parcial ux_clientes_email no banco como defesa final).
        if (emailNormalizado is not null
            && await _repositorio.ExisteEmailAsync(emailNormalizado, ignoreClienteId: null, cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException(
                "Já existe cliente cadastrado com este e-mail.",
                "cliente-email-duplicado");
        }

        var cliente = Cliente.Criar(
            id: Guid.NewGuid(),
            nome: nome,
            dataNascimento: command.DataNascimento.Value,
            celular: new Telefone(celularDigits),
            endereco: endereco,
            cpf: cpfDigits is null ? null : new Cpf(cpfDigits),
            cnpj: cnpjDigits is null ? null : new Cnpj(cnpjDigits),
            telefone: telefoneDigits is null ? null : new Telefone(telefoneDigits),
            email: emailNormalizado is null ? null : new Email(emailNormalizado));

        // GAP-CW-CLI-AUDIT-CREATE: registra o ator do cadastro.
        cliente.RegistrarCriadoPor(command.UsuarioId);

        await _repositorio.AdicionarAsync(cliente, command.TraceId, command.UsuarioId, cancellationToken).ConfigureAwait(false);

        return new CriarClienteResponse
        {
            Id = cliente.Id,
            Mensagem = "Cliente cadastrado com sucesso.",
            TraceId = command.TraceId,
        };
    }

    private static Endereco MontarEndereco(EnderecoRequest request) => new(
        cep: InputNormalizer.OnlyDigitsOrNull(request.Cep) ?? string.Empty,
        logradouro: request.Logradouro ?? string.Empty,
        numero: request.Numero ?? string.Empty,
        complemento: request.Complemento,
        bairro: request.Bairro ?? string.Empty,
        cidade: request.Cidade ?? string.Empty,
        uf: request.Uf ?? string.Empty);
}
