using System.Text.RegularExpressions;

namespace CarWash.Application.Common;

public static class InputNormalizer
{
    public static string? TrimOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Regex.Replace(value.Trim(), @"\s+", " ");
    }

    public static string? OnlyDigitsOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string digits = Regex.Replace(value, @"\D", string.Empty);

        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }

    public static string? EmailOrNull(string? value)
    {
        string? normalized = TrimOrNull(value);

        return normalized?.ToLowerInvariant();
    }

    public static bool ContainsOnlyDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Regex.IsMatch(value.Trim(), @"^\d+$");
    }
}
