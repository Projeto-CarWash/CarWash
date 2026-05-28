using System.Net;
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

namespace CarWash.IntegrationTests.Auth;

[Collection(nameof(PostgresCollection))]
public class AuthFlowTests : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly CarWashWebApplicationFactory _factory;

    public AuthFlowTests(PostgresFixture fixture)
    {
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task POST_auth_flow_rotates_refresh_token_and_revokes_session_on_logout()
    {
        string email = $"user-{Guid.NewGuid():N}@example.com";
        string password = "Senha@123";
        var user = await SeedUserAsync(email, password);

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
        loginBody!.Message.Should().Be("Login realizado com sucesso.");
        loginBody.TraceId.Should().NotBeNullOrWhiteSpace();
        loginBody.AccessToken.Should().NotBeNullOrWhiteSpace();
        loginBody.RefreshToken.Should().NotBeNullOrWhiteSpace();
        loginBody.Usuario.Id.Should().Be(user.Id);
        loginBody.Usuario.Email.Should().Be(email);

        string firstRefreshToken = loginBody.RefreshToken;

        var refreshResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new TokenRequest
            {
                RefreshToken = firstRefreshToken
            });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshBody = await refreshResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        refreshBody.Should().NotBeNull();
        refreshBody!.Message.Should().Be("Token renovado com sucesso.");
        refreshBody.TraceId.Should().NotBeNullOrWhiteSpace();
        refreshBody.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshBody.RefreshToken.Should().NotBe(firstRefreshToken);

        var refreshedSession = await GetSingleSessionAsync(user.Id);
        BCrypt.Net.BCrypt.Verify(refreshBody.RefreshToken, refreshedSession.RefreshTokenHash).Should().BeTrue();
        BCrypt.Net.BCrypt.Verify(firstRefreshToken, refreshedSession.RefreshTokenHash).Should().BeFalse();

        var staleRefreshResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new TokenRequest
            {
                RefreshToken = firstRefreshToken
            });

        staleRefreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var staleRefreshBody = await staleRefreshResponse.Content.ReadFromJsonAsync<BaseResponse>(JsonOptions);
        staleRefreshBody.Should().NotBeNull();
        staleRefreshBody!.Code.Should().Be("AUTH_INVALID_REFRESH_TOKEN");
        staleRefreshBody.TraceId.Should().NotBeNullOrWhiteSpace();

        var logoutResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/logout",
            new TokenRequest
            {
                RefreshToken = refreshBody.RefreshToken
            });

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var logoutBody = await logoutResponse.Content.ReadFromJsonAsync<BaseResponse>(JsonOptions);
        logoutBody.Should().NotBeNull();
        logoutBody!.Message.Should().Be("Logout realizado com sucesso.");
        logoutBody.TraceId.Should().NotBeNullOrWhiteSpace();

        var revokedSession = await GetSingleSessionAsync(user.Id);
        revokedSession.IsRevoked.Should().BeTrue();

        var logoutAgainResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/logout",
            new TokenRequest
            {
                RefreshToken = refreshBody.RefreshToken
            });

        logoutAgainResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task POST_auth_login_blocks_after_three_failed_attempts()
    {
        string email = $"blocked-{Guid.NewGuid():N}@example.com";
        string password = "Senha@123";
        await SeedUserAsync(email, password);

        var client = _factory.CreateClient();

        for (int attempt = 1; attempt <= 2; attempt++)
        {
            var failedResponse = await client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new LoginRequest
                {
                    Email = email,
                    Senha = "SenhaErrada"
                });

            failedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var failedBody = await failedResponse.Content.ReadFromJsonAsync<BaseResponse>(JsonOptions);
            failedBody.Should().NotBeNull();
            failedBody!.Code.Should().Be("AUTH_INVALID_CREDENTIALS");
            failedBody.TraceId.Should().NotBeNullOrWhiteSpace();
        }

        var blockedResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest
            {
                Email = email,
                Senha = "SenhaErrada"
            });

        blockedResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var blockedBody = await blockedResponse.Content.ReadFromJsonAsync<BaseResponse>(JsonOptions);
        blockedBody.Should().NotBeNull();
        blockedBody!.Code.Should().Be("AUTH_TEMPORARILY_BLOCKED");
        blockedBody.TraceId.Should().NotBeNullOrWhiteSpace();

        var blockedCorrectPasswordResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest
            {
                Email = email,
                Senha = password
            });

        blockedCorrectPasswordResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var reloadedUser = await GetUserByEmailAsync(email);
        reloadedUser.FailedLoginAttempts.Should().Be(3);
        reloadedUser.BlockedUntil.Should().NotBeNull();
        reloadedUser.BlockedUntil.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task POST_auth_login_returns_field_details_for_invalid_request()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest
            {
                Email = " ",
                Senha = string.Empty
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<BaseResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Code.Should().Be("AUTH_VALIDATION_ERROR");
        body.Message.Should().Be("Dados de acesso inválidos. Verifique os campos e tente novamente.");
        body.TraceId.Should().NotBeNullOrWhiteSpace();
        body.Errors.Should().ContainKeys("email", "senha");
        body.Errors!["email"].Should().Contain(error => error == "Email é obrigatório.");
        body.Errors["senha"].Should().Contain(error => error == "Senha é obrigatória.");
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private async Task<User> SeedUserAsync(string email, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CarWashDbContext>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Active = true,
            FailedLoginAttempts = 0,
            BlockedUntil = null
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user;
    }

    private async Task<User> GetUserByEmailAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CarWashDbContext>();

        string normalizedEmail = email.Trim().ToLowerInvariant();

        return await db.Users.SingleAsync(user => user.Email == normalizedEmail);
    }

    private async Task<Session> GetSingleSessionAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CarWashDbContext>();

        return await db.Sessions.SingleAsync(session => session.UserId == userId);
    }
}
