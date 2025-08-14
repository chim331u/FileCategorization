using FluentValidation;
using FileCategorization_Api.Contracts.DD;

namespace FileCategorization_Api.Common;

/// <summary>
/// Validator for ProcessThreadRequestDto
/// </summary>
public class ProcessThreadRequestValidator : AbstractValidator<ProcessThreadRequestDto>
{
    public ProcessThreadRequestValidator()
    {
        RuleFor(x => x.ThreadUrl)
            .NotEmpty()
            .WithMessage("Thread URL is required")
            .Must(BeValidUrl)
            .WithMessage("Thread URL must be a valid HTTP/HTTPS URL")
            .MaximumLength(2000)
            .WithMessage("Thread URL cannot exceed 2000 characters");
    }

    private bool BeValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}