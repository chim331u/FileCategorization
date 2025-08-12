using FileCategorization_Api.Domain.Entities.Utility;
using FluentValidation;

namespace FileCategorization_Api.Common;

/// <summary>
/// Validator for VerifyHashRequest DTO.
/// </summary>
public class VerifyHashRequestValidator : AbstractValidator<VerifyHashRequest>
{
    /// <summary>
    /// Initializes a new instance of the VerifyHashRequestValidator class.
    /// </summary>
    public VerifyHashRequestValidator()
    {
        RuleFor(x => x.PlainText)
            .NotEmpty()
            .WithMessage("Plain text is required for hash verification")
            .MaximumLength(10000)
            .WithMessage("Plain text cannot exceed 10,000 characters")
            .Must(BeValidText)
            .WithMessage("Plain text cannot contain control characters");

        RuleFor(x => x.Hash)
            .NotEmpty()
            .WithMessage("Hash is required for verification")
            .Length(64)
            .WithMessage("SHA256 hash must be exactly 64 characters")
            .Matches("^[a-fA-F0-9]{64}$")
            .WithMessage("Hash must be a valid SHA256 hash (64 hexadecimal characters)");
    }

    /// <summary>
    /// Validates if the text contains only valid characters.
    /// </summary>
    /// <param name="text">The text to validate.</param>
    /// <returns>True if the text is valid, false otherwise.</returns>
    private static bool BeValidText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        // Check for control characters except newline, carriage return, and tab
        return !text.Any(c => char.IsControl(c) && c != '\n' && c != '\r' && c != '\t');
    }
}