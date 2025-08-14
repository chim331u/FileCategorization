using FileCategorization_Api.Contracts.Actions;
using FluentValidation;

namespace FileCategorization_Api.Common;

/// <summary>
/// Validator for RefreshFilesRequest using FluentValidation.
/// </summary>
public class RefreshFilesRequestValidator : AbstractValidator<RefreshFilesRequest>
{
    public RefreshFilesRequestValidator()
    {
        RuleFor(x => x.BatchSize)
            .InclusiveBetween(10, 1000)
            .WithMessage("Batch size must be between 10 and 1000");

        When(x => x.FileExtensionFilters != null, () =>
        {
            RuleFor(x => x.FileExtensionFilters)
                .Must(x => x!.Count <= 50)
                .WithMessage("Maximum 50 file extension filters allowed");

            RuleForEach(x => x.FileExtensionFilters)
                .NotEmpty()
                .WithMessage("File extension filter cannot be empty")
                .Matches(@"^\.[a-zA-Z0-9]+$")
                .WithMessage("File extension must start with a dot and contain only alphanumeric characters (e.g., .jpg, .pdf)");
        });
    }
}