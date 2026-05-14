using System.Text.Json;
using CarWash.Application.Common;
using CarWash.Application.DTOs.Clientes;
using CarWash.Application.Exceptions;
using CarWash.Application.Interfaces;
using CarWash.Domain.Entities;
using FluentValidation;

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
            throw new ValidationException(validation.Errors);
        }

        var nome = InputNormalizer.TrimOrNull(request.Nome)!;
        var cpf = InputNormalizer.OnlyDigitsOrNull(request.Cpf);
        var cnpj = InputNormalizer.OnlyDigitsOrNull(request.Cnpj);
        var telefone = InputNormalizer.OnlyDigitsOrNull(request.Telefone);
        var celular = InputNormalizer.OnlyDigitsOrNull(request.Celular);
        var email = InputNormalizer.EmailOrNull(request.Email);
        var endereco = InputNormalizer.TrimOrNull(request.Endereco);
        var observacoes = InputNormalizer.TrimOrNull(request.Observacoes);

        if (cpf is not null && await clienteRepository.ExisteCpfAsync(cpf, cancellationToken))
        {
            throw new ClienteDocumentoDuplicadoException();
        }

        if (cnpj is not null && await clienteRepository.ExisteCnpjAsync(cnpj, cancellationToken))
        {
            throw new ClienteDocumentoDuplicadoException();
        }

        var cliente = new Cliente(
            nome,
            cpf,
            cnpj,
            telefone,
            celular,
            email,
            endereco,
            observacoes);

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
