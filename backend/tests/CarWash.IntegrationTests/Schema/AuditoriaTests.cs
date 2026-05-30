using CarWash.Application.Abstractions;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using CarWash.Domain.ValueObjects;
using CarWash.Infrastructure.Auditing;
using CarWash.Infrastructure.Persistence;
using CarWash.Infrastructure.Persistence.Interceptors;
using CarWash.Infrastructure.Security;
using CarWash.IntegrationTests.Collections;
using CarWash.IntegrationTests.Fixtures;
using CarWash.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace CarWash.IntegrationTests.Schema;

/// <summary>
/// Auditoria de eventos críticos (DoD §8 / DAT §9.1).
/// Confere que <c>audit_logs</c> recebe linhas com <c>correlation_id</c> não-nulo
/// e que o mascaramento de campos sensíveis funciona.
/// </summary>
[Collection(nameof(PostgresCollection))]
public class AuditoriaTests
{
    private readonly PostgresFixture _fixture;

    public AuditoriaTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Login_falha_grava_audit_log_via_IAuditLogger()
    {
        AmbientRequestContext.Reset();
        AmbientRequestContext.DefinirCorrelationId("test-corr-login-falha");
        ICurrentRequestContext contexto = new AmbientRequestContext();

        var options = new DbContextOptionsBuilder<CarWashDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        IDbContextFactory<CarWashDbContext> dbFactory = new PooledDbContextFactory<CarWashDbContext>(options);
        var logger = new AuditLogger(dbFactory, contexto);

        await logger.LogAsync(
            evento: "UsuarioLoginFalha",
            entidade: "Usuario",
            entidadeId: null,
            dados: new { motivo = "senha_invalida", email = "x@local" }).ConfigureAwait(false);

        await using var verify = CarWashDbContextFactoryForTests.Create(_fixture);
        var log = await verify.AuditLogs
            .Where(x => x.CorrelationId == "test-corr-login-falha")
            .OrderByDescending(x => x.CriadoEm)
            .FirstAsync().ConfigureAwait(false);

        log.Evento.Should().Be("UsuarioLoginFalha");
        log.CorrelationId.Should().Be("test-corr-login-falha");

        // JSONB do Postgres normaliza espaços; comparar de forma resiliente.
        log.Dados.Should().Contain("\"motivo\"");
        log.Dados.Should().Contain("\"senha_invalida\"");
    }

    [Fact]
    public async Task Criacao_de_agendamento_grava_audit_log_pelo_interceptor()
    {
        AmbientRequestContext.Reset();
        AmbientRequestContext.DefinirCorrelationId("test-corr-ag-criado");
        ICurrentRequestContext contexto = new AmbientRequestContext();
        contexto.DefinirEvento("AgendamentoCriado");

        var interceptor = new AuditLogInterceptor(contexto);
        await using var db = CarWashDbContextFactoryForTests.Create(_fixture, interceptor);

        var (filialId, clienteId, veiculoId, criadoPor) = await SemearAsync(db).ConfigureAwait(false);

        var inicio = DateTime.UtcNow.AddHours(4);
        var ag = Agendamento.Criar(Guid.NewGuid(), filialId, clienteId, veiculoId, criadoPor, inicio, inicio.AddHours(1));
        db.Agendamentos.Add(ag);
        await db.SaveChangesAsync().ConfigureAwait(false);

        await using var verify = CarWashDbContextFactoryForTests.Create(_fixture);
        var logs = await verify.AuditLogs
            .Where(x => x.CorrelationId == "test-corr-ag-criado")
            .ToListAsync().ConfigureAwait(false);

        logs.Should().NotBeEmpty("o interceptor deve emitir audit_log para entidade auditável");
        logs.Should().Contain(l => l.Evento == "AgendamentoCriado" && l.Entidade == "Agendamento");
    }

    [Fact]
    public void Auditoria_mascara_campos_sensiveis()
    {
        var payload = new
        {
            senha = "minhasenha123",
            password = "minhasenha123",
            refresh_token = "ABCDEF",
            cpf = "39053344705",
            cnpj = "11222333000181",
            outro = "manter",
        };

        string json = AuditDataMasker.Mask(payload);
        json.Should().Contain("\"senha\":\"***\"");
        json.Should().Contain("\"password\":\"***\"");
        json.Should().Contain("\"refresh_token\":\"***\"");
        json.Should().Contain("\"cpf\":\"***.***.447-**\"");
        json.Should().Contain("\"cnpj\":\"**.***.***/****-81\"");
        json.Should().Contain("\"outro\":\"manter\"");
    }

    [Fact]
    public void Auditoria_nao_lanca_para_cpf_cnpj_nao_string()
    {
        var payload = new
        {
            cpf = 39053344705L,
            cnpj = new { valor = "11222333000181" },
        };

        string json = AuditDataMasker.Mask(payload);
        json.Should().Contain("\"cpf\":\"***.***.447-**\"");
        json.Should().Contain("\"cnpj\":\"**.***.***/****-81\"");
    }

    [Fact]
    public void Auditoria_aplica_mascara_generica_para_cpf_cnpj_nao_validos()
    {
        var payload = new
        {
            cpf = 123L,
            cnpj = new { valor = "123" },
        };

        string json = AuditDataMasker.Mask(payload);
        json.Should().Contain("\"cpf\":\"***\"");
        json.Should().Contain("\"cnpj\":\"***\"");
    }

    private static async Task<(Guid, Guid, Guid, Guid)> SemearAsync(CarWashDbContext db)
    {
        var filial = Filial.Criar(Guid.NewGuid(), $"FAud{Guid.NewGuid():N}".Substring(0, 30), 4);
        var cliente = Cliente.Criar(
            id: Guid.NewGuid(),
            nome: "Cliente Aud",
            dataNascimento: new DateOnly(1990, 1, 1),
            celular: new Telefone("11987654321"),
            endereco: new Endereco("01310100", "Av. Paulista", "1000", null, "Bela Vista", "São Paulo", "SP"),
            cpf: new Cpf(GerarCpfValido()));
        var veiculo = Veiculo.Criar(
            id: Guid.NewGuid(),
            clienteId: cliente.Id,
            placa: new Placa(GerarPlacaAleatoria()),
            modelo: "X",
            fabricante: "Y",
            cor: "Z");
        var usuario = Usuario.Criar(
            id: Guid.NewGuid(),
            nome: "Op",
            email: new Email($"au{Guid.NewGuid():N}@local.com"),
            senhaHash: "$argon2id$v=19$m=65536,t=3,p=1$YWFhYWFhYWFhYWFhYWFhYQ$YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWE",
            perfil: PerfilUsuario.Funcionario);

        db.Filiais.Add(filial);
        db.Clientes.Add(cliente);
        db.Veiculos.Add(veiculo);
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync().ConfigureAwait(false);
        return (filial.Id, cliente.Id, veiculo.Id, usuario.Id);
    }

    private static string GerarPlacaAleatoria()
    {
        var rng = Random.Shared;
        string letras = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
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
