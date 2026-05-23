using System.Net;
using System.Text.Json;
using CarWash.Application.DTOs;
using CarWash.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CarWash.Api.Middlewares;

public partial class ExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApiException ex)
        {
            await HandleApiExceptionAsync(context, ex);
        }
        catch (AuthException ex)
        {
            await HandleAuthExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            LogInternalError(_logger, ex);
            await HandleInternalExceptionAsync(context);
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

        string json = JsonSerializer.Serialize(response, JsonOptions);
        return context.Response.WriteAsync(json);
    }

    private static Task HandleApiExceptionAsync(HttpContext context, ApiException exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception.StatusCode;

        var response = new BaseResponse
        {
            Message = exception.Message,
            Code = exception.ErrorCode,
            TraceId = context.TraceIdentifier,
            Errors = exception.Errors
        };

        string json = JsonSerializer.Serialize(response, JsonOptions);
        return context.Response.WriteAsync(json);
    }

    private static Task HandleInternalExceptionAsync(HttpContext context)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new BaseResponse
        {
            Message = "Ocorreu um erro interno no servidor. Tente novamente mais tarde.",
            Code = "INTERNAL_SERVER_ERROR",
            TraceId = context.TraceIdentifier
        };

        string json = JsonSerializer.Serialize(response, JsonOptions);
        return context.Response.WriteAsync(json);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Erro interno não tratado.")]
    private static partial void LogInternalError(ILogger logger, Exception exception);
}
