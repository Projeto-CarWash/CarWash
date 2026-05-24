using System.ComponentModel.DataAnnotations;

namespace CarWash.Application.Validation;

/// <summary>
/// Valida que o valor contenha caracteres não brancos.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NotWhiteSpaceAttribute : ValidationAttribute
{
    /// <inheritdoc/>
    public override bool IsValid(object? value)
    {
        return value is string text && !string.IsNullOrWhiteSpace(text);
    }
}
