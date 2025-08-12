using FileCategorization_Api.Domain.Entities.Utility;
using FluentValidation;

namespace FileCategorization_Api.Common;

/// <summary>
/// Validator for HashStringRequest DTO.
/// </summary>
public class HashStringRequestValidator : AbstractValidator<HashStringRequest>
{
    /// <summary>
    /// Initializes a new instance of the HashStringRequestValidator class.
    /// </summary>
    public HashStringRequestValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty()
            .WithMessage("Text is required for hashing")
            .MaximumLength(10000)
            .WithMessage("Text cannot exceed 10,000 characters")
            .Must(BeValidText)
            .WithMessage("Text cannot contain control characters");
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