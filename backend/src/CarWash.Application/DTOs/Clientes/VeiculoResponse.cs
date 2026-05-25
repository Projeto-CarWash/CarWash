namespace CarWash.Application.DTOs.Clientes;

public class VeiculoResponse
{
    public Guid Id { get; set; }
    public string Placa { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string Fabricante { get; set; } = string.Empty;
    public string Cor { get; set; } = string.Empty;
}
