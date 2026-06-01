namespace CarWash.Application.Abstractions;

/// <summary>
/// Abstração para o hash de senhas — implementação default é Argon2id (ADR 0002).
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Gera hash PHC Argon2id a partir da senha em claro.
    /// </summary>
    /// <returns></returns>
    string Hash(string senha);

    /// <summary>
    /// Verifica em tempo constante se a senha em claro confere com o hash armazenado.
    /// </summary>
    /// <returns></returns>
    bool Verify(string senha, string hash);

    /// <summary>
    /// Indica se o hash armazenado usa parâmetros mais fracos que os atuais —
    /// útil para reidratação em login bem-sucedido.
    /// </summary>
    /// <returns></returns>
    bool NeedsRehash(string hash);
}
