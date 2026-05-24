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

        string nome = InputNormalizer.TrimOrNull(request.Nome)!;
        string? cpf = InputNormalizer.OnlyDigitsOrNull(request.Cpf);
        string? cnpj = InputNormalizer.OnlyDigitsOrNull(request.Cnpj);
        string? telefone = InputNormalizer.OnlyDigitsOrNull(request.Telefone);
        string? celular = InputNormalizer.OnlyDigitsOrNull(request.Celular);
        string? email = InputNormalizer.EmailOrNull(request.Email);
        string? endereco = InputNormalizer.SanitizeTextOrNull(request.Endereco);
        string? observacoes = InputNormalizer.SanitizeTextOrNull(request.Observacoes);

        if (cpf is not null && await clienteRepository.ExisteCpfAsync(cpf, cancellationToken))
        {
            throw new ClienteDocumentoDuplicadoException();
        }

        if (cnpj is not null && await clienteRepository.ExisteCnpjAsync(cnpj, cancellationToken))
        {
            throw new ClienteDocumentoDuplicadoException();
        }

        List<string> placasNormalizadas = request.Veiculos!
            .Select(veiculo => InputNormalizer.PlacaOrNull(veiculo.Placa)!)
            .ToList();

        if (await clienteRepository.ExisteAlgumaPlacaAsync(placasNormalizadas, cancellationToken))
        {
            throw new VeiculoPlacaDuplicadaException();
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

        List<Veiculo> veiculos = request.Veiculos!
            .Select(veiculo => new Veiculo(
                cliente.Id,
                InputNormalizer.PlacaOrNull(veiculo.Placa)!,
                InputNormalizer.TrimOrNull(veiculo.Modelo)!,
                InputNormalizer.TrimOrNull(veiculo.Fabricante)!,
                InputNormalizer.TrimOrNull(veiculo.Cor)!,
                veiculo.Ano))
            .ToList();

        await clienteRepository.AdicionarAsync(
            cliente,
            veiculos,
            traceId,
            usuarioId,
            cancellationToken);

        return new CreateClienteResponse
        {
            Id = cliente.Id,
            Mensagem = "Cliente e veículos cadastrados com sucesso.",
            TraceId = traceId,
            Veiculos = veiculos
                .Select(veiculo => new VeiculoCriadoResponse
                {
                    Id = veiculo.Id,
                    Placa = veiculo.Placa,
                    Modelo = veiculo.Modelo,
                    Fabricante = veiculo.Fabricante,
                    Cor = veiculo.Cor,
                })
                .ToList(),
        };
    }

    public async Task<ClienteResponse?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        Cliente? cliente = await clienteRepository.ObterPorIdAsync(id, cancellationToken);

        if (cliente is null)
        {
            return null;
        }

        IReadOnlyCollection<Veiculo> veiculos =
            await clienteRepository.ObterVeiculosPorClienteIdAsync(id, cancellationToken);

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
            Veiculos = veiculos
                .Select(veiculo => new VeiculoResponse
                {
                    Id = veiculo.Id,
                    Placa = veiculo.Placa,
                    Modelo = veiculo.Modelo,
                    Fabricante = veiculo.Fabricante,
                    Cor = veiculo.Cor,
                })
                .ToList(),
        };
    }
}
