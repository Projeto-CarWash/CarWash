using CarWash.Api.Middleware;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agendamentos.Observacoes.Atualizar;
using CarWash.Application.Agendamentos.Observacoes.Common;
using CarWash.Application.Agendamentos.Observacoes.Criar;
using CarWash.Application.Agendamentos.Observacoes.Excluir;
using CarWash.Application.Agendamentos.Observacoes.Listar;
using FluentValidation;

namespace CarWash.Api.Endpoints.Agendamentos;

public static class AgendamentoObservacoesEndpoints
{
    public static IEndpointRouteBuilder MapAgendamentoObservacoes(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/agendamentos/{agendamentoId:guid}/observacoes")
            .WithTags("Agendamentos - Observações")
            .RequireAuthorization();

        group.MapPost("/", CriarObservacao)
            .WithName("CriarObservacaoAgendamento")
            .Produces<CriarObservacaoAgendamentoResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapPatch("/{observacaoId:guid}", AtualizarObservacao)
            .WithName("AtualizarObservacaoAgendamento")
            .Produces<AtualizarObservacaoAgendamentoResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapDelete("/{observacaoId:guid}", ExcluirObservacao)
            .WithName("ExcluirObservacaoAgendamento")
            .Produces<ExcluirObservacaoAgendamentoResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapGet("/", ListarObservacoes)
            .WithName("ListarObservacoesAgendamento")
            .Produces<ListarObservacoesAgendamentoResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> CriarObservacao(
        Guid agendamentoId,
        CriarObservacaoAgendamentoRequest request,
        ICommandHandler<CriarObservacaoAgendamentoCommand, CriarObservacaoAgendamentoResponse> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        string traceId = ObterTraceId(httpContext);
        var usuarioId = ObterUsuarioId(httpContext);

        var command = new CriarObservacaoAgendamentoCommand(
            agendamentoId,
            request.Texto,
            usuarioId,
            traceId);

        try
        {
            var response =
                await handler.HandleAsync(command, cancellationToken);

            return Results.Created(
                $"/api/v1/agendamentos/{agendamentoId}/observacoes/{response.Data.Id}",
                response);
        }
        catch (ValidationException ex)
        {
            return Results.BadRequest(new
            {
                code = "OBSERVACAO_AGENDAMENTO_VALIDATION_ERROR",
                message = "Dados da observação inválidos. Verifique os campos e tente novamente.",
                traceId,
                details = ex.Errors.Select(e => new
                {
                    field = e.PropertyName,
                    message = e.ErrorMessage,
                }),
            });
        }
        catch (AgendamentoNaoEncontradoException)
        {
            return Results.NotFound(new
            {
                code = "AGENDAMENTO_NOT_FOUND",
                message = "Agendamento não encontrado.",
                traceId,
                details = Array.Empty<object>(),
            });
        }
    }

    private static async Task<IResult> AtualizarObservacao(
        Guid agendamentoId,
        Guid observacaoId,
        AtualizarObservacaoAgendamentoRequest request,
        ICommandHandler<AtualizarObservacaoAgendamentoCommand, AtualizarObservacaoAgendamentoResponse> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        string traceId = ObterTraceId(httpContext);
        var usuarioId = ObterUsuarioId(httpContext);

        var command = new AtualizarObservacaoAgendamentoCommand(
            agendamentoId,
            observacaoId,
            request.Texto,
            usuarioId,
            traceId);

        try
        {
            var response =
                await handler.HandleAsync(command, cancellationToken);

            return Results.Ok(response);
        }
        catch (ValidationException ex)
        {
            return Results.BadRequest(new
            {
                code = "OBSERVACAO_AGENDAMENTO_VALIDATION_ERROR",
                message = "Dados da observação inválidos. Verifique os campos e tente novamente.",
                traceId,
                details = ex.Errors.Select(e => new
                {
                    field = e.PropertyName,
                    message = e.ErrorMessage,
                }),
            });
        }
        catch (AgendamentoNaoEncontradoException)
        {
            return Results.NotFound(new
            {
                code = "AGENDAMENTO_NOT_FOUND",
                message = "Agendamento não encontrado.",
                traceId,
                details = Array.Empty<object>(),
            });
        }
        catch (ObservacaoAgendamentoNaoEncontradaException)
        {
            return Results.NotFound(new
            {
                code = "OBSERVACAO_AGENDAMENTO_NOT_FOUND",
                message = "Observação logística não encontrada para este agendamento.",
                traceId,
                details = Array.Empty<object>(),
            });
        }
        catch (ObservacaoAgendamentoEstadoInvalidoException)
        {
            return Results.Conflict(new
            {
                code = "OBSERVACAO_AGENDAMENTO_ESTADO_INVALIDO",
                message = "A observação logística não pode ser alterada no estado atual.",
                traceId,
                details = Array.Empty<object>(),
            });
        }
    }

    private static async Task<IResult> ExcluirObservacao(
        Guid agendamentoId,
        Guid observacaoId,
        ICommandHandler<ExcluirObservacaoAgendamentoCommand, ExcluirObservacaoAgendamentoResponse> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        string traceId = ObterTraceId(httpContext);
        var usuarioId = ObterUsuarioId(httpContext);

        var command = new ExcluirObservacaoAgendamentoCommand(
            agendamentoId,
            observacaoId,
            usuarioId,
            traceId);

        try
        {
            var response =
                await handler.HandleAsync(command, cancellationToken);

            return Results.Ok(response);
        }
        catch (AgendamentoNaoEncontradoException)
        {
            return Results.NotFound(new
            {
                code = "AGENDAMENTO_NOT_FOUND",
                message = "Agendamento não encontrado.",
                traceId,
                details = Array.Empty<object>(),
            });
        }
        catch (ObservacaoAgendamentoNaoEncontradaException)
        {
            return Results.NotFound(new
            {
                code = "OBSERVACAO_AGENDAMENTO_NOT_FOUND",
                message = "Observação logística não encontrada para este agendamento.",
                traceId,
                details = Array.Empty<object>(),
            });
        }
        catch (ObservacaoAgendamentoEstadoInvalidoException)
        {
            return Results.Conflict(new
            {
                code = "OBSERVACAO_AGENDAMENTO_ESTADO_INVALIDO",
                message = "A observação logística não pode ser alterada no estado atual.",
                traceId,
                details = Array.Empty<object>(),
            });
        }
    }

    private static async Task<IResult> ListarObservacoes(
        Guid agendamentoId,
        bool? incluirInativas,
        IQueryHandler<ListarObservacoesAgendamentoQuery, ListarObservacoesAgendamentoResponse> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        string traceId = ObterTraceId(httpContext);

        var query = new ListarObservacoesAgendamentoQuery(
            agendamentoId,
            incluirInativas ?? false,
            traceId);

        try
        {
            var response =
                await handler.HandleAsync(query, cancellationToken);

            return Results.Ok(response);
        }
        catch (AgendamentoNaoEncontradoException)
        {
            return Results.NotFound(new
            {
                code = "AGENDAMENTO_NOT_FOUND",
                message = "Agendamento não encontrado.",
                traceId,
                details = Array.Empty<object>(),
            });
        }
    }

    private static string ObterTraceId(HttpContext httpContext)
    {
        return httpContext.Items[CorrelationIdMiddleware.ItemKey] as string
            ?? httpContext.TraceIdentifier;
    }

    private static Guid ObterUsuarioId(HttpContext httpContext)
    {
        string? raw = httpContext.User.FindFirst("sub")?.Value;

        if (Guid.TryParse(raw, out var usuarioId))
        {
            return usuarioId;
        }

        throw new UnauthorizedAccessException("Usuário autenticado não identificado.");
    }
}
