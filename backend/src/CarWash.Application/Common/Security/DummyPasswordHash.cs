namespace CarWash.Application.Common.Security;

/// <summary>
/// Hash dummy pré-computado em startup. Usado pelo login handler quando o usuário
/// informado não existe — garante latência similar ao caso "usuário existe + senha
/// errada", mitigando ataques de enumeração por tempo de resposta.
/// </summary>
public sealed record DummyPasswordHash(string Value);
