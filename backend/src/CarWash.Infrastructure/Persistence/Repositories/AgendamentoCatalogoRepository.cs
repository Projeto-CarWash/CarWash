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
        _db.Responsaveis
            .AsNoTracking()
            .Where(r => r.Id == responsavelId)
            .Select(r => new ResponsavelSnapshot(r.Id, r.ClienteTitularId, r.Ativo))
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

        // Lista (não array) evita a sobrecarga MemoryExtensions.Contains(ReadOnlySpan<T>)
        // que quebra a tradução do EF Core 8 no runtime .NET 9/10 (ref struct no
        // interpretador de expressões). List<T>.Contains é método de instância e
        // traduz para IN sem ambiguidade.
        var ids = servicoIds.Distinct().ToList();
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

    /// <inheritdoc/>
    public Task<ResponsavelResumoSnapshot?> ObterResponsavelResumoAsync(Guid responsavelId, CancellationToken cancellationToken) =>
        _db.Responsaveis
            .AsNoTracking()
            .Where(r => r.Id == responsavelId)
            .Select(r => new ResponsavelResumoSnapshot(
                r.Id,
                r.ClienteTitularId,
                r.Nome,
                r.Documento,
                r.GrauVinculo,
                r.Ativo))
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc/>
    public Task<int?> ObterCelulasAtivasFilialAsync(Guid filialId, CancellationToken cancellationToken) =>
        _db.Filiais
            .AsNoTracking()
            .Where(f => f.Id == filialId)
            .Select(f => (int?)f.CelulasAtivas)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<int> ContarSobreposicoesNaFilialAsync(
        Guid filialId,
        DateTime inicio,
        DateTime fim,
        CancellationToken cancellationToken) =>
        _db.Agendamentos
            .AsNoTracking()
            .Where(a => a.FilialId == filialId
                     && a.StatusRaw == "agendado"
                     && a.Inicio < fim
                     && a.Fim > inicio)
            .CountAsync(cancellationToken);
}
