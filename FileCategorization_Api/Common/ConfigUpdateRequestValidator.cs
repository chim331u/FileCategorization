using FileCategorization_Shared.DTOs.Configuration;
using FluentValidation;

namespace FileCategorization_Api.Common;

/// <summary>
/// Validator for ConfigUpdateRequest DTO.
/// </summary>
public class ConfigUpdateRequestValidator : AbstractValidator<ConfigUpdateRequest>
{
    /// <summary>
    /// Initializes a new instance of the ConfigUpdateRequestValidator class.
    /// </summary>
    public ConfigUpdateRequestValidator()
    {
        // At least one field must be provided for update
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Key) || 
                      !string.IsNullOrWhiteSpace(x.Value) || 
                      x.IsDev.HasValue)
            .WithMessage("At least one field (Key, Value, or IsDev) must be provided for update");

        RuleFor(x => x.Key)
            .MaximumLength(100)
            .WithMessage("Configuration key cannot exceed 100 characters")
            .Matches("^[a-zA-Z0-9._-]+$")
            .WithMessage("Configuration key can only contain alphanumeric characters, dots, underscores, and hyphens")
            .Must(BeValidKey)
            .WithMessage("Configuration key cannot contain reserved keywords")
            .When(x => !string.IsNullOrWhiteSpace(x.Key));

        RuleFor(x => x.Value)
            .MaximumLength(5000)
            .WithMessage("Configuration value cannot exceed 5000 characters")
            .Must(BeValidValue)
            .WithMessage("Configuration value contains invalid characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Value));
    }

    /// <summary>
    /// Validates if the key is not a reserved keyword.
    /// </summary>
    /// <param name="key">The key to validate.</param>
    /// <returns>True if the key is valid, false otherwise.</returns>
    private static bool BeValidKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return true; // Allow null/empty for updates

        // List of reserved keywords that shouldn't be used as config keys
        var reservedKeywords = new[]
        {
            "system", "admin", "root", "config", "settings", "default",
            "null", "undefined", "true", "false", "password", "secret"
        };

        return !reservedKeywords.Contains(key.ToLowerInvariant());
    }

    /// <summary>
    /// Validates if the value contains only valid characters.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>True if the value is valid, false otherwise.</returns>
    private static bool BeValidValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true; // Allow null/empty for updates

        // Check for dangerous control characters (excluding newline, carriage return, and tab)
        return !value.Any(c => char.IsControl(c) && c != '\n' && c != '\r' && c != '\t');
    }
}