namespace CarWash.Application.DTOs.Clientes;

public class CreateVeiculoRequest
{
    public string? Placa { get; set; }
    public string? Modelo { get; set; }
    public string? Fabricante { get; set; }
    public string? Cor { get; set; }
    public int? Ano { get; set; }
}
