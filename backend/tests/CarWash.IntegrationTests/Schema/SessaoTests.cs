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
/// Cobre DoD §8 — sessão expirada e revogada não renovam (regra de aplicação validada
/// via método de domínio <see cref="UsuarioSessao.EstaAtiva(DateTime)"/>).
/// </summary>
[Collection(nameof(PostgresCollection))]
public class SessaoTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private CarWashDbContext _db = null!;

    public SessaoTests(PostgresFixture fixture)
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
    public async Task Sessao_revogada_nao_renova()
    {
        var usuario = await CriarUsuarioAsync().ConfigureAwait(false);
        var sessao = UsuarioSessao.Criar(
            id: Guid.NewGuid(),
            usuarioId: usuario.Id,
            refreshTokenHash: new string('a', 64),
            expiraEm: DateTime.UtcNow.AddDays(30));

        _db.UsuarioSessoes.Add(sessao);
        await _db.SaveChangesAsync().ConfigureAwait(false);

        sessao.Revogar(DateTime.UtcNow);
        await _db.SaveChangesAsync().ConfigureAwait(false);

        var recarregada = await _db.UsuarioSessoes.SingleAsync(s => s.Id == sessao.Id).ConfigureAwait(false);
        recarregada.EstaAtiva(DateTime.UtcNow).Should().BeFalse("sessão revogada não pode renovar");
        recarregada.RevogadoEm.Should().NotBeNull();
    }

    [Fact]
    public async Task Sessao_expirada_nao_renova()
    {
        var usuario = await CriarUsuarioAsync().ConfigureAwait(false);

        // Insert direto (o construtor exige expira_em > now()).
        await using var conn = new Npgsql.NpgsqlConnection(_fixture.ConnectionString);
        await conn.OpenAsync().ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        var id = Guid.NewGuid();
        cmd.CommandText =
            "INSERT INTO public.usuario_sessoes (id, usuario_id, refresh_token_hash, expira_em, criado_em) "
            + $"VALUES ('{id}', '{usuario.Id}', repeat('a', 64), now() - interval '1 minute', now());";
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

        var sessao = await _db.UsuarioSessoes.SingleAsync(s => s.Id == id).ConfigureAwait(false);
        sessao.EstaAtiva(DateTime.UtcNow).Should().BeFalse("sessão com expira_em no passado não pode renovar");
    }

    private async Task<Usuario> CriarUsuarioAsync()
    {
        var u = Usuario.Criar(
            id: Guid.NewGuid(),
            nome: "Usuario Sessao",
            email: new Email($"sess{Guid.NewGuid():N}@local.com"),
            senhaHash: "$argon2id$v=19$m=65536,t=3,p=1$YWFhYWFhYWFhYWFhYWFhYQ$YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWE",
            perfil: PerfilUsuario.Funcionario);
        _db.Usuarios.Add(u);
        await _db.SaveChangesAsync().ConfigureAwait(false);
        return u;
    }
}
