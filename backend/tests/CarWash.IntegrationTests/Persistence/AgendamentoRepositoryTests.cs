using CarWash.Application.Agendamentos.Common;
using CarWash.Application.Common.Exceptions;
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
using Microsoft.Extensions.Logging.Abstractions;
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

    /// <inheritdoc/>
    public Task InitializeAsync()
    {
        _db = CarWashDbContextFactoryForTests.Create(_fixture);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task DisposeAsync() => await _db.DisposeAsync().ConfigureAwait(false);

    [Fact]
    public async Task AdicionarAsync_persiste_agendamento_itens_e_historico()
    {
        var (filialId, clienteId, veiculoId, criadoPor, servicoId, responsavelId) = await SemearAsync();
        var repo = new AgendamentoRepository(_db, NullLogger<AgendamentoRepository>.Instance);

        var inicio = DateTime.UtcNow.AddDays(1);
        var (agendamento, itens, historico) = MontarAgendamento(
            filialId, clienteId, veiculoId, criadoPor, servicoId, responsavelId, inicio);

        await repo.AdicionarAsync(agendamento, itens, historico, "trace-int",
            responsavelId, clienteId, CancellationToken.None);

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
        var (filialId, clienteId, veiculoId, criadoPor, servicoId, responsavelId) = await SemearAsync();
        var repo = new AgendamentoRepository(_db, NullLogger<AgendamentoRepository>.Instance);

        var inicio = DateTime.UtcNow.AddDays(2);
        var (primeiro, itens1, hist1) = MontarAgendamento(filialId, clienteId, veiculoId, criadoPor, servicoId, responsavelId, inicio);
        await repo.AdicionarAsync(primeiro, itens1, hist1, "trace-1",
            responsavelId, clienteId, CancellationToken.None);

        // Segundo agendamento do mesmo veículo com janela sobreposta — simula a
        // race condition que escapa do pré-check: vai direto ao banco.
        await using var db2 = CarWashDbContextFactoryForTests.Create(_fixture);
        var repo2 = new AgendamentoRepository(db2, NullLogger<AgendamentoRepository>.Instance);
        var (segundo, itens2, hist2) = MontarAgendamento(
            filialId, clienteId, veiculoId, criadoPor, servicoId, responsavelId, inicio.AddMinutes(10));

        var act = () => repo2.AdicionarAsync(segundo, itens2, hist2, "trace-2",
            responsavelId, clienteId, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AgendamentoConflitanteException>();
        ex.Which.Slug.Should().Be("agendamento-conflito-veiculo");
    }

    [Fact]
    public async Task ExisteConflitoVeiculoAsync_detecta_sobreposicao_e_ignora_janelas_adjacentes()
    {
        var (filialId, clienteId, veiculoId, criadoPor, servicoId, responsavelId) = await SemearAsync();
        var repo = new AgendamentoRepository(_db, NullLogger<AgendamentoRepository>.Instance);

        var inicio = DateTime.UtcNow.AddDays(3);
        var (existente, itens, hist) = MontarAgendamento(filialId, clienteId, veiculoId, criadoPor, servicoId, responsavelId, inicio);
        await repo.AdicionarAsync(existente, itens, hist, "trace-3",
            responsavelId, clienteId, CancellationToken.None);

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

    [Fact]
    public async Task AdicionarAsync_vinculo_responsavel_alterado_concorrentemente_lanca_ConflictException_CA009()
    {
        // RF024/CA009: a transação do agendamento revalida o vínculo
        // responsável→cliente sob SELECT FOR UPDATE. Se uma transação
        // concorrente alterou cliente_titular_id entre o pre-check
        // (AsNoTracking) e o COMMIT, o agendamento é rejeitado com 409.
        var (filialId, clienteId, veiculoId, criadoPor, servicoId, responsavelId) = await SemearAsync();
        var outroClienteId = Guid.NewGuid();

        // Cria um segundo cliente titular para simular a transferência.
        await using (var dbSetup = CarWashDbContextFactoryForTests.Create(_fixture))
        {
            var outroCliente = Cliente.Criar(
                id: outroClienteId,
                nome: "Outro Cliente",
                dataNascimento: new DateOnly(1985, 5, 15),
                celular: new Telefone("11998765432"),
                endereco: new Endereco("01310100", "Rua Augusta", "500", null, "Consolação", "São Paulo", "SP"),
                cpf: new Cpf(GerarCpfValido()));
            dbSetup.Clientes.Add(outroCliente);
            await dbSetup.SaveChangesAsync();
        }

        // Altera o cliente_titular_id do responsável via SQL direto
        // (simula operação administrativa concorrente que o domínio
        // ainda não expõe via método).
        await using (var dbAlt = CarWashDbContextFactoryForTests.Create(_fixture))
        {
            await dbAlt.Database.ExecuteSqlRawAsync(
                "UPDATE public.responsaveis SET cliente_titular_id = {0} WHERE id = {1}",
                outroClienteId, responsavelId);
        }

        // Tenta criar o agendamento com o clienteId original — o
        // SELECT FOR UPDATE dentro da transação detecta que o vínculo
        // foi alterado e lança ConflictException.
        await using var db = CarWashDbContextFactoryForTests.Create(_fixture);
        var repo = new AgendamentoRepository(db);
        var inicio = DateTime.UtcNow.AddDays(4);
        var (agendamento, itens, historico) = MontarAgendamento(
            filialId, clienteId, veiculoId, criadoPor, servicoId, responsavelId, inicio);

        var act = () => repo.AdicionarAsync(agendamento, itens, historico, "trace-ca009",
            responsavelId, clienteId, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Slug.Should().Be("responsavel-nao-vinculado");

        // Nenhum agendamento parcial persiste — rollback total.
        await using var dbVerificacao = CarWashDbContextFactoryForTests.Create(_fixture);
        (await dbVerificacao.Agendamentos.AnyAsync(a => a.Id == agendamento.Id))
            .Should().BeFalse("o agendamento rejeitado não deve persistir");
        (await dbVerificacao.AgendamentoItens.AnyAsync(i => i.AgendamentoId == agendamento.Id))
            .Should().BeFalse("nenhum item órfão deve persistir");
    }

    [Fact]
    public async Task AdicionarAsync_responsavel_inativado_concorrentemente_lanca_RecursoInativoException_CA009()
    {
        // RF024/CA009: se o responsável for inativado concorrentemente,
        // o SELECT FOR UPDATE detecta ativo=false e lança
        // RecursoInativoException — rollback total.
        var (filialId, clienteId, veiculoId, criadoPor, servicoId, responsavelId) = await SemearAsync();

        await using (var dbAlt = CarWashDbContextFactoryForTests.Create(_fixture))
        {
            await dbAlt.Database.ExecuteSqlRawAsync(
                "UPDATE public.responsaveis SET ativo = false WHERE id = {0}",
                responsavelId);
        }

        await using var db = CarWashDbContextFactoryForTests.Create(_fixture);
        var repo = new AgendamentoRepository(db);
        var inicio = DateTime.UtcNow.AddDays(5);
        var (agendamento, itens, historico) = MontarAgendamento(
            filialId, clienteId, veiculoId, criadoPor, servicoId, responsavelId, inicio);

        var act = () => repo.AdicionarAsync(agendamento, itens, historico, "trace-ca009-inativo",
            responsavelId, clienteId, CancellationToken.None);

        await act.Should().ThrowAsync<RecursoInativoException>();

        await using var dbVerificacao = CarWashDbContextFactoryForTests.Create(_fixture);
        (await dbVerificacao.Agendamentos.AnyAsync(a => a.Id == agendamento.Id))
            .Should().BeFalse("o agendamento rejeitado não deve persistir");
    }

    private static (Agendamento Agendamento, IReadOnlyCollection<AgendamentoItem> Itens, AgendamentoHistorico Historico)
        MontarAgendamento(Guid filialId, Guid clienteId, Guid veiculoId, Guid criadoPor, Guid servicoId, Guid responsavelId, DateTime inicio)
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
            responsavelId: responsavelId,
            duracaoTotalMin: 30,
            valorTotal: 30m);

        var itens = new[] { AgendamentoItem.Criar(Guid.NewGuid(), id, servicoId, 30m, 30) };
        var historico = AgendamentoHistorico.Registrar(Guid.NewGuid(), id, EventoHistorico.Criado, criadoPor);
        return (agendamento, itens, historico);
    }

    private async Task<(Guid FilialId, Guid ClienteId, Guid VeiculoId, Guid CriadoPor, Guid ServicoId, Guid ResponsavelId)> SemearAsync()
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
        var responsavel = Responsavel.Criar(
            id: Guid.NewGuid(),
            clienteTitularId: cliente.Id,
            nome: "Responsavel Teste",
            documento: GerarCpfValido(),
            grauVinculo: GrauVinculo.ResponsavelFinanceiro);
        var usuario = Usuario.Criar(
            id: Guid.NewGuid(),
            nome: "Operador",
            email: new Email($"op{Guid.NewGuid():N}@local.com"),
            senhaHash: "$argon2id$v=19$m=65536,t=3,p=1$YWFhYWFhYWFhYWFhYWFhYQ$YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWE",
            perfil: PerfilUsuario.Funcionario);

        _db.Filiais.Add(filial);
        _db.Clientes.Add(cliente);
        _db.Veiculos.Add(veiculo);
        _db.Responsaveis.Add(responsavel);
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync().ConfigureAwait(false);

        var servicoId = await _db.Servicos.AsNoTracking().OrderBy(s => s.Nome).Select(s => s.Id).FirstAsync()
            .ConfigureAwait(false);

        return (filial.Id, cliente.Id, veiculo.Id, usuario.Id, servicoId, responsavel.Id);
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
}
