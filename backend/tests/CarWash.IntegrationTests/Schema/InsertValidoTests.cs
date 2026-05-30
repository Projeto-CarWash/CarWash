using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using CarWash.Domain.ValueObjects;
using CarWash.Infrastructure.Persistence;
using CarWash.IntegrationTests.Collections;
using CarWash.IntegrationTests.Fixtures;
using CarWash.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarWash.IntegrationTests.Schema;

/// <summary>
/// DoD §8 "Insert válido em todas as 15 tabelas" — exercita um insert
/// no caminho válido para cada uma.
/// </summary>
[Collection(nameof(PostgresCollection))]
public class InsertValidoTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private CarWashDbContext _db = null!;

    public InsertValidoTests(PostgresFixture fixture)
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
    public async Task Insere_uma_linha_em_cada_uma_das_15_tabelas()
    {
        // Filial
        var filial = Filial.Criar(Guid.NewGuid(), $"F{Guid.NewGuid():N}".Substring(0, 30), 5);
        _db.Filiais.Add(filial);

        // Usuario
        var usuario = Usuario.Criar(
            id: Guid.NewGuid(),
            nome: "Func Teste",
            email: new Email($"f{Guid.NewGuid():N}@local.com"),
            senhaHash: "$argon2id$v=19$m=65536,t=3,p=1$YWFhYWFhYWFhYWFhYWFhYQ$YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWE",
            perfil: PerfilUsuario.Funcionario);
        _db.Usuarios.Add(usuario);

        // Cliente
        var cliente = Cliente.Criar(
            id: Guid.NewGuid(),
            nome: "Cliente Teste",
            dataNascimento: new DateOnly(1990, 1, 1),
            celular: new Telefone("11987654321"),
            endereco: new Endereco("01310100", "Av. Paulista", "1000", null, "Bela Vista", "São Paulo", "SP"),
            cpf: new Cpf(GerarCpfValido()));
        _db.Clientes.Add(cliente);

        // Servico
        var servico = Servico.Criar(Guid.NewGuid(), $"S{Guid.NewGuid():N}".Substring(0, 30), 50m, 45);
        _db.Servicos.Add(servico);

        await _db.SaveChangesAsync().ConfigureAwait(false);

        // Filiado (FK Cliente)
        var filiado = Filiado.Criar(
            id: Guid.NewGuid(),
            clienteId: cliente.Id,
            nome: "Resp",
            telefone: new Telefone("11999990000"),
            cpf: new Cpf(GerarCpfValido()));
        _db.Filiados.Add(filiado);

        // Veiculo
        var veiculo = Veiculo.Criar(
            id: Guid.NewGuid(),
            clienteId: cliente.Id,
            placa: new Placa(GerarPlacaAleatoria()),
            modelo: "Onix",
            fabricante: "Chevrolet",
            cor: "Preto",
            ano: 2023);
        _db.Veiculos.Add(veiculo);

        // Sessao
        var sessao = UsuarioSessao.Criar(
            id: Guid.NewGuid(),
            usuarioId: usuario.Id,
            refreshTokenHash: new string('b', 64),
            expiraEm: DateTime.UtcNow.AddDays(30),
            ipOrigem: "127.0.0.1",
            userAgent: "test-agent");
        _db.UsuarioSessoes.Add(sessao);

        // Preferencia
        var preferencia = UsuarioPreferencia.Criar(Guid.NewGuid(), usuario.Id, TemaPreferencia.Escuro);
        _db.UsuarioPreferencias.Add(preferencia);

        await _db.SaveChangesAsync().ConfigureAwait(false);

        // Agendamento
        var inicio = DateTime.UtcNow.AddHours(5);
        var ag = Agendamento.Criar(
            id: Guid.NewGuid(),
            filialId: filial.Id,
            clienteId: cliente.Id,
            veiculoId: veiculo.Id,
            criadoPor: usuario.Id,
            inicio: inicio,
            fim: inicio.AddHours(1),
            duracaoTotalMin: 30,
            valorTotal: 50m,
            responsavelId: filiado.Id,
            observacoes: "teste");
        _db.Agendamentos.Add(ag);
        await _db.SaveChangesAsync().ConfigureAwait(false);

        // Item
        var item = AgendamentoItem.Criar(Guid.NewGuid(), ag.Id, servico.Id, 50m, 45);
        _db.AgendamentoItens.Add(item);

        // Historico
        var hist = AgendamentoHistorico.Registrar(Guid.NewGuid(), ag.Id, EventoHistorico.Criado, usuario.Id, "{}");
        _db.AgendamentoHistoricos.Add(hist);

        // Feature flag
        var flag = FeatureFlag.Criar(Guid.NewGuid(), "demo", "dev", usuario.Id, filial.Id, habilitada: true);
        _db.FeatureFlags.Add(flag);

        // Audit log
        var audit = AuditLog.Registrar(
            id: Guid.NewGuid(),
            evento: "Teste",
            entidade: "Agendamento",
            correlationId: "corr-insert-valido",
            entidadeId: ag.Id,
            usuarioId: usuario.Id);
        _db.AuditLogs.Add(audit);

        // Outbox
        var outbox = OutboxEvento.Criar(
            id: Guid.NewGuid(),
            evento: "AgendamentoCriado",
            agregado: "Agendamento",
            agregadoId: ag.Id,
            payload: "{\"id\":\"" + ag.Id + "\"}",
            idempotencyKey: $"k-{Guid.NewGuid():N}");
        _db.OutboxEventos.Add(outbox);

        // Notificacao
        var notif = Notificacao.Criar(
            id: Guid.NewGuid(),
            agendamentoId: ag.Id,
            tipo: "lembrete",
            canal: "email",
            destino: "cliente@local.com",
            idempotencyKey: $"n-{Guid.NewGuid():N}");
        _db.Notificacoes.Add(notif);

        await _db.SaveChangesAsync().ConfigureAwait(false);

        // Confirma persistência de uma linha de cada (entidades transacionais inclusas).
        (await _db.Usuarios.CountAsync().ConfigureAwait(false)).Should().BeGreaterThan(0);
        (await _db.Filiais.CountAsync().ConfigureAwait(false)).Should().BeGreaterThan(0);
        (await _db.Clientes.CountAsync().ConfigureAwait(false)).Should().BeGreaterThan(0);
        (await _db.Filiados.CountAsync().ConfigureAwait(false)).Should().BeGreaterThan(0);
        (await _db.Veiculos.CountAsync().ConfigureAwait(false)).Should().BeGreaterThan(0);
        (await _db.Servicos.CountAsync().ConfigureAwait(false)).Should().BeGreaterThan(0);
        (await _db.Agendamentos.CountAsync().ConfigureAwait(false)).Should().BeGreaterThan(0);
        (await _db.AgendamentoItens.CountAsync().ConfigureAwait(false)).Should().BeGreaterThan(0);
        (await _db.AgendamentoHistoricos.CountAsync().ConfigureAwait(false)).Should().BeGreaterThan(0);
        (await _db.UsuarioSessoes.CountAsync().ConfigureAwait(false)).Should().BeGreaterThan(0);
        (await _db.UsuarioPreferencias.CountAsync().ConfigureAwait(false)).Should().BeGreaterThan(0);
        (await _db.FeatureFlags.CountAsync().ConfigureAwait(false)).Should().BeGreaterThan(0);
        (await _db.AuditLogs.CountAsync().ConfigureAwait(false)).Should().BeGreaterThan(0);
        (await _db.OutboxEventos.CountAsync().ConfigureAwait(false)).Should().BeGreaterThan(0);
        (await _db.Notificacoes.CountAsync().ConfigureAwait(false)).Should().BeGreaterThan(0);
    }

    private static string GerarPlacaAleatoria()
    {
        var rng = Random.Shared;
        var letras = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
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
