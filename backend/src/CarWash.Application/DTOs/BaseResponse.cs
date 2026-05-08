namespace CarWash.Application.DTOs;

/// <summary>
/// Classe base para as respostas da API.
/// </summary>
public class BaseResponse
{
    /// <summary>
    /// Gets or sets a mensagem de retorno.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets o código de erro ou sucesso.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Gets or sets o identificador de rastreio da requisição.
    /// </summary>
    public string? TraceId { get; set; }
}
