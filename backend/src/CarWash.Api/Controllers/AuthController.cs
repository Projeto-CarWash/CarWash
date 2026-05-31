using System.Diagnostics;
using CarWash.Application.DTOs;
using CarWash.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CarWash.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        response.TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] TokenRequest request)
    {
        var response = await _authService.RefreshAsync(request);
        response.TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        return Ok(response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] TokenRequest request)
    {
        await _authService.LogoutAsync(request.RefreshToken);

        var response = new BaseResponse
        {
            Message = "Logout realizado com sucesso.",
            TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        };

        return Ok(response);
    }
}
