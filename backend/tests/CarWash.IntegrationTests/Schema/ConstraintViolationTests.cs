using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using CarWash.Domain.ValueObjects;
using CarWash.Infrastructure.Persistence;
using CarWash.IntegrationTests.Collections;
using CarWash.IntegrationTests.Fixtures;
using CarWash.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace CarWash.IntegrationTests.Schema;

/// <summary>
/// Testes de violação esperada das constraints do schema (inserts ilegais).
/// </summary>
[Collection(nameof(PostgresCollection))]
public class ConstraintViolationTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private CarWashDbContext _db = null!;

    public ConstraintViolationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _db = CarWashDbContextFactoryForTests.Create(_fixture);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _db.DisposeAsync().ConfigureAwait(false);

    [Fact]
    public async Task Email_duplicado_em_usuarios_falha_com_uk_usuarios_email()
    {
        var u1 = Usuario.Criar(
            id: Guid.NewGuid(),
            nome: "Funcionario 1",
            email: new Email("dup@local.com"),
            senhaHash: HashPlaceholder(),
            perfil: PerfilUsuario.Funcionario);

        var u2 = Usuario.Criar(
            id: Guid.NewGuid(),
            nome: "Funcionario 2",
            email: new Email("dup@local.com"),
            senhaHash: HashPlaceholder(),
            perfil: PerfilUsuario.Funcionario);

        _db.Usuarios.Add(u1);
        await _db.SaveChangesAsync().ConfigureAwait(false);

        _db.Usuarios.Add(u2);
        var ex = await Record.ExceptionAsync(() => _db.SaveChangesAsync()).ConfigureAwait(false);
        ex.Should().NotBeNull();
        ex!.InnerException.Should().BeOfType<PostgresException>();
        ((PostgresException)ex.InnerException!).ConstraintName.Should().Be("uk_usuarios_email");
    }

    [Fact]
    public async Task Placa_duplicada_em_veiculos_falha_com_uk_veiculos_placa()
    {
        var cliente = ClienteValido();
        _db.Clientes.Add(cliente);
        await _db.SaveChangesAsync().ConfigureAwait(false);

        var v1 = Veiculo.Criar(
            id: Guid.NewGuid(),
            clienteId: cliente.Id,
            placa: new Placa("ABC1D23"),
            modelo: "Civic",
            fabricante: "Honda",
            cor: "Preto");

        var v2 = Veiculo.Criar(
            id: Guid.NewGuid(),
            clienteId: cliente.Id,
            placa: new Placa("abc1d23"),
            modelo: "Onix",
            fabricante: "Chevrolet",
            cor: "Branco");

        _db.Veiculos.Add(v1);
        await _db.SaveChangesAsync().ConfigureAwait(false);

        _db.Veiculos.Add(v2);
        var ex = await Record.ExceptionAsync(() => _db.SaveChangesAsync()).ConfigureAwait(false);
        ex.Should().NotBeNull();
        ex!.InnerException.Should().BeOfType<PostgresException>();
        ((PostgresException)ex.InnerException!).ConstraintName.Should().Be("uk_veiculos_placa");
    }

    [Fact]
    public async Task Celulas_ativas_zero_falha_com_ck_filiais_celulas_faixa()
    {
        // Bypass intencional do domain (que já bloqueia) para validar o CHECK.
        await using var conn = new NpgsqlConnection(_fixture.ConnectionString);
        await conn.OpenAsync().ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "INSERT INTO public.filiais (id, nome, ativa, celulas_ativas, timezone) "
            + "VALUES (gen_random_uuid_or_id(), 'F0', true, 0, 'America/Sao_Paulo');";
        cmd.CommandText = cmd.CommandText.Replace("gen_random_uuid_or_id()", $"'{Guid.NewGuid()}'", StringComparison.Ordinal);
        var ex = await Record.ExceptionAsync(() => cmd.ExecuteNonQueryAsync()).ConfigureAwait(false);
        ex.Should().BeOfType<PostgresException>();
        ((PostgresException)ex!).ConstraintName.Should().Be("ck_filiais_celulas_faixa");
    }

    [Fact]
    public async Task Celulas_ativas_acima_de_100_falha_com_ck_filiais_celulas_faixa()
    {
        await using var conn = new NpgsqlConnection(_fixture.ConnectionString);
        await conn.OpenAsync().ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            $"INSERT INTO public.filiais (id, nome, ativa, celulas_ativas, timezone) "
            + $"VALUES ('{Guid.NewGuid()}', 'F101', true, 101, 'America/Sao_Paulo');";
        var ex = await Record.ExceptionAsync(() => cmd.ExecuteNonQueryAsync()).ConfigureAwait(false);
        ex.Should().BeOfType<PostgresException>();
        ((PostgresException)ex!).ConstraintName.Should().Be("ck_filiais_celulas_faixa");
    }

    [Fact]
    public async Task Agendamento_sem_filial_falha_com_not_null()
    {
        await using var conn = new NpgsqlConnection(_fixture.ConnectionString);
        await conn.OpenAsync().ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "INSERT INTO public.agendamentos (id, filial_id, cliente_id, veiculo_id, criado_por, status, inicio, fim) "
            + $"VALUES ('{Guid.NewGuid()}', NULL, '{Guid.NewGuid()}', '{Guid.NewGuid()}', '{Guid.NewGuid()}', 'agendado', now(), now() + interval '1 hour');";
        var ex = await Record.ExceptionAsync(() => cmd.ExecuteNonQueryAsync()).ConfigureAwait(false);
        ex.Should().BeOfType<PostgresException>();
        ((PostgresException)ex!).SqlState.Should().Be("23502"); // not_null_violation
    }

    [Fact]
    public async Task Agendamento_inicio_igual_ou_maior_que_fim_falha_com_ck_ag_inicio_menor_fim()
    {
        var (filialId, clienteId, veiculoId, criadoPor) = await SemearAgendamentoDependenciasAsync().ConfigureAwait(false);

        var inicio = DateTime.UtcNow.AddHours(1);
        await using var conn = new NpgsqlConnection(_fixture.ConnectionString);
        await conn.OpenAsync().ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "INSERT INTO public.agendamentos (id, filial_id, cliente_id, veiculo_id, criado_por, status, inicio, fim) "
            + $"VALUES ('{Guid.NewGuid()}', '{filialId}', '{clienteId}', '{veiculoId}', '{criadoPor}', 'agendado', '{inicio:O}', '{inicio:O}');";
        var ex = await Record.ExceptionAsync(() => cmd.ExecuteNonQueryAsync()).ConfigureAwait(false);
        ex.Should().BeOfType<PostgresException>();
        ((PostgresException)ex!).ConstraintName.Should().Be("ck_ag_inicio_menor_fim");
    }

    [Fact]
    public async Task Servico_duplicado_no_mesmo_agendamento_falha_com_uk_item_agendamento_servico()
    {
        var (filialId, clienteId, veiculoId, criadoPor) = await SemearAgendamentoDependenciasAsync().ConfigureAwait(false);

        var inicio = DateTime.UtcNow.AddHours(1);
        var agendamento = Agendamento.Criar(
            id: Guid.NewGuid(),
            filialId: filialId,
            clienteId: clienteId,
            veiculoId: veiculoId,
            criadoPor: criadoPor,
            inicio: inicio,
            fim: inicio.AddHours(1));
        _db.Agendamentos.Add(agendamento);
        await _db.SaveChangesAsync().ConfigureAwait(false);

        var servicoId = await _db.Servicos.OrderBy(s => s.Nome).Select(s => s.Id).FirstAsync().ConfigureAwait(false);

        var item1 = AgendamentoItem.Criar(Guid.NewGuid(), agendamento.Id, servicoId, 30m, 30);
        var item2 = AgendamentoItem.Criar(Guid.NewGuid(), agendamento.Id, servicoId, 30m, 30);

        _db.AgendamentoItens.Add(item1);
        await _db.SaveChangesAsync().ConfigureAwait(false);

        _db.AgendamentoItens.Add(item2);
        var ex = await Record.ExceptionAsync(() => _db.SaveChangesAsync()).ConfigureAwait(false);
        ex!.InnerException.Should().BeOfType<PostgresException>();
        ((PostgresException)ex.InnerException!).ConstraintName.Should().Be("uk_item_agendamento_servico");
    }

    [Fact]
    public async Task Conflito_mesmo_veiculo_mesma_filial_falha_com_ex_ag_veiculo_janela()
    {
        var (filialId, clienteId, veiculoId, criadoPor) = await SemearAgendamentoDependenciasAsync().ConfigureAwait(false);

        var inicio = DateTime.UtcNow.AddHours(1);
        var a1 = Agendamento.Criar(Guid.NewGuid(), filialId, clienteId, veiculoId, criadoPor, inicio, inicio.AddHours(1));
        var a2 = Agendamento.Criar(Guid.NewGuid(), filialId, clienteId, veiculoId, criadoPor, inicio.AddMinutes(30), inicio.AddMinutes(90));

        _db.Agendamentos.Add(a1);
        await _db.SaveChangesAsync().ConfigureAwait(false);

        _db.Agendamentos.Add(a2);
        var ex = await Record.ExceptionAsync(() => _db.SaveChangesAsync()).ConfigureAwait(false);
        ex!.InnerException.Should().BeOfType<PostgresException>();
        ((PostgresException)ex.InnerException!).ConstraintName.Should().Be("ex_ag_veiculo_janela");
    }

    [Fact]
    public async Task Conflito_global_RN011_em_filiais_diferentes_ainda_falha()
    {
        var (filialId1, clienteId, veiculoId, criadoPor) = await SemearAgendamentoDependenciasAsync().ConfigureAwait(false);
        var filialId2 = Guid.NewGuid();
        _db.Filiais.Add(Filial.Criar(filialId2, "Filial 2", 3));
        await _db.SaveChangesAsync().ConfigureAwait(false);

        var inicio = DateTime.UtcNow.AddHours(2);
        var a1 = Agendamento.Criar(Guid.NewGuid(), filialId1, clienteId, veiculoId, criadoPor, inicio, inicio.AddHours(1));
        var a2 = Agendamento.Criar(Guid.NewGuid(), filialId2, clienteId, veiculoId, criadoPor, inicio.AddMinutes(15), inicio.AddMinutes(75));

        _db.Agendamentos.Add(a1);
        await _db.SaveChangesAsync().ConfigureAwait(false);

        _db.Agendamentos.Add(a2);
        var ex = await Record.ExceptionAsync(() => _db.SaveChangesAsync()).ConfigureAwait(false);
        ex!.InnerException.Should().BeOfType<PostgresException>();
        ((PostgresException)ex.InnerException!).ConstraintName.Should().Be("ex_ag_veiculo_janela");
    }

    [Fact]
    public async Task Janelas_adjacentes_meio_abertas_passam()
    {
        var (filialId, clienteId, veiculoId, criadoPor) = await SemearAgendamentoDependenciasAsync().ConfigureAwait(false);

        var inicio = DateTime.UtcNow.AddHours(3);
        var a1 = Agendamento.Criar(Guid.NewGuid(), filialId, clienteId, veiculoId, criadoPor, inicio, inicio.AddHours(1));
        var a2 = Agendamento.Criar(Guid.NewGuid(), filialId, clienteId, veiculoId, criadoPor, inicio.AddHours(1), inicio.AddHours(2));

        _db.Agendamentos.Add(a1);
        await _db.SaveChangesAsync().ConfigureAwait(false);

        _db.Agendamentos.Add(a2);
        await _db.SaveChangesAsync().ConfigureAwait(false);

        var total = await _db.Agendamentos
            .CountAsync(x => x.VeiculoId == veiculoId && (x.Id == a1.Id || x.Id == a2.Id))
            .ConfigureAwait(false);
        total.Should().Be(2);
    }

    private async Task<(Guid filialId, Guid clienteId, Guid veiculoId, Guid criadoPor)> SemearAgendamentoDependenciasAsync()
    {
        var filial = Filial.Criar(Guid.NewGuid(), $"Filial {Guid.NewGuid():N}".Substring(0, 30), 4);
        var cliente = ClienteValido();
        var veiculo = Veiculo.Criar(
            id: Guid.NewGuid(),
            clienteId: cliente.Id,
            placa: new Placa(GerarPlacaAleatoria()),
            modelo: "Civic",
            fabricante: "Honda",
            cor: "Preto");

        var usuario = Usuario.Criar(
            id: Guid.NewGuid(),
            nome: "Operador",
            email: new Email($"op{Guid.NewGuid():N}@local.com"),
            senhaHash: HashPlaceholder(),
            perfil: PerfilUsuario.Funcionario);

        _db.Filiais.Add(filial);
        _db.Clientes.Add(cliente);
        _db.Veiculos.Add(veiculo);
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync().ConfigureAwait(false);

        return (filial.Id, cliente.Id, veiculo.Id, usuario.Id);
    }

    private static Cliente ClienteValido() => Cliente.Criar(
        id: Guid.NewGuid(),
        nome: "Cliente Teste",
        cpf: new Cpf(GerarCpfValido()));

    private static string HashPlaceholder() =>
        "$argon2id$v=19$m=65536,t=3,p=1$YWFhYWFhYWFhYWFhYWFhYQ$YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWE";

    private static string GerarPlacaAleatoria()
    {
        var rng = Random.Shared;
        var letras = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return $"{letras[rng.Next(26)]}{letras[rng.Next(26)]}{letras[rng.Next(26)]}{rng.Next(0, 10)}{letras[rng.Next(26)]}{rng.Next(0, 10)}{rng.Next(0, 10)}";
    }

    private static string GerarCpfValido()
    {
        // Gera 9 dígitos aleatórios e calcula DVs. Garante unicidade por teste.
        Span<int> d = stackalloc int[11];
        var rng = Random.Shared;
        for (var i = 0; i < 9; i++)
        {
            d[i] = rng.Next(0, 10);
        }

        d[9] = Dv(d[..9], 10);
        d[10] = Dv(d[..10], 11);
        var chars = new char[11];
        for (var i = 0; i < 11; i++)
        {
            chars[i] = (char)('0' + d[i]);
        }

        return new string(chars);

        static int Dv(ReadOnlySpan<int> parcial, int pesoInicial)
        {
            var soma = 0;
            for (var i = 0; i < parcial.Length; i++)
            {
                soma += parcial[i] * (pesoInicial - i);
            }

            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
    }
}
