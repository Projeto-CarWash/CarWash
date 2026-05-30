namespace CarWash.Application.DTOs.Common;

public class ApiErrorResponse
{
    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string TraceId { get; set; } = string.Empty;

    public List<ApiErrorDetail> Details { get; set; } = [];
}
