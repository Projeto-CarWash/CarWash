using CarWash.Application.Common;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.DTOs.Clientes;
using CarWash.Application.Interfaces;
using CarWash.Domain.Entities;
using CarWash.Domain.ValueObjects;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ValidationException = CarWash.Application.Common.Exceptions.ValidationException;

namespace CarWash.Application.Services.Clientes;

public class ClienteService : IClienteService
{
    private readonly IClienteRepository clienteRepository;
    private readonly IValidator<CreateClienteRequest> createValidator;
    private readonly IValidator<UpdateClienteRequest> updateValidator;
    private readonly ILogger<ClienteService> logger;

    public ClienteService(
        IClienteRepository clienteRepository,
        IValidator<CreateClienteRequest> createValidator,
        IValidator<UpdateClienteRequest> updateValidator,
        ILogger<ClienteService> logger)
    {
        this.clienteRepository = clienteRepository;
        this.createValidator = createValidator;
        this.updateValidator = updateValidator;
        this.logger = logger;
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

        // Defesa em profundidade: o validator já exige NotNull em DataNascimento,
        // mas se algum chamador interno bypassar a pipeline, falhamos com 400
        // estruturado em vez de 500 (NullReferenceException no Cliente.Criar).
        if (!request.DataNascimento.HasValue)
        {
            throw new ValidationException(
                "Dados do cliente inválidos. Verifique os campos e tente novamente.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["dataNascimento"] = ["Data de nascimento é obrigatória."],
                });
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

        // GAP-CW-CLI-EMAIL-1: e-mail deve ser único entre os clientes ativos
        // (índice parcial ux_clientes_email no banco como defesa final).
        if (emailNormalizado is not null
            && await clienteRepository.ExisteEmailAsync(emailNormalizado, ignoreClienteId: null, cancellationToken))
        {
            throw new ConflictException(
                "Já existe cliente cadastrado com este e-mail.",
                "cliente-email-duplicado");
        }

        var cliente = Cliente.Criar(
            id: Guid.NewGuid(),
            nome: nome,
            dataNascimento: request.DataNascimento.Value,
            celular: new Telefone(celularDigits),
            endereco: endereco,
            cpf: cpfDigits is null ? null : new Cpf(cpfDigits),
            cnpj: cnpjDigits is null ? null : new Cnpj(cnpjDigits),
            telefone: telefoneDigits is null ? null : new Telefone(telefoneDigits),
            email: emailNormalizado is null ? null : new Email(emailNormalizado));

        // GAP-CW-CLI-AUDIT-CREATE: registra o ator do cadastro.
        cliente.RegistrarCriadoPor(usuarioId);

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

        // GAP-CW-CLI-PUT-CPF (Opção B em .NET 8): se o body trouxer cpf/cnpj/ativo,
        // não falhamos a requisição — apenas logamos warning. Esses campos não são
        // editáveis via PUT (cpf/cnpj: decisão de produto; ativo: mudança via
        // PATCH /clientes/{id}/status). Campos extras são descartados pelo binder.
        if (request.CamposExtras is { Count: > 0 })
        {
            var camposNaoEditaveis = request.CamposExtras.Keys
                .Where(k => string.Equals(k, "cpf", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(k, "cnpj", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(k, "ativo", StringComparison.OrdinalIgnoreCase));

            foreach (var campo in camposNaoEditaveis)
            {
                logger.LogWarning(
                    "PUT /clientes/{ClienteId} recebeu campo não editável '{Campo}' — ignorado. UsuarioId={UsuarioId}",
                    id,
                    campo,
                    usuarioId);
            }
        }

        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(
                "Dados do cliente inválidos. Verifique os campos e tente novamente.",
                AgruparErros(validation.Errors));
        }

        // Defesa em profundidade: validator já exige NotNull em DataNascimento.
        // Bloqueio de fallback para nunca cair em InvalidOperationException no AtualizarDados.
        if (!request.DataNascimento.HasValue)
        {
            throw new ValidationException(
                "Dados do cliente inválidos. Verifique os campos e tente novamente.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["dataNascimento"] = ["Data de nascimento é obrigatória."],
                });
        }

        var cliente = await clienteRepository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Cliente não encontrado.");

        var nome = InputNormalizer.SanitizeTextOrNull(request.Nome)!;
        var telefoneDigits = InputNormalizer.OnlyDigitsOrNull(request.Telefone);
        var celularDigits = InputNormalizer.OnlyDigitsOrNull(request.Celular)!;
        var emailNormalizado = InputNormalizer.EmailOrNull(request.Email);
        var endereco = MontarEndereco(request.Endereco!);

        // GAP-CW-CLI-PUT-EML: e-mail deve continuar único entre clientes,
        // ignorando o próprio cliente (permite manter o mesmo valor).
        if (emailNormalizado is not null
            && await clienteRepository.ExisteEmailAsync(emailNormalizado, ignoreClienteId: id, cancellationToken))
        {
            throw new ConflictException(
                "Já existe cliente cadastrado com este e-mail.",
                "cliente-email-duplicado");
        }

        cliente.AtualizarDados(
            nome: nome,
            dataNascimento: request.DataNascimento.Value,
            celular: new Telefone(celularDigits),
            endereco: endereco,
            telefone: telefoneDigits is null ? null : new Telefone(telefoneDigits),
            email: emailNormalizado is null ? null : new Email(emailNormalizado));

        // GAP-CW-CLI-AUDIT: ator da última alteração.
        cliente.RegistrarAtualizadoPor(usuarioId);

        await clienteRepository.SalvarAsync(cancellationToken);

        return ToResponse(cliente);
    }

    public async Task<ClienteResponse> AlterarStatusAsync(
        Guid id,
        bool ativo,
        Guid? usuarioId,
        CancellationToken cancellationToken)
    {
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

        // GAP-CW-CLI-AUDIT: status também conta como alteração — registra o ator.
        cliente.RegistrarAtualizadoPor(usuarioId);

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

        // GAP-CLAMP: reflete o tamanho efetivo (clamp aplicado no repositório),
        // não o valor pedido pelo cliente. O controller já rejeita fora da faixa,
        // mas se algum chamador interno bypassar a validação, o JSON ainda é honesto.
        var paginaEfetiva = pagina < 1 ? 1 : pagina;
        int tamanhoEfetivo;
        if (tamanhoPagina < 1)
        {
            tamanhoEfetivo = 20;
        }
        else if (tamanhoPagina > 100)
        {
            tamanhoEfetivo = 100;
        }
        else
        {
            tamanhoEfetivo = tamanhoPagina;
        }

        return new ListaClientesResponse
        {
            Total = total,
            Pagina = paginaEfetiva,
            TamanhoPagina = tamanhoEfetivo,
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
