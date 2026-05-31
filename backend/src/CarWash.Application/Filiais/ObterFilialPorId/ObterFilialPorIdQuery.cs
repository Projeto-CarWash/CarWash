using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Filiais.Common;

namespace CarWash.Application.Filiais.ObterFilialPorId;

/// <summary>
/// Consulta de filial por id (RF017/RF018). Retorna <see cref="FilialResponse"/>.
/// Sem auditoria — operação de leitura.
/// </summary>
public sealed record ObterFilialPorIdQuery(Guid Id) : IQuery<FilialResponse>;
