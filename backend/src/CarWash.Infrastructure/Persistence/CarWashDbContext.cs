using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarWash.Infrastructure.Persistence;

/// <summary>
/// DbContext principal do CarWash. Schema <c>public</c> e configurations carregadas
/// por convenção (<see cref="ModelBuilder.ApplyConfigurationsFromAssembly"/>).
/// Naming convertido para <c>snake_case</c> via <c>EFCore.NamingConventions</c>.
/// </summary>
public class CarWashDbContext : DbContext
{
    public CarWashDbContext(DbContextOptions<CarWashDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<UsuarioSessao> UsuarioSessoes => Set<UsuarioSessao>();

    public DbSet<UsuarioPreferencia> UsuarioPreferencias => Set<UsuarioPreferencia>();

    public DbSet<Filial> Filiais => Set<Filial>();

    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<Filiado> Filiados => Set<Filiado>();

    public DbSet<Responsavel> Responsaveis => Set<Responsavel>();

    public DbSet<Veiculo> Veiculos => Set<Veiculo>();

    public DbSet<Servico> Servicos => Set<Servico>();

    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();

    public DbSet<AgendamentoItem> AgendamentoItens => Set<AgendamentoItem>();

    public DbSet<AgendamentoHistorico> AgendamentoHistoricos => Set<AgendamentoHistorico>();

    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<IdempotenciaRequisicao> IdempotenciaRequisicoes => Set<IdempotenciaRequisicao>();

    public DbSet<OutboxEvento> OutboxEventos => Set<OutboxEvento>();

    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();

    /// <summary>
    /// Stub C# mapeado para a função PostgreSQL <c>public.unaccent(text)</c>
    /// (extensão <c>unaccent</c>, instalada via migration <c>AdicionaAuditoriaUsuarioCliente</c>).
    /// Permite que LINQ traduza <c>CarWashDbContext.Unaccent(x.Nome)</c> diretamente
    /// para SQL — usado na busca de clientes (GAP-UNACCENT-ASSIM) para fechar a
    /// assimetria de busca por termos com/sem acento. Não deve ser chamado em C# puro.
    /// </summary>
    public static string Unaccent(string input)
        => throw new InvalidOperationException(
            "CarWashDbContext.Unaccent só pode ser usado dentro de expressões LINQ traduzidas para SQL.");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CarWashDbContext).Assembly);

        // Mapeia o stub Unaccent(string) para a função SQL public.unaccent(text).
        // A extensão é instalada pela migration AdicionaAuditoriaUsuarioCliente.
        modelBuilder
            .HasDbFunction(typeof(CarWashDbContext).GetMethod(nameof(Unaccent), new[] { typeof(string) })!)
            .HasName("unaccent")
            .HasSchema("public");

        base.OnModelCreating(modelBuilder);
    }
}
