using CarWash.IntegrationTests.Collections;
using CarWash.IntegrationTests.Fixtures;
using CarWash.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarWash.IntegrationTests.Schema;

[Collection(nameof(PostgresCollection))]
public class SeedTests
{
    private readonly PostgresFixture _fixture;

    public SeedTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Seed_inseriu_administrador_com_hash_argon2id_phc()
    {
        await using var db = CarWashDbContextFactoryForTests.Create(_fixture);

        var admin = await db.Usuarios
            .SingleOrDefaultAsync(u => u.Id == new Guid("00000000-0000-0000-0000-000000000001"))
            .ConfigureAwait(false);

        admin.Should().NotBeNull();
        admin!.EmailValor.Should().Be("admin@carwash.local");
        admin.PerfilRaw.Should().Be("ADMIN");
        admin.Ativo.Should().BeTrue();
        admin.SenhaHash.Should().StartWith("$argon2id$v=19$m=65536,t=3,p=1$");
    }

    [Fact]
    public async Task Seed_inseriu_filial_matriz()
    {
        await using var db = CarWashDbContextFactoryForTests.Create(_fixture);

        var matriz = await db.Filiais
            .SingleOrDefaultAsync(f => f.Id == new Guid("00000000-0000-0000-0000-000000000010"))
            .ConfigureAwait(false);

        matriz.Should().NotBeNull();
        matriz!.Nome.Should().Be("Matriz");
        matriz.CelulasAtivas.Should().Be(4);
        matriz.Timezone.Should().Be("America/Sao_Paulo");
    }

    [Fact]
    public async Task Seed_inseriu_ao_menos_3_servicos()
    {
        await using var db = CarWashDbContextFactoryForTests.Create(_fixture);

        var total = await db.Servicos.CountAsync().ConfigureAwait(false);
        total.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task Seed_inseriu_preferencia_do_admin()
    {
        await using var db = CarWashDbContextFactoryForTests.Create(_fixture);

        var pref = await db.UsuarioPreferencias
            .SingleOrDefaultAsync(p => p.UsuarioId == new Guid("00000000-0000-0000-0000-000000000001"))
            .ConfigureAwait(false);

        pref.Should().NotBeNull();
        pref!.TemaRaw.Should().Be("claro");
    }
}
