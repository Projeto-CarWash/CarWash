namespace Carwash.Application.DTOs.Clientes;

public class CreateVeiculoResponse{
    public Guid Id { get; set; }
    public string? Placa { get; set; }
    public string? Modelo { get; set; }
    public string? Fabricante { get; set; }
    public string? Cor { get; set; }
    public int? Ano { get; set; }
}
