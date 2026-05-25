namespace CarWash.Application.Exceptions;

public class VeiculoPlacaDuplicadaException : Exception
{
    public VeiculoPlacaDuplicadaException() : base("Já existe veículo cadastrado com uma das placas informadas."){
    }
}
