using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarWash.IntegrationTests.Collections;
using CarWash.IntegrationTests.Fixtures;
using CarWash.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CarWash.IntegrationTests.Endpoints;

/// <summary>
/// Garante que o gerador Swagger consegue construir o documento OpenAPI sem
/// colisões de schemaId (regressão: dois records aninhados com mesmo nome
/// curto, ex.: LoginResponse.UsuarioLogado e RefreshResponse.UsuarioLogado).
/// </summary>
[Collection(nameof(PostgresCollection))]
public class SwaggerEndpointTests : IAsyncDisposable
{
    private readonly SwaggerWebApplicationFactory _factory;

    public SwaggerEndpointTests(PostgresFixture fixture)
    {
        _factory = new SwaggerWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task GET_swagger_v1_swagger_json_retorna_200_com_documento_valido()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync(new Uri("/swagger/v1/swagger.json", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = await resp.Content.ReadFromJsonAsync<JsonElement>();
        doc.GetProperty("openapi").GetString().Should().StartWith("3.");
        doc.GetProperty("info").GetProperty("title").GetString().Should().Be("CarWash API");

        // Endpoints críticos devem estar presentes.
        var paths = doc.GetProperty("paths");
        paths.TryGetProperty("/api/v1/auth/login", out _).Should().BeTrue();
        paths.TryGetProperty("/api/v1/auth/refresh", out _).Should().BeTrue();
        paths.TryGetProperty("/api/v1/auth/logout", out _).Should().BeTrue();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
