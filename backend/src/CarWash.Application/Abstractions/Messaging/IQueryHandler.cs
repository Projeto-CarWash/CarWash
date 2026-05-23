namespace CarWash.Application.Abstractions.Messaging;

/// <summary>
/// Handler de query (leitura) — sem efeitos colaterais, retorna <typeparamref name="TResponse"/>.
/// </summary>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken);
}

/// <summary>
/// Marker para queries (intent de leitura). Tipado para amarrar a resposta no handler.
/// </summary>
#pragma warning disable CA1040, S2326 // Marker interface intencional (tipa o response do handler).
public interface IQuery<TResponse>
{
}
#pragma warning restore CA1040, S2326
