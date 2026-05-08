using System.Net;
using System.Text.Json;
using CarWash.Application.DTOs;
using CarWash.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CarWash.Api.Middlewares;

/// <summary>
/// Middleware global para tratamento de exceções.
/// </summary>
public class ExceptionMiddleware
{
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionMiddleware"/> class.
    /// </summary>
    /// <param name="next">O próximo delegate da requisição.</param>
    /// <param name="logger">O serviço de log.</param>
    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        this._next = next;
        this._logger = logger;
    }

    /// <summary>
    /// Invoca o middleware para processar a requisição HTTP.
    /// </summary>
    /// <param name="context">O contexto HTTP atual.</param>
    /// <returns>Uma tarefa representando a operação assíncrona.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await this._next(context);
        }
        catch (AuthException ex)
        {
            await HandleAuthExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
#pragma warning disable CA1848
            this._logger.LogError(ex, "Erro interno não tratado pelo servidor.");
#pragma warning restore CA1848
            await HandleGenericExceptionAsync(context, ex);
        }
    }

    private static Task HandleAuthExceptionAsync(HttpContext context, AuthException exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception.StatusCode;

        var response = new BaseResponse
        {
            Message = exception.Message,
            Code = exception.ErrorCode,
            TraceId = context.TraceIdentifier
        };

        string json = JsonSerializer.Serialize(response, _jsonOptions);
        return context.Response.WriteAsync(json);
    }

    private static Task HandleGenericExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new BaseResponse
        {
            Message = "Ocorreu um erro interno no servidor. Tente novamente mais tarde.",
            Code = "INTERNAL_SERVER_ERROR",
            TraceId = context.TraceIdentifier
        };

        string json = JsonSerializer.Serialize(response, _jsonOptions);
        return context.Response.WriteAsync(json);
    }
}
