using FileCategorization_Api.Domain.Entities.FilesDetail;
using FileCategorization_Shared.DTOs.FileManagement;
using FluentValidation;

namespace FileCategorization_Api.Common;

/// <summary>
/// Validator for FilesDetailRequest DTO.
/// </summary>
public class FilesDetailRequestValidator : AbstractValidator<FilesDetailRequest>
{
    /// <summary>
    /// Initializes a new instance of the FilesDetailRequestValidator class.
    /// </summary>
    public FilesDetailRequestValidator()
    {
        // Name validation
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("File name is required")
            .MaximumLength(255)
            .WithMessage("File name cannot exceed 255 characters")
            .Matches(@"^[^<>:""/\\|?*]+$")
            .WithMessage("File name contains invalid characters");

        // Path validation
        RuleFor(x => x.Path)
            .NotEmpty()
            .WithMessage("File path is required")
            .MaximumLength(1000)
            .WithMessage("File path cannot exceed 1000 characters")
            .Must(BeValidPath)
            .WithMessage("Invalid file path format");

        // File category validation (optional)
        RuleFor(x => x.FileCategory)
            .MaximumLength(100)
            .WithMessage("File category cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.FileCategory));

        // File size validation
        RuleFor(x => x.FileSize)
            .GreaterThanOrEqualTo(0)
            .WithMessage("File size cannot be negative")
            .LessThan(long.MaxValue)
            .WithMessage("File size is too large");

        // Boolean fields don't need explicit validation as they are already constrained by type
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