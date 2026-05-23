using System.Collections.Generic;
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

    public AuthService(
        ICarWashDbContext context,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Senha))
        {
            throw new AuthException(
                400,
                "AUTH_VALIDATION_ERROR",
                "Dados de acesso inválidos. Verifique os campos e tente novamente.");
        }

        string emailNormalizado = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailNormalizado);

        if (user == null || !user.Active)
        {
#pragma warning disable CA1848
            _logger.LogWarning(
                "LOGIN_FALHA: Tentativa de acesso para utilizador inexistente ou inativo. Email: {Email}",
                MascararEmail(emailNormalizado));
#pragma warning restore CA1848

            throw new AuthException(
                401,
                "AUTH_INVALID_CREDENTIALS",
                "Usuário ou senha inválidos.");
        }

        if (user.BlockedUntil.HasValue &&
            user.BlockedUntil.Value > DateTime.UtcNow)
        {
#pragma warning disable CA1848
            _logger.LogWarning(
                "LOGIN_BLOQUEIO: Utilizador bloqueado tentou aceder. Email: {Email}",
                MascararEmail(emailNormalizado));
#pragma warning restore CA1848

            throw new AuthException(
                403,
                "AUTH_TEMPORARILY_BLOCKED",
                "Acesso temporariamente bloqueado por tentativas inválidas. Tente novamente em alguns minutos.");
        }

        bool senhaValida = BCrypt.Net.BCrypt.Verify(
            request.Senha,
            user.PasswordHash);

        if (!senhaValida)
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= 3)
            {
                user.BlockedUntil = DateTime.UtcNow.AddMinutes(15);

                await _context.SaveChangesAsync();

#pragma warning disable CA1848
                _logger.LogWarning(
                    "LOGIN_BLOQUEIO: Utilizador bloqueado por exceder 3 falhas. Email: {Email}",
                    MascararEmail(emailNormalizado));
#pragma warning restore CA1848

                throw new AuthException(
                    403,
                    "AUTH_TEMPORARILY_BLOCKED",
                    "Acesso temporariamente bloqueado por tentativas inválidas. Tente novamente em alguns minutos.");
            }

            await _context.SaveChangesAsync();

#pragma warning disable CA1848
            _logger.LogWarning(
                "LOGIN_FALHA: Credencial inválida. Email: {Email}",
                MascararEmail(emailNormalizado));
#pragma warning restore CA1848

            throw new AuthException(
                401,
                "AUTH_INVALID_CREDENTIALS",
                "Usuário ou senha inválidos.");
        }

        user.FailedLoginAttempts = 0;
        user.BlockedUntil = null;

        var accessToken = GerarAccessToken(user);
        var refreshToken = GerarRefreshToken();

        var session = new Session
        {
            UserId = user.Id,
            RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        _context.Sessions.Add(session);

        await _context.SaveChangesAsync();

#pragma warning disable CA1848
        _logger.LogInformation(
            "LOGIN_SUCESSO: Utilizador autenticado. Email: {Email}",
            MascararEmail(emailNormalizado));
#pragma warning restore CA1848

        return new LoginResponse
        {
            Message = "Login realizado com sucesso.",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Usuario = new UsuarioResponse
            {
                Id = user.Id,
                Email = user.Email
            }
        };
    }

    public async Task<LoginResponse> RefreshAsync(TokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new AuthException(
                400,
                "AUTH_VALIDATION_ERROR",
                "Refresh token é obrigatório.");
        }

        var sessions = await _context.Sessions
            .Include(s => s.User)
            .Where(s => !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        _logger.LogInformation(
            "REFRESH: Encontradas {SessionCount} sessões válidas",
            sessions.Count);

        var session = sessions.FirstOrDefault(s =>
            BCrypt.Net.BCrypt.Verify(request.RefreshToken, s.RefreshTokenHash));

        if (session == null)
        {
            _logger.LogWarning(
                "REFRESH_FALHA: Token inválido ou não encontrado. Token length: {TokenLength}",
                request.RefreshToken?.Length ?? 0);

            throw new AuthException(
                401,
                "AUTH_INVALID_REFRESH_TOKEN",
                "Refresh token inválido ou expirado.");
        }

        var user = session.User;

        if (user == null || !user.Active)
        {
            throw new AuthException(
                401,
                "AUTH_INVALID_USER",
                "Usuário não existe ou está inativo.");
        }

        string newAccessToken = GerarAccessToken(user);
        string newRefreshToken = GerarRefreshToken();

        session.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(newRefreshToken);
        session.ExpiresAt = DateTime.UtcNow.AddDays(7);

        await _context.SaveChangesAsync();

#pragma warning disable CA1848
        _logger.LogInformation(
            "REFRESH_SUCESSO: Tokens renovados para utilizador. Email: {Email}",
            MascararEmail(user.Email));
#pragma warning restore CA1848

        return new LoginResponse
        {
            Message = "Token renovado com sucesso.",
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            Usuario = new UsuarioResponse
            {
                Id = user.Id,
                Email = user.Email
            }
        };
    }

    public async Task LogoutAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new AuthException(
                400,
                "AUTH_VALIDATION_ERROR",
                "Refresh token é obrigatório.");
        }

        var sessions = await _context.Sessions
            .Where(s => !s.IsRevoked)
            .ToListAsync();

        var session = sessions.FirstOrDefault(s =>
            BCrypt.Net.BCrypt.Verify(refreshToken, s.RefreshTokenHash));

        if (session != null)
        {
            session.IsRevoked = true;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "LOGOUT: Sessão revogada para usuário ID: {UserId}",
                session.UserId);
        }
    }

    private static string GerarRefreshToken()
    {
        byte[] randomNumber = new byte[32];

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
        var jwtSettings = _configuration.GetSection("JwtSettings");

        string secretKey = jwtSettings["Secret"]
            ?? throw new InvalidOperationException("JWT Secret não configurado.");

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey));

        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (string.Equals(user.Email, "admins@carwash.com", StringComparison.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ClaimTypes.Role, "admin"));
        }

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
