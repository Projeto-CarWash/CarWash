using CarWash.Application.Abstractions;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Common;
using CarWash.Application.Filiais.Common;
using CarWash.Application.Filiais.Persistence;
using CarWash.Domain.Entities;
using CarWash.Domain.ValueObjects;

namespace CarWash.Application.Filiais.Criar;

/// <summary>
/// Use case de cadastro de filial (RF017 + RF018). Defesa em camadas para
/// unicidade de <c>codigo</c>, <c>cnpj</c> e <c>nome</c>: pré-check + UK no
/// banco. O repositório intercepta <c>DbUpdateException</c> e traduz pela
/// <c>ConstraintName</c> para a exceção específica (ADR-0007 §5.2),
/// garantindo defesa contra race condition.
/// </summary>
public sealed class CriarFilialHandler : ICommandHandler<CriarFilialCommand, CriarFilialResponse>
{
    public const string EventoAuditoria = "FilialCriada";

    private readonly IFilialRepository _repositorio;
    private readonly ICurrentRequestContext _contexto;

    public CriarFilialHandler(IFilialRepository repositorio, ICurrentRequestContext contexto)
    {
        _repositorio = repositorio;
        _contexto = contexto;
    }

    public async Task<CriarFilialResponse> HandleAsync(CriarFilialCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        _contexto.DefinirEvento(EventoAuditoria);

        var nome = InputNormalizer.SanitizeTextOrNull(command.Nome)!;
        var codigo = CriarFilialCommandValidator.NormalizarCodigo(command.Codigo);
        var cnpjDigits = InputNormalizer.OnlyDigitsOrNull(command.Cnpj);
        var timezone = InputNormalizer.SanitizeTextOrNull(command.Timezone);
        var endereco = command.Endereco is null ? null : MontarEndereco(command.Endereco);

        // Camada 1 — pré-check para mensagens amigáveis.
        if (await _repositorio.ExisteCodigoAsync(codigo, cancellationToken).ConfigureAwait(false))
        {
            throw new FilialCodigoJaExisteException();
        }

        if (cnpjDigits is not null
            && await _repositorio.ExisteCnpjAsync(cnpjDigits, cancellationToken).ConfigureAwait(false))
        {
            throw new FilialCnpjJaExisteException();
        }

        if (await _repositorio.ExisteNomeAsync(nome, cancellationToken).ConfigureAwait(false))
        {
            throw new FilialNomeJaExisteException();
        }

        var filial = Filial.Criar(
            id: Guid.NewGuid(),
            nome: nome,
            codigo: codigo,
            celulasAtivas: command.CelulasAtivas!.Value,
            endereco: endereco,
            cnpj: cnpjDigits is null ? null : new Cnpj(cnpjDigits),
            timezone: timezone);

        filial.RegistrarCriadoPor(command.UsuarioId);

        // Camada 2 — UKs do banco + tradução em FilialXxxJaExisteException.
        await _repositorio.AdicionarAsync(filial, command.TraceId, command.UsuarioId, cancellationToken)
            .ConfigureAwait(false);

        return new CriarFilialResponse
        {
            Id = filial.Id,
            Mensagem = "Filial cadastrada com sucesso.",
            TraceId = command.TraceId,
        };
    }

    private static Endereco MontarEndereco(EnderecoFilialRequest request) => new(
        cep: InputNormalizer.OnlyDigitsOrNull(request.Cep) ?? string.Empty,
        logradouro: request.Logradouro ?? string.Empty,
        numero: request.Numero ?? string.Empty,
        complemento: request.Complemento,
        bairro: request.Bairro ?? string.Empty,
        cidade: request.Cidade ?? string.Empty,
        uf: request.Uf ?? string.Empty);
}
