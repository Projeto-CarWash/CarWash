namespace CarWash.Application.DTOs.Common;

public class ApiDataResponse<T>
{
    public T? Data { get; set; }
    public string TraceId { get; set; } = string.Empty;
}
