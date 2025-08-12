using FileCategorization_Api.Domain.Entities.Utility;
using FluentValidation;

namespace FileCategorization_Api.Common;

/// <summary>
/// Validator for DecryptStringRequest DTO.
/// </summary>
public class DecryptStringRequestValidator : AbstractValidator<DecryptStringRequest>
{
    /// <summary>
    /// Initializes a new instance of the DecryptStringRequestValidator class.
    /// </summary>
    public DecryptStringRequestValidator()
    {
        RuleFor(x => x.EncryptedText)
            .NotEmpty()
            .WithMessage("Encrypted text is required for decryption")
            .MaximumLength(20000)
            .WithMessage("Encrypted text cannot exceed 20,000 characters")
            .Must(BeValidBase64OrHex)
            .WithMessage("Encrypted text must be valid Base64 or hexadecimal format");
    }

    /// <summary>
    /// Validates if the text is valid Base64 or hexadecimal format.
    /// </summary>
    /// <param name="text">The text to validate.</param>
    /// <returns>True if the text is valid format, false otherwise.</returns>
    private static bool BeValidBase64OrHex(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        // Try Base64 validation
        try
        {
            Convert.FromBase64String(text);
            return true;
        }
        catch
        {
            // If not Base64, try hex validation
            return IsValidHex(text);
        }
    }

    /// <summary>
    /// Validates if the text is valid hexadecimal.
    /// </summary>
    /// <param name="text">The text to validate.</param>
    /// <returns>True if the text is valid hex, false otherwise.</returns>
    private static bool IsValidHex(string text)
    {
        return text.All(c => char.IsDigit(c) || 
                           (c >= 'A' && c <= 'F') || 
                           (c >= 'a' && c <= 'f'));
    }
}