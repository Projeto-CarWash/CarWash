using CarWash.Application.Agenda.Common;
using CarWash.Application.Agenda.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CarWash.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação de leitura de <see cref="IAgendaRepository"/> sobre EF Core
/// (RF009). Projeta uma única query sem materializar entidades inteiras
/// (<see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}"/> +
/// <c>.Select(...)</c>), com joins explícitos por LINQ — o agregado
/// <c>Agendamento</c> não expõe propriedades de navegação. Reusa o índice
/// <c>idx_ag_filial_inicio</c> (FilialId, Inicio) para filtro + ordenação.
/// </summary>
public sealed class AgendaRepository : IAgendaRepository
{
    private readonly CarWashDbContext _db;

    public AgendaRepository(CarWashDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AgendaProjecao>> ConsultarAsync(
        Guid filialId,
        DateTime inicioUtc,
        DateTime fimUtc,
        Guid? clienteId,
        Guid? responsavelId,
        string? statusDb,
        CancellationToken cancellationToken)
    {
        // Janela meio-aberta [inicioUtc, fimUtc) + filial — coberta pelo índice
        // composto idx_ag_filial_inicio.
        var query = _db.Agendamentos
            .AsNoTracking()
            .Where(ag => ag.FilialId == filialId
                && ag.Inicio >= inicioUtc
                && ag.Inicio < fimUtc);

        // Filtros opcionais aplicados condicionalmente (compõem o WHERE).
        if (clienteId.HasValue)
        {
            query = query.Where(ag => ag.ClienteId == clienteId.Value);
        }

        if (responsavelId.HasValue)
        {
            // L5: usuarioId filtra pelo responsável de execução, não pelo criador.
            query = query.Where(ag => ag.ResponsavelId == responsavelId.Value);
        }

        if (!string.IsNullOrWhiteSpace(statusDb))
        {
            query = query.Where(ag => ag.StatusRaw == statusDb);
        }

        // Projeção via .Select: join explícito com Clientes/Veiculos e subquery
        // correlacionada para os serviços. Tudo numa única query SQL (sem N+1).
        var resultado = await query
            .OrderBy(ag => ag.Inicio)
            .ThenBy(ag => ag.CriadoEm)
            .Select(ag => new AgendaProjecao
            {
                AgendamentoId = ag.Id,
                Status = ag.StatusRaw,
                FilialId = ag.FilialId,
                Inicio = ag.Inicio,
                Fim = ag.Fim,
                DuracaoTotalMin = ag.DuracaoTotalMin,
                ValorTotal = ag.ValorTotal,
                Observacoes = ag.Observacoes,
                CriadoEm = ag.CriadoEm,
                AtualizadoEm = ag.AtualizadoEm,
                ClienteId = ag.ClienteId,
                ClienteNome = _db.Clientes
                    .Where(c => c.Id == ag.ClienteId)
                    .Select(c => c.Nome)
                    .FirstOrDefault()!,
                ClienteCpf = _db.Clientes
                    .Where(c => c.Id == ag.ClienteId)
                    .Select(c => c.Cpf)
                    .FirstOrDefault(),
                ClienteCnpj = _db.Clientes
                    .Where(c => c.Id == ag.ClienteId)
                    .Select(c => c.Cnpj)
                    .FirstOrDefault(),
                ClienteTelefone = _db.Clientes
                    .Where(c => c.Id == ag.ClienteId)
                    .Select(c => c.Telefone)
                    .FirstOrDefault(),
                ClienteCelular = _db.Clientes
                    .Where(c => c.Id == ag.ClienteId)
                    .Select(c => c.Celular)
                    .FirstOrDefault()!,
                VeiculoId = ag.VeiculoId,
                VeiculoPlaca = _db.Veiculos
                    .Where(v => v.Id == ag.VeiculoId)
                    .Select(v => v.Placa)
                    .FirstOrDefault()!,
                VeiculoModelo = _db.Veiculos
                    .Where(v => v.Id == ag.VeiculoId)
                    .Select(v => v.Modelo)
                    .FirstOrDefault()!,
                VeiculoFabricante = _db.Veiculos
                    .Where(v => v.Id == ag.VeiculoId)
                    .Select(v => v.Fabricante)
                    .FirstOrDefault()!,
                VeiculoCor = _db.Veiculos
                    .Where(v => v.Id == ag.VeiculoId)
                    .Select(v => v.Cor)
                    .FirstOrDefault()!,
                Servicos = (from item in _db.AgendamentoItens
                            join servico in _db.Servicos on item.ServicoId equals servico.Id
                            where item.AgendamentoId == ag.Id
                            orderby item.CriadoEm, item.Id
                            select new AgendaServicoProjecao
                            {
                                ItemId = item.Id,
                                Id = servico.Id,
                                Nome = servico.Nome,

                                // Snapshot RN006: duração/preço aplicados no item,
                                // não os valores correntes do catálogo de Servico.
                                DuracaoMin = item.DuracaoAplicada,
                                Preco = item.PrecoAplicado,
                            }).ToList(),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return resultado;
    }
}
