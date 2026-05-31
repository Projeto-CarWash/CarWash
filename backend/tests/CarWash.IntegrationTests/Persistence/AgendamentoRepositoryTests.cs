using CarWash.Application.Agendamentos.Common;
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
using Xunit;

namespace CarWash.IntegrationTests.Persistence;

/// <summary>
/// Valida que o <see cref="AgendamentoRepository"/> traduz a violação da
/// constraint EXCLUDE <c>ex_ag_veiculo_janela</c> (RN011/CA006) em
/// <see cref="AgendamentoConflitanteException"/> — inclusive na race condition
/// que escapa do pré-check.
/// </summary>
[Collection(nameof(PostgresCollection))]
public class AgendamentoRepositoryTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private CarWashDbContext _db = null!;

    public AgendamentoRepositoryTests(PostgresFixture fixture)
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
    public async Task AdicionarAsync_persiste_agendamento_itens_e_historico()
    {
        var (filialId, clienteId, veiculoId, criadoPor, servicoId) = await SemearAsync();
        var repo = new AgendamentoRepository(_db);

        var inicio = DateTime.UtcNow.AddDays(1);
        var (agendamento, itens, historico) = MontarAgendamento(
            filialId, clienteId, veiculoId, criadoPor, servicoId, inicio);

        await repo.AdicionarAsync(agendamento, itens, historico, "trace-int", CancellationToken.None);

        await using var verificacao = CarWashDbContextFactoryForTests.Create(_fixture);
        (await verificacao.Agendamentos.AnyAsync(a => a.Id == agendamento.Id)).Should().BeTrue();
        (await verificacao.AgendamentoItens.CountAsync(i => i.AgendamentoId == agendamento.Id)).Should().Be(1);
        (await verificacao.AgendamentoHistoricos.AnyAsync(h => h.AgendamentoId == agendamento.Id)).Should().BeTrue();
        (await verificacao.AuditLogs.AnyAsync(l => l.EntidadeId == agendamento.Id && l.Evento == "AGENDAMENTO_CRIADO"))
            .Should().BeTrue();
    }

    [Fact]
    public async Task AdicionarAsync_em_conflito_de_janela_lanca_AgendamentoConflitanteException_RN011()
    {
        var (filialId, clienteId, veiculoId, criadoPor, servicoId) = await SemearAsync();
        var repo = new AgendamentoRepository(_db);

        var inicio = DateTime.UtcNow.AddDays(2);
        var (primeiro, itens1, hist1) = MontarAgendamento(filialId, clienteId, veiculoId, criadoPor, servicoId, inicio);
        await repo.AdicionarAsync(primeiro, itens1, hist1, "trace-1", CancellationToken.None);

        // Segundo agendamento do mesmo veículo com janela sobreposta — simula a
        // race condition que escapa do pré-check: vai direto ao banco.
        await using var db2 = CarWashDbContextFactoryForTests.Create(_fixture);
        var repo2 = new AgendamentoRepository(db2);
        var (segundo, itens2, hist2) = MontarAgendamento(
            filialId, clienteId, veiculoId, criadoPor, servicoId, inicio.AddMinutes(10));

        var act = () => repo2.AdicionarAsync(segundo, itens2, hist2, "trace-2", CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AgendamentoConflitanteException>();
        ex.Which.Slug.Should().Be("agendamento-conflito-veiculo");
    }

    [Fact]
    public async Task ExisteConflitoVeiculoAsync_detecta_sobreposicao_e_ignora_janelas_adjacentes()
    {
        var (filialId, clienteId, veiculoId, criadoPor, servicoId) = await SemearAsync();
        var repo = new AgendamentoRepository(_db);

        var inicio = DateTime.UtcNow.AddDays(3);
        var (existente, itens, hist) = MontarAgendamento(filialId, clienteId, veiculoId, criadoPor, servicoId, inicio);
        await repo.AdicionarAsync(existente, itens, hist, "trace-3", CancellationToken.None);

        var fim = existente.Fim;

        // Sobreposto → conflito.
        (await repo.ExisteConflitoVeiculoAsync(veiculoId, inicio.AddMinutes(5), fim.AddMinutes(5), CancellationToken.None))
            .Should().BeTrue();

        // Adjacente (começa exatamente no fim) → sem conflito (janela meio-aberta).
        (await repo.ExisteConflitoVeiculoAsync(veiculoId, fim, fim.AddMinutes(30), CancellationToken.None))
            .Should().BeFalse();

        // Outro veículo → sem conflito.
        (await repo.ExisteConflitoVeiculoAsync(Guid.NewGuid(), inicio, fim, CancellationToken.None))
            .Should().BeFalse();
    }

    private static (Agendamento Agendamento, IReadOnlyCollection<AgendamentoItem> Itens, AgendamentoHistorico Historico)
        MontarAgendamento(Guid filialId, Guid clienteId, Guid veiculoId, Guid criadoPor, Guid servicoId, DateTime inicio)
    {
        var id = Guid.NewGuid();
        var agendamento = Agendamento.Criar(
            id: id,
            filialId: filialId,
            clienteId: clienteId,
            veiculoId: veiculoId,
            criadoPor: criadoPor,
            inicio: inicio,
            fim: inicio.AddMinutes(30),
            duracaoTotalMin: 30,
            valorTotal: 30m);

        var itens = new[] { AgendamentoItem.Criar(Guid.NewGuid(), id, servicoId, 30m, 30) };
        var historico = AgendamentoHistorico.Registrar(Guid.NewGuid(), id, EventoHistorico.Criado, criadoPor);
        return (agendamento, itens, historico);
    }

    private async Task<(Guid FilialId, Guid ClienteId, Guid VeiculoId, Guid CriadoPor, Guid ServicoId)> SemearAsync()
    {
        var filial = Filial.Criar(Guid.NewGuid(), $"Filial {Guid.NewGuid():N}"[..30], $"F{Guid.NewGuid():N}"[..10].ToUpperInvariant(), 4);
        var cliente = Cliente.Criar(
            id: Guid.NewGuid(),
            nome: "Cliente Teste",
            dataNascimento: new DateOnly(1990, 1, 1),
            celular: new Telefone("11987654321"),
            endereco: new Endereco("01310100", "Av. Paulista", "1000", null, "Bela Vista", "São Paulo", "SP"),
            cpf: new Cpf(GerarCpfValido()));
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
            senhaHash: "$argon2id$v=19$m=65536,t=3,p=1$YWFhYWFhYWFhYWFhYWFhYQ$YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWE",
            perfil: PerfilUsuario.Funcionario);

        _db.Filiais.Add(filial);
        _db.Clientes.Add(cliente);
        _db.Veiculos.Add(veiculo);
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync().ConfigureAwait(false);

        var servicoId = await _db.Servicos.AsNoTracking().OrderBy(s => s.Nome).Select(s => s.Id).FirstAsync()
            .ConfigureAwait(false);

        return (filial.Id, cliente.Id, veiculo.Id, usuario.Id, servicoId);
    }

    private static string GerarPlacaAleatoria()
    {
        var rng = Random.Shared;
        const string letras = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return $"{letras[rng.Next(26)]}{letras[rng.Next(26)]}{letras[rng.Next(26)]}{rng.Next(0, 10)}{letras[rng.Next(26)]}{rng.Next(0, 10)}{rng.Next(0, 10)}";
    }

    private static string GerarCpfValido()
    {
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
