using System.Linq;
using System.Reflection;
using CarWash.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace CarWash.UnitTests.Architecture;

/// <summary>
/// Convenção: toda migration EF Core do projeto deve viver no namespace canônico
/// <c>CarWash.Infrastructure.Persistence.Migrations</c>. Esta regra evita o cenário
/// que motivou a consolidação do ADR 0006 — duas árvores paralelas de migrations
/// no mesmo assembly com <c>InitialSchema</c> duplicado, levando o EF Core a tentar
/// aplicar ambas e quebrar todos os testes de integração com
/// "relation already exists". Documentado em
/// <c>docs/arquitetura-backend.md → Padrão de migrations EF Core</c>.
/// </summary>
public sealed class MigrationConventionTests
{
    private const string NamespaceCanonico = "CarWash.Infrastructure.Persistence.Migrations";

    [Fact]
    public void Todas_as_migrations_devem_estar_no_namespace_canonico()
    {
        var assembly = typeof(CarWashDbContext).Assembly;

        var migrationsForaDoNamespace = assembly
            .GetTypes()
            .Where(t => t.GetCustomAttribute<MigrationAttribute>() is not null)
            .Where(t => t.Namespace != NamespaceCanonico)
            .Select(t => t.FullName)
            .ToList();

        migrationsForaDoNamespace.Should().BeEmpty(
            because: "todas as migrations devem estar em " + NamespaceCanonico +
                " (ver docs/arquitetura-backend.md → Padrão de migrations EF Core)");
    }
}
