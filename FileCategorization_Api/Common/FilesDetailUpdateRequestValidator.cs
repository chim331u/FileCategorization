using FileCategorization_Api.Domain.Entities.FilesDetail;
using FluentValidation;

namespace FileCategorization_Api.Common;

/// <summary>
/// Validator for FilesDetailUpdateRequest DTO.
/// </summary>
public class FilesDetailUpdateRequestValidator : AbstractValidator<FilesDetailUpdateRequest>
{
    /// <summary>
    /// Initializes a new instance of the FilesDetailUpdateRequestValidator class.
    /// </summary>
    public FilesDetailUpdateRequestValidator()
    {
        // Name validation (optional for updates)
        RuleFor(x => x.Name)
            .MaximumLength(255)
            .WithMessage("File name cannot exceed 255 characters")
            .Matches(@"^[^<>:""/\\|?*]+$")
            .WithMessage("File name contains invalid characters")
            .When(x => !string.IsNullOrEmpty(x.Name));

        // Path validation (optional for updates)
        RuleFor(x => x.Path)
            .MaximumLength(1000)
            .WithMessage("File path cannot exceed 1000 characters")
            .Must(BeValidPath)
            .WithMessage("Invalid file path format")
            .When(x => !string.IsNullOrEmpty(x.Path));

        // File category validation (optional)
        RuleFor(x => x.FileCategory)
            .MaximumLength(100)
            .WithMessage("File category cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.FileCategory));

        // File size validation (optional for updates)
        RuleFor(x => x.FileSize)
            .GreaterThanOrEqualTo(0)
            .WithMessage("File size cannot be negative")
            .LessThan(long.MaxValue)
            .WithMessage("File size is too large");

        // Custom business rule: If IsToCategorize is false, FileCategory should be provided
        RuleFor(x => x.FileCategory)
            .NotEmpty()
            .WithMessage("File category is required when the file is marked as categorized")
            .When(x => x.IsToCategorize == false);
    }

    /// <summary>
    /// Validates if the provided path is in a valid format.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns>True if the path is valid, false otherwise.</returns>
    private static bool BeValidPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            // Basic path validation - checks for invalid characters
            var invalidChars = Path.GetInvalidPathChars();
            return !path.Any(c => invalidChars.Contains(c));
        }
        catch
        {
            return false;
        }
    }
}