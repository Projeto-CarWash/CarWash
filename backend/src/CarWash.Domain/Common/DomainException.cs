namespace CarWash.Domain.Common;

/// <summary>
/// Exceção lançada quando uma invariante de domínio é violada.
/// Mensagens em PT-BR para refletir a linguagem ubíqua do CarWash.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public DomainException()
    {
    }
}
