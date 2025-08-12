using FileCategorization_Api.Domain.Entities.FilesDetail;
using FluentValidation;

namespace FileCategorization_Api.Common;

/// <summary>
/// Validator for FileMoveDto.
/// </summary>
public class FileMoveRequestValidator : AbstractValidator<FileMoveDto>
{
    /// <summary>
    /// Initializes a new instance of the FileMoveRequestValidator class.
    /// </summary>
    public FileMoveRequestValidator()
    {
        // ID validation
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("File ID must be greater than 0");

        // File category validation
        RuleFor(x => x.FileCategory)
            .NotEmpty()
            .WithMessage("File category is required for file move operation")
            .MaximumLength(100)
            .WithMessage("File category cannot exceed 100 characters")
            .Must(BeValidCategory)
            .WithMessage("Invalid file category format");
    }

    /// <summary>
    /// Validates if the provided category is in a valid format.
    /// </summary>
    /// <param name="category">The category to validate.</param>
    /// <returns>True if the category is valid, false otherwise.</returns>
    private static bool BeValidCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return false;

        // Category should not contain special characters that could cause issues
        var invalidChars = new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*', '\n', '\r', '\t' };
        return !category.Any(c => invalidChars.Contains(c));
    }
}