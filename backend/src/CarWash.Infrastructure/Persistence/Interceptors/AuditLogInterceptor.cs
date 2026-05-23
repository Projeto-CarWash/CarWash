using System.Globalization;
using System.Text.Json;
using CarWash.Application.Abstractions;
using CarWash.Domain.Entities;
using CarWash.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CarWash.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Popula <c>audit_logs</c> a partir das entidades sendo persistidas no
/// <c>SaveChanges</c> quando há <see cref="ICurrentRequestContext.EventoAtual"/>
/// definido (DB001 §06.1). Aplica mascaramento via <see cref="AuditDataMasker"/>.
/// </summary>
public sealed class AuditLogInterceptor : SaveChangesInterceptor
{
    private static readonly Type[] EntidadesAuditaveis =
    [
        typeof(Usuario),
        typeof(Filial),
        typeof(Cliente),
        typeof(Filiado),
        typeof(Veiculo),
        typeof(Servico),
        typeof(Agendamento),
        typeof(AgendamentoItem),
        typeof(UsuarioPreferencia),
    ];

    private readonly ICurrentRequestContext _contexto;

    public AuditLogInterceptor(ICurrentRequestContext contexto)
    {
        _contexto = contexto;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        RegistrarLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        RegistrarLogs(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void RegistrarLogs(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var evento = _contexto.EventoAtual;
        if (string.IsNullOrWhiteSpace(evento))
        {
            return;
        }

        var entradas = context.ChangeTracker.Entries()
            .Where(IsEntradaAuditavel)
            .ToList();

        if (entradas.Count == 0)
        {
            return;
        }

        foreach (var entrada in entradas)
        {
            var nomeEntidade = entrada.Entity.GetType().Name;
            var entidadeId = ExtrairId(entrada);
            var dadosJson = SerializarDados(entrada);

            var log = AuditLog.Registrar(
                id: Guid.NewGuid(),
                evento: evento,
                entidade: nomeEntidade,
                correlationId: _contexto.CorrelationId,
                entidadeId: entidadeId,
                usuarioId: _contexto.UsuarioId,
                dados: dadosJson);

            context.Add(log);
        }
    }

    private static bool IsEntradaAuditavel(EntityEntry entry)
    {
        if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            return false;
        }

        return Array.Exists(EntidadesAuditaveis, t => t.IsInstanceOfType(entry.Entity));
    }

    private static Guid? ExtrairId(EntityEntry entry)
    {
        var idProperty = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey() && p.Metadata.Name == "Id");
        if (idProperty?.CurrentValue is Guid g)
        {
            return g;
        }

        return null;
    }

    private static string SerializarDados(EntityEntry entry)
    {
        var snapshot = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["state"] = entry.State.ToString(),
        };

        switch (entry.State)
        {
            case EntityState.Added:
                snapshot["snapshot"] = BuildPropertyDictionary(entry, useOriginal: false);
                break;
            case EntityState.Deleted:
                snapshot["snapshot"] = BuildPropertyDictionary(entry, useOriginal: true);
                break;
            case EntityState.Modified:
                var (before, after, changed) = BuildDiff(entry);
                snapshot["before"] = before;
                snapshot["after"] = after;
                snapshot["fields_changed"] = changed;
                break;
        }

        var raw = JsonSerializer.Serialize(snapshot);
        return AuditDataMasker.MaskJson(raw);
    }

    private static Dictionary<string, object?> BuildPropertyDictionary(EntityEntry entry, bool useOriginal)
    {
        var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var prop in entry.Properties)
        {
            var nome = prop.Metadata.Name;
            var valor = useOriginal ? prop.OriginalValue : prop.CurrentValue;
            dict[nome] = NormalizeValue(valor);
        }

        return dict;
    }

    private static (Dictionary<string, object?> Before, Dictionary<string, object?> After, List<string> Changed)
        BuildDiff(EntityEntry entry)
    {
        var before = new Dictionary<string, object?>(StringComparer.Ordinal);
        var after = new Dictionary<string, object?>(StringComparer.Ordinal);
        var changed = new List<string>();

        foreach (var prop in entry.Properties)
        {
            if (!prop.IsModified)
            {
                continue;
            }

            var nome = prop.Metadata.Name;
            before[nome] = NormalizeValue(prop.OriginalValue);
            after[nome] = NormalizeValue(prop.CurrentValue);
            changed.Add(nome);
        }

        return (before, after, changed);
    }

    private static object? NormalizeValue(object? valor) => valor switch
    {
        null => null,
        DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
        Guid g => g.ToString(),
        _ => valor,
    };
}
