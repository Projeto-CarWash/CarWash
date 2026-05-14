namespace CarWash.Application.Exceptions;

public class ClienteDocumentoDuplicadoException : Exception
{
    public ClienteDocumentoDuplicadoException()
        : base("Já existe cliente cadastrado com este documento.")
    {
    }
}
