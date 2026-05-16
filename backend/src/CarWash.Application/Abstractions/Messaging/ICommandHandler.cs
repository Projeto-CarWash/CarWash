namespace CarWash.Application.Abstractions.Messaging;

/// <summary>
/// Handler de comando (escrita) — produz <typeparamref name="TResponse"/>.
/// Substitui MediatR mantendo CQRS sem dependência externa.
/// </summary>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

/// <summary>
/// Marker para comandos (intent de escrita). Tipado para amarrar a resposta no handler.
/// </summary>
#pragma warning disable CA1040, S2326 // Marker interface intencional (tipa o response do handler).
public interface ICommand<TResponse>
{
}
#pragma warning restore CA1040, S2326
