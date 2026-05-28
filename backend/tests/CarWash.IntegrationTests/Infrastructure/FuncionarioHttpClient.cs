using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CarWash.IntegrationTests.Infrastructure;

/// <summary>
/// Helper que cria um usuário de perfil <c>Funcionario</c> (via Admin) e devolve
/// um <see cref="HttpClient"/> autenticado com o token DESSE funcionário. Após a
/// reconciliação com a development, as rotas de filiais usam
/// <c>RequireAuthorization()</c> puro (sem policy Admin / sem 403 por perfil —
/// adiado para RF-FUT003). Permanece útil para validar que um funcionário
/// autenticado tem acesso às rotas apenas-autenticadas (ex.: GET por id).
/// </summary>
public static class FuncionarioHttpClient
{
    private const string SenhaFuncionario = "Funcionario1234";

    private static readonly Uri RotaCriarUsuario = new("/api/v1/usuarios", UriKind.Relative);
    private static readonly Uri RotaLogin = new("/api/v1/auth/login", UriKind.Relative);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task<HttpClient> CreateAsync(CarWashWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        // 1) Admin cria um funcionário com credenciais conhecidas.
        var admin = await AuthenticatedHttpClient.CreateAsync(factory).ConfigureAwait(false);
        var email = $"func-{Guid.NewGuid():N}@carwash.local";

        var criar = await admin.PostAsJsonAsync(RotaCriarUsuario, new
        {
            nome = "Funcionario Teste",
            email,
            senha = SenhaFuncionario,
            perfil = "Funcionario",
        }, Json).ConfigureAwait(false);
        criar.EnsureSuccessStatusCode();

        // 2) Login com as credenciais do funcionário → token com claim perfil=Funcionario.
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync(RotaLogin, new
        {
            email,
            senha = SenhaFuncionario,
        }, Json).ConfigureAwait(false);
        login.EnsureSuccessStatusCode();

        var body = await login.Content.ReadFromJsonAsync<JsonElement>(Json).ConfigureAwait(false);
        var accessToken = body.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Login do funcionário não devolveu accessToken.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}
