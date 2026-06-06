using System.Data.Common;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using CarWash.Domain.ValueObjects;
using CarWash.Infrastructure.Persistence;
using CarWash.Infrastructure.Persistence.Repositories;
using CarWash.IntegrationTests.Collections;
using CarWash.IntegrationTests.Fixtures;
using CarWash.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace CarWash.IntegrationTests.Persistence;

/// <summary>
/// Valida a consulta de leitura do <see cref="AgendaRepository"/> (RF009 — card 132)
/// contra PostgreSQL real: ordenação, filtros e — principalmente — a ausência de
/// N+1 (a projeção com serviços aninhados deve resultar numa única query SQL).
/// </summary>
[Collection(nameof(PostgresCollection))]
public class AgendaRepositoryTests : IAsyncDisposable
{
    private static readonly Guid AdminId = new("00000000-0000-0000-0000-000000000001");

    private readonly PostgresFixture _fixture;

    public AgendaRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConsultarAsync_com_varios_agendamentos_e_servicos_executa_uma_unica_query()
    {
        var filialId = await SemearFilialAsync();

        // 3 agendamentos, cada um com 2 serviços — cenário em que um N+1 explodiria
        // em SELECTs adicionais por agendamento e por serviço.
        var baseInicio = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
        for (int i = 0; i < 3; i++)
        {
            await SemearAgendamentoComServicosAsync(filialId, baseInicio.AddHours(i), servicos: 2);
        }

        var contador = new ContadorDeComandos();
        await using var db = CarWashDbContextFactoryForTests.Create(_fixture, contador);
        var repo = new AgendaRepository(db);

        var projecoes = await repo.ConsultarAsync(
            filialId,
            baseInicio.AddHours(-1),
            baseInicio.AddHours(6),
            clienteId: null,
            responsavelId: null,
            statusDb: null,
            CancellationToken.None);

        projecoes.Should().HaveCount(3);
        projecoes.Should().OnlyContain(p => p.Servicos.Count == 2);

        // Núcleo do anti-N+1: independentemente da quantidade de agendamentos e
        // serviços, a consulta deve disparar exatamente 1 SELECT.
        contador.TotalSelects.Should().Be(
            1,
            "a projeção de agenda deve traduzir para uma única query SQL (sem N+1)");
    }

