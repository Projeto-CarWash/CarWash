namespace CarWash.Application.Usuarios.Preferencias.Common;

public sealed class UsuarioPreferenciasResponse
{
    public string Message { get; set; } = string.Empty;

    public UsuarioPreferenciasDataResponse Data { get; set; } = new();

    public string TraceId { get; set; } = string.Empty;
}

public sealed class UsuarioPreferenciasDataResponse
{
    public string Theme { get; set; } = "light";
}
