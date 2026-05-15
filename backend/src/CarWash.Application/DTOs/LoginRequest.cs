using System.ComponentModel.DataAnnotations;
using CarWash.Application.Validation;

namespace CarWash.Application.DTOs;

/// <summary>
/// Modelo de dados para a requisição de login.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Gets or sets o email do utilizador.
    /// </summary>
    [Required(ErrorMessage = "Email é obrigatório.")]
    [NotWhiteSpace(ErrorMessage = "Email é obrigatório.")]
    [EmailAddress(ErrorMessage = "Email inválido.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a senha do utilizador.
    /// </summary>
    [Required(ErrorMessage = "Senha é obrigatória.")]
    [NotWhiteSpace(ErrorMessage = "Senha é obrigatória.")]
    public string Senha { get; set; } = string.Empty;
}
