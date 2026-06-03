using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BCrypt.Net;
using CarWash.Application.DTOs;
using CarWash.Domain.Entities;
using CarWash.Infrastructure.Persistence;
using CarWash.IntegrationTests.Collections;
using CarWash.IntegrationTests.Fixtures;
using CarWash.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CarWash.IntegrationTests.Veiculos;

[Collection(nameof(PostgresCollection))]
public class CadastrarVeiculoEndpointTests : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly CarWashWebApplicationFactory _factory;

    public CadastrarVeiculoEndpointTests(PostgresFixture fixture)
    {
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task POST_veiculos_returns_201_with_id_for_valid_request()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        Guid clienteId = await SeedClienteAsync();
        var client = await CreateAuthenticatedClientAsync(isAdmin: true);
        string placa = GeneratePlaca();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/clientes/{clienteId}/veiculos",
            new CriarVeiculoRequest
            {
                Placa = placa,
                Modelo = "Corolla",
                Fabricante = "Toyota",
                Cor = "Prata"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<VeiculoResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Id.Should().NotBeEmpty();
        body.Message.Should().Be("Veículo cadastrado com sucesso.");
    }

    [Fact]
    public async Task POST_veiculos_returns_400_for_invalid_cliente_id()
    {
        var client = await CreateAuthenticatedClientAsync(isAdmin: true);

        var response = await client.PostAsJsonAsync(
            "/api/v1/clientes/invalid/veiculos",
            new CriarVeiculoRequest
            {
                Placa = "ABC1D23",
                Modelo = "Corolla",
                Fabricante = "Toyota",
                Cor = "Prata"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<BaseResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Message.Should().Be("Dados do veículo inválidos. Verifique os campos e tente novamente.");
        body.Errors.Should().ContainKey("clienteId");
    }

    [Fact]
    public async Task POST_veiculos_returns_404_when_cliente_not_found()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        var client = await CreateAuthenticatedClientAsync(isAdmin: true);
        Guid clienteId = Guid.NewGuid();
        string placa = GeneratePlaca();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/clientes/{clienteId}/veiculos",
            new CriarVeiculoRequest
            {
                Placa = placa,
                Modelo = "Corolla",
                Fabricante = "Toyota",
                Cor = "Prata"
            });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<BaseResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Message.Should().Be("Cliente não encontrado para vincular o veículo.");
    }

    [Fact]
    public async Task POST_veiculos_returns_400_for_empty_placa()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        Guid clienteId = await SeedClienteAsync();
        var client = await CreateAuthenticatedClientAsync(isAdmin: true);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/clientes/{clienteId}/veiculos",
            new CriarVeiculoRequest
            {
                Placa = " ",
                Modelo = "Corolla",
                Fabricante = "Toyota",
                Cor = "Prata"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<BaseResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Errors.Should().ContainKey("placa");
        body.Errors!["placa"].Should().Contain(error => error == "Placa é obrigatória.");
    }

    [Fact]
    public async Task POST_veiculos_returns_400_for_invalid_placa_characters()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        Guid clienteId = await SeedClienteAsync();
        var client = await CreateAuthenticatedClientAsync(isAdmin: true);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/clientes/{clienteId}/veiculos",
            new CriarVeiculoRequest
            {
                Placa = "ABÇ1234",
                Modelo = "Corolla",
                Fabricante = "Toyota",
                Cor = "Prata"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<BaseResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Errors.Should().ContainKey("placa");
        body.Errors!["placa"].Should().Contain(error =>
            error == "Placa inválida. Formatos aceitos: AAA0000 ou AAA0A00.");
    }

    [Fact]
    public async Task POST_veiculos_returns_400_for_placa_with_invalid_length()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        Guid clienteId = await SeedClienteAsync();
        var client = await CreateAuthenticatedClientAsync(isAdmin: true);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/clientes/{clienteId}/veiculos",
            new CriarVeiculoRequest
            {
                Placa = "ABC12345",
                Modelo = "Corolla",
                Fabricante = "Toyota",
                Cor = "Prata"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<BaseResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Errors.Should().ContainKey("placa");
        body.Errors!["placa"].Should().Contain(error =>
            error == "Placa deve conter 7 caracteres válidos.");
    }

    [Fact]
    public async Task POST_veiculos_returns_409_for_duplicate_placa()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        Guid clienteId = await SeedClienteAsync();
        Guid outroClienteId = await SeedClienteAsync();
        var client = await CreateAuthenticatedClientAsync(isAdmin: true);
        string placa = GeneratePlaca();

        var firstResponse = await client.PostAsJsonAsync(
            $"/api/v1/clientes/{clienteId}/veiculos",
            new CriarVeiculoRequest
            {
                Placa = placa,
                Modelo = "Corolla",
                Fabricante = "Toyota",
                Cor = "Prata"
            });

        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/clientes/{outroClienteId}/veiculos",
            new CriarVeiculoRequest
            {
                Placa = placa,
                Modelo = "Yaris",
                Fabricante = "Toyota",
                Cor = "Preto"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await response.Content.ReadFromJsonAsync<BaseResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Message.Should().Be("Já existe veículo cadastrado com esta placa.");
    }

    [Fact]
    public async Task POST_veiculos_returns_400_for_empty_modelo()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        Guid clienteId = await SeedClienteAsync();
        var client = await CreateAuthenticatedClientAsync(isAdmin: true);
        string placa = GeneratePlaca();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/clientes/{clienteId}/veiculos",
            new CriarVeiculoRequest
            {
                Placa = placa,
                Modelo = " ",
                Fabricante = "Toyota",
                Cor = "Prata"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<BaseResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Errors.Should().ContainKey("modelo");
        body.Errors!["modelo"].Should().Contain(error => error == "Modelo é obrigatório.");
    }

    [Fact]
    public async Task POST_veiculos_returns_400_for_modelo_with_one_char()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        Guid clienteId = await SeedClienteAsync();
        var client = await CreateAuthenticatedClientAsync(isAdmin: true);
        string placa = GeneratePlaca();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/clientes/{clienteId}/veiculos",
            new CriarVeiculoRequest
            {
                Placa = placa,
                Modelo = "A",
                Fabricante = "Toyota",
                Cor = "Prata"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<BaseResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Errors.Should().ContainKey("modelo");
        body.Errors!["modelo"].Should().Contain(error => error == "Modelo deve ter entre 2 e 80 caracteres.");
    }

    [Fact]
    public async Task POST_veiculos_returns_400_for_empty_fabricante()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        Guid clienteId = await SeedClienteAsync();
        var client = await CreateAuthenticatedClientAsync(isAdmin: true);
        string placa = GeneratePlaca();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/clientes/{clienteId}/veiculos",
            new CriarVeiculoRequest
            {
                Placa = placa,
                Modelo = "Corolla",
                Fabricante = " ",
                Cor = "Prata"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<BaseResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Errors.Should().ContainKey("fabricante");
        body.Errors!["fabricante"].Should().Contain(error => error == "Fabricante é obrigatório.");
    }

    [Fact]
    public async Task POST_veiculos_returns_400_for_empty_cor()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        Guid clienteId = await SeedClienteAsync();
        var client = await CreateAuthenticatedClientAsync(isAdmin: true);
        string placa = GeneratePlaca();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/clientes/{clienteId}/veiculos",
            new CriarVeiculoRequest
            {
                Placa = placa,
                Modelo = "Corolla",
                Fabricante = "Toyota",
                Cor = " "
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<BaseResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Errors.Should().ContainKey("cor");
        body.Errors!["cor"].Should().Contain(error => error == "Cor é obrigatória.");
    }

    [Fact]
    public async Task POST_veiculos_returns_400_for_cor_with_one_char()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        Guid clienteId = await SeedClienteAsync();
        var client = await CreateAuthenticatedClientAsync(isAdmin: true);
        string placa = GeneratePlaca();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/clientes/{clienteId}/veiculos",
            new CriarVeiculoRequest
            {
                Placa = placa,
                Modelo = "Corolla",
                Fabricante = "Toyota",
                Cor = "P"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<BaseResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Errors.Should().ContainKey("cor");
        body.Errors!["cor"].Should().Contain(error => error == "Cor deve ter entre 2 e 40 caracteres.");
    }

    [Fact]
    public async Task POST_veiculos_normalizes_placa_to_uppercase()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        Guid clienteId = await SeedClienteAsync();
        var client = await CreateAuthenticatedClientAsync(isAdmin: true);
        string placa = GeneratePlaca();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/clientes/{clienteId}/veiculos",
            new CriarVeiculoRequest
            {
                Placa = placa.ToLowerInvariant(),
                Modelo = "Corolla",
                Fabricante = "Toyota",
                Cor = "Prata"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CarWashDbContext>();

        var veiculo = await db.Veiculos.SingleAsync(v => v.ClienteId == clienteId);
        veiculo.Placa.Should().Be(placa.ToUpperInvariant());
    }

    [Fact]
    public async Task POST_veiculos_normalizes_placa_removing_hyphen_and_spaces()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        Guid clienteId = await SeedClienteAsync();
        var client = await CreateAuthenticatedClientAsync(isAdmin: true);
        string placa = GeneratePlaca();
        string placaComSeparadores = $"{placa.Substring(0, 3)}-{placa.Substring(3, 1)} {placa.Substring(4)}";

        var response = await client.PostAsJsonAsync(
            $"/api/v1/clientes/{clienteId}/veiculos",
            new CriarVeiculoRequest
            {
                Placa = placaComSeparadores.ToLowerInvariant(),
                Modelo = "Corolla",
                Fabricante = "Toyota",
                Cor = "Prata"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CarWashDbContext>();

        var veiculo = await db.Veiculos.SingleAsync(v => v.ClienteId == clienteId);
        veiculo.Placa.Should().Be(placa.ToUpperInvariant());
    }

    [Fact]
    public async Task POST_veiculos_returns_401_when_missing_authentication()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        Guid clienteId = await SeedClienteAsync();
        var client = _factory.CreateClient();
        string placa = GeneratePlaca();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/clientes/{clienteId}/veiculos",
            new CriarVeiculoRequest
            {
                Placa = placa,
                Modelo = "Corolla",
                Fabricante = "Toyota",
                Cor = "Prata"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await response.Content.ReadFromJsonAsync<BaseResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Message.Should().Be("Autenticação obrigatória para executar esta operação.");
    }

    [Fact]
    public async Task POST_veiculos_returns_403_when_missing_permission()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        Guid clienteId = await SeedClienteAsync();
        var client = await CreateAuthenticatedClientAsync(isAdmin: false);
        string placa = GeneratePlaca();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/clientes/{clienteId}/veiculos",
            new CriarVeiculoRequest
            {
                Placa = placa,
                Modelo = "Corolla",
                Fabricante = "Toyota",
                Cor = "Prata"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var body = await response.Content.ReadFromJsonAsync<BaseResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Message.Should().Be("Você não possui permissão para cadastrar ou editar veículos.");
    }

    [Fact]
    public async Task PATCH_veiculos_returns_200_for_valid_update()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        Guid clienteId = await SeedClienteAsync();
        var client = await CreateAuthenticatedClientAsync(isAdmin: true);
        Guid veiculoId = await CreateVeiculoAsync(clienteId, client);
        string novaPlaca = GeneratePlaca();

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/veiculos/{veiculoId}",
            new CriarVeiculoRequest
            {
                Placa = novaPlaca,
                Modelo = "Civic",
                Fabricante = "Honda",
                Cor = "Preto"
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<VeiculoResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Id.Should().Be(veiculoId);
        body.Message.Should().Be("Veículo atualizado com sucesso.");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CarWashDbContext>();

        var veiculo = await db.Veiculos.SingleAsync(v => v.Id == veiculoId);
        veiculo.Placa.Should().Be(novaPlaca.ToUpperInvariant());
        veiculo.Modelo.Should().Be("Civic");
        veiculo.Fabricante.Should().Be("Honda");
        veiculo.Cor.Should().Be("Preto");
    }

    [Fact]
    public async Task PATCH_veiculos_returns_409_for_duplicate_placa()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        Guid clienteId = await SeedClienteAsync();
        var client = await CreateAuthenticatedClientAsync(isAdmin: true);

        string placaOriginal = GeneratePlaca();
        Guid veiculoId = await CreateVeiculoAsync(clienteId, client, placaOriginal);
        Guid outroVeiculoId = await CreateVeiculoAsync(clienteId, client);

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/veiculos/{outroVeiculoId}",
            new CriarVeiculoRequest
            {
                Placa = placaOriginal,
                Modelo = "Civic",
                Fabricante = "Honda",
                Cor = "Preto"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await response.Content.ReadFromJsonAsync<BaseResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Message.Should().Be("Já existe veículo cadastrado com esta placa.");
    }

    [Fact]
    public async Task PATCH_veiculos_returns_400_for_invalid_placa_length()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        Guid clienteId = await SeedClienteAsync();
        var client = await CreateAuthenticatedClientAsync(isAdmin: true);
        Guid veiculoId = await CreateVeiculoAsync(clienteId, client);

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/veiculos/{veiculoId}",
            new CriarVeiculoRequest
            {
                Placa = "ABC12345",
                Modelo = "Civic",
                Fabricante = "Honda",
                Cor = "Preto"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<BaseResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Errors.Should().ContainKey("placa");
        body.Errors!["placa"].Should().Contain(error => error == "Placa deve conter 7 caracteres válidos.");
    }

    [Fact]
    public async Task PATCH_veiculos_returns_401_when_missing_authentication()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        Guid clienteId = await SeedClienteAsync();
        var client = _factory.CreateClient();
        Guid veiculoId = await CreateVeiculoAsync(clienteId, await CreateAuthenticatedClientAsync(isAdmin: true));

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/veiculos/{veiculoId}",
            new CriarVeiculoRequest
            {
                Placa = GeneratePlaca(),
                Modelo = "Civic",
                Fabricante = "Honda",
                Cor = "Preto"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await response.Content.ReadFromJsonAsync<BaseResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Message.Should().Be("Autenticação obrigatória para executar esta operação.");
    }

    [Fact]
    public async Task PATCH_veiculos_returns_403_when_missing_permission()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        Guid clienteId = await SeedClienteAsync();
        var adminClient = await CreateAuthenticatedClientAsync(isAdmin: true);
        Guid veiculoId = await CreateVeiculoAsync(clienteId, adminClient);

        var client = await CreateAuthenticatedClientAsync(isAdmin: false);

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/veiculos/{veiculoId}",
            new CriarVeiculoRequest
            {
                Placa = GeneratePlaca(),
                Modelo = "Civic",
                Fabricante = "Honda",
                Cor = "Preto"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var body = await response.Content.ReadFromJsonAsync<BaseResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Message.Should().Be("Você não possui permissão para cadastrar ou editar veículos.");
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private async Task<Guid> SeedClienteAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CarWashDbContext>();

        var cliente = new Cliente
        {
            Id = Guid.NewGuid(),
            Nome = $"Cliente {Guid.NewGuid():N}",
            CreatedAt = DateTime.UtcNow
        };

        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();

        return cliente.Id;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(bool isAdmin)
    {
        await _factory.EnsureDatabaseCreatedAsync();

        string email = isAdmin
            ? "admins@carwash.com"
            : $"user-{Guid.NewGuid():N}@example.com";

        string password = "Senha@123";
        await SeedUserAsync(email, password);

        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest
            {
                Email = email,
                Senha = password
            });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        loginBody.Should().NotBeNull();
        loginBody!.AccessToken.Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.AccessToken);
        return client;
    }

    private async Task<User> SeedUserAsync(string email, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CarWashDbContext>();

        string normalizedEmail = email.Trim().ToLowerInvariant();
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
        if (existing != null)
        {
            existing.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            existing.Active = true;
            existing.FailedLoginAttempts = 0;
            existing.BlockedUntil = null;
            await db.SaveChangesAsync();
            return existing;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Active = true,
            FailedLoginAttempts = 0,
            BlockedUntil = null
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user;
    }

    private async Task<Guid> CreateVeiculoAsync(Guid clienteId, HttpClient client, string? placa = null)
    {
        string placaValue = placa ?? GeneratePlaca();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/clientes/{clienteId}/veiculos",
            new CriarVeiculoRequest
            {
                Placa = placaValue,
                Modelo = "Corolla",
                Fabricante = "Toyota",
                Cor = "Prata"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<VeiculoResponse>(JsonOptions);
        body.Should().NotBeNull();
        return body!.Id;
    }

    private static string GeneratePlaca()
    {
        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";

        char L() => letters[Random.Shared.Next(letters.Length)];
        char D() => digits[Random.Shared.Next(digits.Length)];

        bool mercosul = Random.Shared.Next(0, 2) == 0;
        if (mercosul)
        {
            return new string(new[] { L(), L(), L(), D(), L(), D(), D() });
        }

        return new string(new[] { L(), L(), L(), D(), D(), D(), D() });
    }
}
