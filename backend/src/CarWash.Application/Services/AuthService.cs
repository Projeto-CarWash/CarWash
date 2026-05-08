using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using CarWash.Application.DTOs;
using CarWash.Application.Exceptions;
using CarWash.Application.Interfaces;
using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CarWash.Application.Services;

/// <summary>
/// Serviço responsável por gerenciar a autenticação de utilizadores.
/// </summary>
public class AuthService : IAuthService
{
    private readonly ICarWashDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    /// <param name="context">O contexto do banco de dados.</param>
    /// <param name="configuration">As configurações do sistema.</param>
    /// <param name="logger">O serviço de log.</param>
    public AuthService(ICarWashDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
    {
        this._context = context;
        this._configuration = configuration;
        this._logger = logger;
    }

    /// <summary>
    /// Efetua o login do utilizador validando suas credenciais.
    /// </summary>
    /// <param name="request">As credenciais de acesso.</param>
    /// <returns>A resposta de login contendo os tokens.</returns>
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Senha))
        {
            throw new AuthException(400, "AUTH_VALIDATION_ERROR", "Dados de acesso inválidos. Verifique os campos e tente novamente.");
        }

        string emailNormalizado = request.Email.Trim().ToLowerInvariant();

        var user = await this._context.Users.FirstOrDefaultAsync(u => u.Email == emailNormalizado);

        if (user == null || !user.Active)
        {
#pragma warning disable CA1848
            this._logger.LogWarning("LOGIN_FALHA: Tentativa de acesso para utilizador inexistente ou inativo. Email: {Email}", MascararEmail(emailNormalizado));
#pragma warning restore CA1848
            throw new AuthException(401, "AUTH_INVALID_CREDENTIALS", "Usuário ou senha inválidos.");
        }

        if (user.BlockedUntil.HasValue && user.BlockedUntil.Value > DateTime.UtcNow)
        {
#pragma warning disable CA1848
            this._logger.LogWarning("LOGIN_BLOQUEIO: Utilizador bloqueado tentou aceder. Email: {Email}", MascararEmail(emailNormalizado));
#pragma warning restore CA1848
            throw new AuthException(403, "AUTH_TEMPORARILY_BLOCKED", "Acesso temporariamente bloqueado por tentativas inválidas. Tente novamente em alguns minutos.");
        }

        bool senhaValida = BCrypt.Net.BCrypt.Verify(request.Senha, user.PasswordHash);

        if (!senhaValida)
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= 3)
            {
                user.BlockedUntil = DateTime.UtcNow.AddMinutes(15);
                await this._context.SaveChangesAsync();

#pragma warning disable CA1848
                this._logger.LogWarning("LOGIN_BLOQUEIO: Utilizador bloqueado por exceder 3 falhas. Email: {Email}", MascararEmail(emailNormalizado));
#pragma warning restore CA1848
                throw new AuthException(403, "AUTH_TEMPORARILY_BLOCKED", "Acesso temporariamente bloqueado por tentativas inválidas. Tente novamente em alguns minutos.");
            }

            await this._context.SaveChangesAsync();
#pragma warning disable CA1848
            this._logger.LogWarning("LOGIN_FALHA: Credencial inválida. Email: {Email}", MascararEmail(emailNormalizado));
#pragma warning restore CA1848
            throw new AuthException(401, "AUTH_INVALID_CREDENTIALS", "Usuário ou senha inválidos.");
        }

        user.FailedLoginAttempts = 0;
        user.BlockedUntil = null;

        var accessToken = this.GerarAccessToken(user);
        var refreshToken = GerarRefreshToken();

        var session = new Session
        {
            UserId = user.Id,
            RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        this._context.Sessions.Add(session);
        await this._context.SaveChangesAsync();

#pragma warning disable CA1848
        this._logger.LogInformation("LOGIN_SUCESSO: Utilizador autenticado. Email: {Email}", MascararEmail(emailNormalizado));
#pragma warning restore CA1848

        return new LoginResponse
        {
            Message = "Login realizado com sucesso.",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Usuario = new UsuarioResponse { Id = user.Id, Email = user.Email }
        };
    }

    /// <summary>
    /// Atualiza os tokens da sessão.
    /// </summary>
    /// <param name="request">O token antigo.</param>
    /// <returns>Os novos tokens.</returns>
    public Task<LoginResponse> RefreshAsync(TokenRequest request)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Finaliza a sessão.
    /// </summary>
    /// <param name="refreshToken">O token a ser revogado.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    public Task LogoutAsync(string refreshToken)
    {
        throw new NotImplementedException();
    }

    private static string GerarRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private static string MascararEmail(string email)
    {
        string[] partes = email.Split('@');
        if (partes.Length != 2 || partes[0].Length <= 2)
        {
            return "***@***";
        }

        return $"{partes[0].Substring(0, 2)}***@{partes[1]}";
    }

    private string GerarAccessToken(User user)
    {
        var jwtSettings = this._configuration.GetSection("JwtSettings");
        string secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret não configurado.");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
