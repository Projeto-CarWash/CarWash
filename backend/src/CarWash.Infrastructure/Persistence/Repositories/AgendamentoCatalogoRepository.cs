using CarWash.Application.Agendamentos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CarWash.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação concreta de <see cref="IAgendamentoCatalogoRepository"/> — leituras
/// de validação das dependências de um agendamento (filial, veículo, cliente,
/// responsável e serviços). Todas as consultas são <c>AsNoTracking</c>.
/// </summary>
public sealed class AgendamentoCatalogoRepository : IAgendamentoCatalogoRepository
{
    private readonly CarWashDbContext _db;

    public AgendamentoCatalogoRepository(CarWashDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc/>
    public Task<bool> FilialAtivaAsync(Guid filialId, CancellationToken cancellationToken) =>
        _db.Filiais.AsNoTracking().AnyAsync(f => f.Id == filialId && f.Ativa, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> FilialExisteAsync(Guid filialId, CancellationToken cancellationToken) =>
        _db.Filiais.AsNoTracking().AnyAsync(f => f.Id == filialId, cancellationToken);

    /// <inheritdoc/>
    public Task<VeiculoSnapshot?> ObterVeiculoAsync(Guid veiculoId, CancellationToken cancellationToken) =>
        _db.Veiculos
            .AsNoTracking()
            .Where(v => v.Id == veiculoId)
            .Select(v => new VeiculoSnapshot(v.Id, v.ClienteId, v.Ativo))
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc/>
    public Task<bool> ClienteAtivoAsync(Guid clienteId, CancellationToken cancellationToken) =>
        _db.Clientes.AsNoTracking().AnyAsync(c => c.Id == clienteId && c.Ativo, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> ClienteExisteAsync(Guid clienteId, CancellationToken cancellationToken) =>
        _db.Clientes.AsNoTracking().AnyAsync(c => c.Id == clienteId, cancellationToken);

    /// <inheritdoc/>
    public Task<ResponsavelSnapshot?> ObterResponsavelAsync(Guid responsavelId, CancellationToken cancellationToken) =>
        _db.Filiados
            .AsNoTracking()
            .Where(f => f.Id == responsavelId)
            .Select(f => new ResponsavelSnapshot(f.Id, f.ClienteId, f.Ativo))
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ServicoSnapshot>> ObterServicosAsync(
        IReadOnlyCollection<Guid> servicoIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(servicoIds);

        if (servicoIds.Count == 0)
        {
            return Array.Empty<ServicoSnapshot>();
        }

        var ids = servicoIds.Distinct().ToArray();
        return await _db.Servicos
            .AsNoTracking()
            .Where(s => ids.Contains(s.Id))
            .Select(s => new ServicoSnapshot(s.Id, s.Nome, s.Preco, s.DuracaoMin, s.Ativo))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<FilialResumoSnapshot?> ObterFilialResumoAsync(Guid filialId, CancellationToken cancellationToken) =>
        _db.Filiais
            .AsNoTracking()
            .Where(f => f.Id == filialId)
            .Select(f => new FilialResumoSnapshot(f.Id, f.Nome, f.Ativa))
            .FirstOrDefaultAsync(cancellationToken);

    // O cliente é PF (CPF) ou PJ (CNPJ) — CHECK ck_clientes_cpf_ou_cnpj garante
    // exatamente um preenchido. O coalesce expõe o documento de negócio.
    /// <inheritdoc/>
    public Task<ClienteResumoSnapshot?> ObterClienteResumoAsync(Guid clienteId, CancellationToken cancellationToken) =>
        _db.Clientes
            .AsNoTracking()
            .Where(c => c.Id == clienteId)
            .Select(c => new ClienteResumoSnapshot(
                c.Id,
                c.Nome,
                c.Cpf ?? c.Cnpj ?? string.Empty,
                c.Ativo))
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc/>
    public Task<VeiculoResumoSnapshot?> ObterVeiculoResumoAsync(Guid veiculoId, CancellationToken cancellationToken) =>
        _db.Veiculos
            .AsNoTracking()
            .Where(v => v.Id == veiculoId)
            .Select(v => new VeiculoResumoSnapshot(
                v.Id,
                v.ClienteId,
                v.Placa,
                v.Modelo,
                v.Cor,
                v.Ativo))
            .FirstOrDefaultAsync(cancellationToken);
}
