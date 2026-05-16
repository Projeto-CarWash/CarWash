using CarWash.Application.Common;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.DTOs.Clientes;
using CarWash.Application.Interfaces;
using CarWash.Domain.Entities;
using CarWash.Domain.ValueObjects;
using FluentValidation;
using ValidationException = CarWash.Application.Common.Exceptions.ValidationException;

namespace CarWash.Application.Services.Clientes;

public class ClienteService : IClienteService
{
    private readonly IClienteRepository clienteRepository;
    private readonly IValidator<CreateClienteRequest> validator;

    public ClienteService(
        IClienteRepository clienteRepository,
        IValidator<CreateClienteRequest> validator)
    {
        this.clienteRepository = clienteRepository;
        this.validator = validator;
    }

    public async Task<CreateClienteResponse> CriarAsync(
        CreateClienteRequest request,
        string traceId,
        Guid? usuarioId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);

        if (!validation.IsValid)
        {
            var erros = validation.Errors
                .GroupBy(e => string.IsNullOrWhiteSpace(e.PropertyName) ? "body" : e.PropertyName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray(),
                    StringComparer.OrdinalIgnoreCase);

            throw new ValidationException(
                "Dados do cliente inválidos. Verifique os campos e tente novamente.",
                erros);
        }

        var nome = InputNormalizer.TrimOrNull(request.Nome)!;
        var cpfDigits = InputNormalizer.OnlyDigitsOrNull(request.Cpf);
        var cnpjDigits = InputNormalizer.OnlyDigitsOrNull(request.Cnpj);
        var telefoneDigits = InputNormalizer.OnlyDigitsOrNull(request.Telefone);
        var celularDigits = InputNormalizer.OnlyDigitsOrNull(request.Celular);
        var emailNormalizado = InputNormalizer.EmailOrNull(request.Email);
        var endereco = InputNormalizer.TrimOrNull(request.Endereco);
        var observacoes = InputNormalizer.TrimOrNull(request.Observacoes);

        if (cpfDigits is not null && await clienteRepository.ExisteCpfAsync(cpfDigits, cancellationToken))
        {
            throw new ConflictException(
                "Já existe cliente cadastrado com este documento.",
                "cliente-documento-duplicado");
        }

        if (cnpjDigits is not null && await clienteRepository.ExisteCnpjAsync(cnpjDigits, cancellationToken))
        {
            throw new ConflictException(
                "Já existe cliente cadastrado com este documento.",
                "cliente-documento-duplicado");
        }

        var cliente = Cliente.Criar(
            id: Guid.NewGuid(),
            nome: nome,
            cpf: cpfDigits is null ? null : new Cpf(cpfDigits),
            cnpj: cnpjDigits is null ? null : new Cnpj(cnpjDigits),
            telefone: telefoneDigits is null ? null : new Telefone(telefoneDigits),
            celular: celularDigits is null ? null : new Telefone(celularDigits),
            email: emailNormalizado is null ? null : new Email(emailNormalizado),
            endereco: endereco,
            observacoes: observacoes);

        await clienteRepository.AdicionarAsync(cliente, traceId, usuarioId, cancellationToken);

        return new CreateClienteResponse
        {
            Id = cliente.Id,
            Mensagem = "Cliente cadastrado com sucesso.",
            TraceId = traceId,
        };
    }

    public async Task<ClienteResponse?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var cliente = await clienteRepository.ObterPorIdAsync(id, cancellationToken);

        if (cliente is null)
        {
            return null;
        }

        return new ClienteResponse
        {
            Id = cliente.Id,
            Nome = cliente.Nome,
            Cpf = cliente.Cpf,
            Cnpj = cliente.Cnpj,
            Telefone = cliente.Telefone,
            Celular = cliente.Celular,
            Email = cliente.Email,
            Endereco = cliente.Endereco,
            Observacoes = cliente.Observacoes,
            Ativo = cliente.Ativo,
            CriadoEm = cliente.CriadoEm,
            AtualizadoEm = cliente.AtualizadoEm,
        };
    }
}