    [Fact]
    public async Task ConsultarAsync_ordena_por_inicio_asc()
    {
        var filialId = await SemearFilialAsync();
        var baseInicio = new DateTime(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc);

        // Semeia fora de ordem.
        await SemearAgendamentoComServicosAsync(filialId, baseInicio.AddHours(4), servicos: 1);
        await SemearAgendamentoComServicosAsync(filialId, baseInicio, servicos: 1);
        await SemearAgendamentoComServicosAsync(filialId, baseInicio.AddHours(2), servicos: 1);

        await using var db = CarWashDbContextFactoryForTests.Create(_fixture);
        var repo = new AgendaRepository(db);

        var projecoes = await repo.ConsultarAsync(
            filialId,
            baseInicio.AddHours(-1),
            baseInicio.AddHours(6),
            clienteId: null,
            responsavelId: null,
            statusDb: null,
            CancellationToken.None);

        projecoes.Select(p => p.Inicio).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task ConsultarAsync_filtra_por_status_db()
    {
        var filialId = await SemearFilialAsync();
        var baseInicio = new DateTime(2026, 12, 1, 9, 0, 0, DateTimeKind.Utc);

        await SemearAgendamentoComServicosAsync(filialId, baseInicio, servicos: 1);
        await SemearAgendamentoComServicosAsync(filialId, baseInicio.AddHours(2), servicos: 1, cancelar: true);

        await using var db = CarWashDbContextFactoryForTests.Create(_fixture);
        var repo = new AgendaRepository(db);

        var cancelados = await repo.ConsultarAsync(
            filialId,
            baseInicio.AddHours(-1),
            baseInicio.AddHours(6),
            clienteId: null,
            responsavelId: null,
            statusDb: "cancelado",
            CancellationToken.None);

        cancelados.Should().ContainSingle();
        cancelados[0].Status.Should().Be("cancelado");
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    private async Task<Guid> SemearFilialAsync()
    {
        await using var db = CarWashDbContextFactoryForTests.Create(_fixture);
        var filial = Filial.Criar(Guid.NewGuid(), $"Filial {Guid.NewGuid():N}"[..30], $"F{Guid.NewGuid():N}"[..10].ToUpperInvariant(), 4);
        db.Filiais.Add(filial);
        await db.SaveChangesAsync();
        return filial.Id;
    }

    private async Task SemearAgendamentoComServicosAsync(
        Guid filialId,
        DateTime inicio,
        int servicos,
        bool cancelar = false)
    {
        await using var db = CarWashDbContextFactoryForTests.Create(_fixture);

        var cliente = ClienteValido();
        var veiculo = Veiculo.Criar(
            id: Guid.NewGuid(),
            clienteId: cliente.Id,
            placa: new Placa(GerarPlacaAleatoria()),
            modelo: "Civic",
            fabricante: "Honda",
            cor: "Preto");

        var agendamentoId = Guid.NewGuid();
        var itens = new List<AgendamentoItem>();
        var catalogos = new List<Servico>();
        int duracaoTotal = 0;
        decimal valorTotal = 0m;

        for (int i = 0; i < servicos; i++)
        {
            var servico = Servico.Criar(Guid.NewGuid(), $"Servico {Guid.NewGuid():N}"[..20], 50m, 30);
            var item = AgendamentoItem.Criar(Guid.NewGuid(), agendamentoId, servico.Id, 55m, 31);
            catalogos.Add(servico);
            itens.Add(item);
            duracaoTotal += 31;
            valorTotal += 55m;
        }

        var responsavel = ResponsavelValido(cliente.Id);

        var agendamento = Agendamento.Criar(
            id: agendamentoId,
            filialId: filialId,
            clienteId: cliente.Id,
            veiculoId: veiculo.Id,
            criadoPor: AdminId,
            inicio: inicio,
            fim: inicio.AddMinutes(Math.Max(duracaoTotal, 30)),
            responsavelId: responsavel.Id,
            observacoes: null,
            duracaoTotalMin: duracaoTotal,
            valorTotal: valorTotal);

        if (cancelar)
        {
            agendamento.Cancelar("Cancelado para fins de teste", Guid.NewGuid());
        }

        db.Clientes.Add(cliente);
        db.Veiculos.Add(veiculo);
        db.Responsaveis.Add(responsavel);
        db.Servicos.AddRange(catalogos);
        db.Agendamentos.Add(agendamento);
        db.AgendamentoItens.AddRange(itens);
        await db.SaveChangesAsync();
    }

    private static Responsavel ResponsavelValido(Guid clienteTitularId) => Responsavel.Criar(
        id: Guid.NewGuid(),
        clienteTitularId: clienteTitularId,
        nome: "Responsável Teste",
        documento: GerarCpfValido(),
        grauVinculo: GrauVinculo.ResponsavelFinanceiro);

    private static Cliente ClienteValido() => Cliente.Criar(
        id: Guid.NewGuid(),
        nome: "Cliente Teste",
        dataNascimento: new DateOnly(1990, 1, 1),
        celular: new Telefone("11987654321"),
        endereco: new Endereco(
            "01310100", "Av. Paulista", "1000", null, "Bela Vista", "São Paulo", "SP"),
        cpf: new Cpf(GerarCpfValido()));

    private static string GerarPlacaAleatoria()
    {
        var rng = Random.Shared;
        const string letras = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return $"{letras[rng.Next(26)]}{letras[rng.Next(26)]}{letras[rng.Next(26)]}"
            + $"{rng.Next(0, 10)}{letras[rng.Next(26)]}{rng.Next(0, 10)}{rng.Next(0, 10)}";
    }

    private static string GerarCpfValido()
    {
        Span<int> d = stackalloc int[11];
        var rng = Random.Shared;
        for (int i = 0; i < 9; i++)
        {
            d[i] = rng.Next(0, 10);
        }

        d[9] = Dv(d[..9], 10);
        d[10] = Dv(d[..10], 11);
        char[] chars = new char[11];
        for (int i = 0; i < 11; i++)
        {
            chars[i] = (char)('0' + d[i]);
        }

        return new string(chars);

        static int Dv(ReadOnlySpan<int> parcial, int pesoInicial)
        {
            int soma = 0;
            for (int i = 0; i < parcial.Length; i++)
            {
                soma += parcial[i] * (pesoInicial - i);
            }

            int resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
    }

    /// <summary>
    /// Interceptor de comando que conta os <c>SELECT</c> executados — usado para
    /// provar que a consulta de agenda não produz N+1.
    /// </summary>
    private sealed class ContadorDeComandos : DbCommandInterceptor
    {
        private int _totalSelects;

        public int TotalSelects => _totalSelects;

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Contar(command);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Contar(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        private void Contar(DbCommand command)
        {
            if (command.CommandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref _totalSelects);
            }
        }
    }
}
