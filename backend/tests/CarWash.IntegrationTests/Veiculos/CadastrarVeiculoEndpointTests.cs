using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CarWash.Application.DTOs;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using CarWash.Domain.ValueObjects;
using CarWash.Infrastructure.Persistence;
using CarWash.IntegrationTests.Collections;
using CarWash.IntegrationTests.Fixtures;
using CarWash.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using CriarVeiculoRequest = CarWash.Application.Veiculos.Criar.CriarVeiculoRequest;
using VeiculoResponse = CarWash.Application.Veiculos.Common.VeiculoResponse;

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
        await EnsureDatabaseCreatedAsync();

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
    }

    [Fact]
    public async Task POST_veiculos_returns_400_for_empty_placa()
    {
        await EnsureDatabaseCreatedAsync();

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
        body.Errors!["placa"].Should().Contain(error => error == "O campo placa é obrigatório.");
    }

    [Fact]
    public async Task POST_veiculos_returns_400_for_invalid_placa_characters()
    {
        await EnsureDatabaseCreatedAsync();

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
            error == "A placa informada não está em um formato válido.");
    }

    [Fact]
    public async Task POST_veiculos_returns_400_for_placa_with_hyphen()
    {
        await EnsureDatabaseCreatedAsync();

        Guid clienteId = await SeedClienteAsync();
        var client = await CreateAuthenticatedClientAsync(isAdmin: true);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/clientes/{clienteId}/veiculos",
            new CriarVeiculoRequest
            {
                Placa = "ABC-1234",
                Modelo = "Corolla",
                Fabricante = "Toyota",
                Cor = "Prata"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<BaseResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Errors.Should().ContainKey("placa");
        body.Errors!["placa"].Should().Contain(error =>
            error == "A placa informada não está em um formato válido.");
    }

    [Fact]
    public async Task POST_veiculos_returns_400_for_placa_with_invalid_length()
    {
        await EnsureDatabaseCreatedAsync();

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
            error == "A placa informada não está em um formato válido.");
    }

    [Fact]
    public async Task POST_veiculos_returns_409_for_duplicate_placa()
    {
        await EnsureDatabaseCreatedAsync();

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
        body!.Message.Should().Be("Já existe um veículo cadastrado com a placa informada.");
    }

    [Fact]
    public async Task POST_veiculos_returns_400_for_empty_modelo()
    {
        await EnsureDatabaseCreatedAsync();

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
    }

    [Fact]
    public async Task POST_veiculos_returns_400_for_empty_fabricante()
    {
        await EnsureDatabaseCreatedAsync();

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
    }

    [Fact]
    public async Task POST_veiculos_returns_400_for_empty_cor()
    {
        await EnsureDatabaseCreatedAsync();

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
    }

    [Fact]
    public async Task POST_veiculos_normalizes_placa_to_uppercase()
    {
        await EnsureDatabaseCreatedAsync();

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
    public async Task POST_veiculos_rejects_placa_with_hyphen_and_spaces()
    {
        await EnsureDatabaseCreatedAsync();

        Guid clienteId = await SeedClienteAsync();
        var client = await CreateAuthenticatedClientAsync(isAdmin: true);
        string placa = GeneratePlaca();
        string placaComSeparadores = $"{placa.Substring(0, 3)}-{placa.Substring(3)}";

        var response = await client.PostAsJsonAsync(
            $"/api/v1/clientes/{clienteId}/veiculos",
            new CriarVeiculoRequest
            {
                Placa = placaComSeparadores,
                Modelo = "Corolla",
                Fabricante = "Toyota",
                Cor = "Prata"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<BaseResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Errors.Should().ContainKey("placa");
        body.Errors!["placa"].Should().Contain(error =>
            error == "A placa informada não está em um formato válido.");
    }

    [Fact]
    public async Task POST_veiculos_returns_401_when_missing_authentication()
    {
        await EnsureDatabaseCreatedAsync();

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
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private async Task EnsureDatabaseCreatedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CarWashDbContext>();
        await db.Database.MigrateAsync();
    }

    private async Task<Guid> SeedClienteAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CarWashDbContext>();

        var cliente = Cliente.Criar(
            id: Guid.NewGuid(),
            nome: $"Cliente {Guid.NewGuid():N}",
            dataNascimento: new DateOnly(1990, 1, 1),
            celular: new Telefone("11987654321"),
            endereco: new Endereco(
                cep: "01001000",
                logradouro: "Praça da Sé",
                numero: "1",
                complemento: null,
                bairro: "Sé",
                cidade: "São Paulo",
                uf: "SP"),
            cpf: new Cpf("39053344705"));

        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();

        return cliente.Id;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(bool isAdmin)
    {
        await EnsureDatabaseCreatedAsync();

        string email = isAdmin
            ? $"admin-{Guid.NewGuid():N}@carwash.com"
            : $"user-{Guid.NewGuid():N}@example.com";

        string password = "Senha@123";
        await SeedUsuarioAsync(email, password, isAdmin);

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

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginBody.AccessToken);
        return client;
    }

    private async Task SeedUsuarioAsync(string email, string password, bool isAdmin)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CarWashDbContext>();

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existing = await db.Usuarios
            .FirstOrDefaultAsync(u => u.EmailValor == normalizedEmail);

        if (existing is not null)
        {
            existing.TrocarSenha(BCrypt.Net.BCrypt.HashPassword(password));
            if (!existing.Ativo) existing.Ativar();
            existing.RegistrarLoginBemSucedido();
            await db.SaveChangesAsync();
            return;
        }

        var usuario = Usuario.Criar(
            id: Guid.NewGuid(),
            nome: isAdmin ? "Admin Teste" : "Funcionário Teste",
            email: new Email(normalizedEmail),
            senhaHash: BCrypt.Net.BCrypt.HashPassword(password),
            perfil: isAdmin ? PerfilUsuario.Admin : PerfilUsuario.Funcionario);

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
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
