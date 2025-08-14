using FileCategorization_Api.Contracts.Actions;
using FluentValidation;

namespace FileCategorization_Api.Common;

/// <summary>
/// Validator for MoveFilesRequest using FluentValidation.
/// </summary>
public class MoveFilesRequestValidator : AbstractValidator<MoveFilesRequest>
{
    public MoveFilesRequestValidator()
    {
        RuleFor(x => x.FilesToMove)
            .NotNull()
            .WithMessage("Files to move cannot be null")
            .NotEmpty()
            .WithMessage("At least one file must be specified")
            .Must(x => x.Count <= 1000)
            .WithMessage("Maximum 1000 files can be moved in a single request");

        RuleForEach(x => x.FilesToMove)
            .ChildRules(file =>
            {
                file.RuleFor(f => f.Id)
                    .GreaterThan(0)
                    .WithMessage("File ID must be greater than 0");

                file.RuleFor(f => f.FileCategory)
                    .NotEmpty()
                    .WithMessage("File category cannot be empty")
                    .MaximumLength(100)
                    .WithMessage("File category cannot exceed 100 characters")
                    .Matches(@"^[a-zA-Z0-9_\-\s]+$")
                    .WithMessage("File category can only contain letters, numbers, spaces, hyphens, and underscores");
            });
    }
}