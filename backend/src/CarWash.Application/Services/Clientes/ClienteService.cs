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
    private readonly IValidator<CreateClienteRequest> createValidator;
    private readonly IValidator<UpdateClienteRequest> updateValidator;

    public ClienteService(
        IClienteRepository clienteRepository,
        IValidator<CreateClienteRequest> createValidator,
        IValidator<UpdateClienteRequest> updateValidator)
    {
        this.clienteRepository = clienteRepository;
        this.createValidator = createValidator;
        this.updateValidator = updateValidator;
    }

    public async Task<CreateClienteResponse> CriarAsync(
        CreateClienteRequest request,
        string traceId,
        Guid? usuarioId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = await createValidator.ValidateAsync(request, cancellationToken);

        if (!validation.IsValid)
        {
            throw new ValidationException(
                "Dados do cliente inválidos. Verifique os campos e tente novamente.",
                AgruparErros(validation.Errors));
        }

        var nome = InputNormalizer.SanitizeTextOrNull(request.Nome)!;
        var cpfDigits = InputNormalizer.OnlyDigitsOrNull(request.Cpf);
        var cnpjDigits = InputNormalizer.OnlyDigitsOrNull(request.Cnpj);
        var telefoneDigits = InputNormalizer.OnlyDigitsOrNull(request.Telefone);
        var celularDigits = InputNormalizer.OnlyDigitsOrNull(request.Celular)!;
        var emailNormalizado = InputNormalizer.EmailOrNull(request.Email);
        var endereco = MontarEndereco(request.Endereco!);

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
            dataNascimento: request.DataNascimento!.Value,
            celular: new Telefone(celularDigits),
            endereco: endereco,
            cpf: cpfDigits is null ? null : new Cpf(cpfDigits),
            cnpj: cnpjDigits is null ? null : new Cnpj(cnpjDigits),
            telefone: telefoneDigits is null ? null : new Telefone(telefoneDigits),
            email: emailNormalizado is null ? null : new Email(emailNormalizado));

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
        return cliente is null ? null : ToResponse(cliente);
    }

    public async Task<ClienteResponse> AtualizarAsync(
        Guid id,
        UpdateClienteRequest request,
        Guid? usuarioId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        _ = usuarioId;

        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(
                "Dados do cliente inválidos. Verifique os campos e tente novamente.",
                AgruparErros(validation.Errors));
        }

        var cliente = await clienteRepository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Cliente não encontrado.");

        var nome = InputNormalizer.SanitizeTextOrNull(request.Nome)!;
        var telefoneDigits = InputNormalizer.OnlyDigitsOrNull(request.Telefone);
        var celularDigits = InputNormalizer.OnlyDigitsOrNull(request.Celular)!;
        var emailNormalizado = InputNormalizer.EmailOrNull(request.Email);
        var endereco = MontarEndereco(request.Endereco!);

        cliente.AtualizarDados(
            nome: nome,
            dataNascimento: request.DataNascimento!.Value,
            celular: new Telefone(celularDigits),
            endereco: endereco,
            telefone: telefoneDigits is null ? null : new Telefone(telefoneDigits),
            email: emailNormalizado is null ? null : new Email(emailNormalizado));

        await clienteRepository.SalvarAsync(cancellationToken);

        return ToResponse(cliente);
    }

    public async Task<ClienteResponse> AlterarStatusAsync(
        Guid id,
        bool ativo,
        Guid? usuarioId,
        CancellationToken cancellationToken)
    {
        _ = usuarioId;

        var cliente = await clienteRepository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Cliente não encontrado.");

        if (ativo)
        {
            cliente.Ativar();
        }
        else
        {
            cliente.Inativar();
        }

        await clienteRepository.SalvarAsync(cancellationToken);
        return ToResponse(cliente);
    }

    public async Task<ListaClientesResponse> ListarAsync(
        string? busca,
        bool? ativo,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken)
    {
        var (itens, total) = await clienteRepository.ListarAsync(
            busca,
            ativo,
            pagina,
            tamanhoPagina,
            cancellationToken);

        return new ListaClientesResponse
        {
            Total = total,
            Pagina = pagina < 1 ? 1 : pagina,
            TamanhoPagina = tamanhoPagina < 1 ? 20 : tamanhoPagina,
            Itens = itens.Select(c => new ClienteResumoResponse
            {
                Id = c.Id,
                Nome = c.Nome,
                Cpf = c.Cpf,
                Cnpj = c.Cnpj,
                Celular = c.Celular,
                Email = c.Email,
                Cidade = c.EnderecoCidade,
                Uf = c.EnderecoUf,
                Ativo = c.Ativo,
                CriadoEm = c.CriadoEm,
            }).ToList(),
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

    private static Dictionary<string, string[]> AgruparErros(IEnumerable<FluentValidation.Results.ValidationFailure> erros)
        => erros
            .GroupBy(e => string.IsNullOrWhiteSpace(e.PropertyName) ? "body" : e.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray(),
                StringComparer.OrdinalIgnoreCase);

    private static ClienteResponse ToResponse(Cliente cliente) => new()
    {
        Id = cliente.Id,
        Nome = cliente.Nome,
        DataNascimento = cliente.DataNascimento,
        Cpf = cliente.Cpf,
        Cnpj = cliente.Cnpj,
        Telefone = cliente.Telefone,
        Celular = cliente.Celular,
        Email = cliente.Email,
        Endereco = new EnderecoResponse
        {
            Cep = cliente.EnderecoCep,
            Logradouro = cliente.EnderecoLogradouro,
            Numero = cliente.EnderecoNumero,
            Complemento = cliente.EnderecoComplemento,
            Bairro = cliente.EnderecoBairro,
            Cidade = cliente.EnderecoCidade,
            Uf = cliente.EnderecoUf,
        },
        Ativo = cliente.Ativo,
        CriadoEm = cliente.CriadoEm,
        AtualizadoEm = cliente.AtualizadoEm,
    };
}
