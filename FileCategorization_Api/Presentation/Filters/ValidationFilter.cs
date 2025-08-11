using FileCategorization_Api.Core.Common;
using FluentValidation;

namespace FileCategorization_Api.Presentation.Filters;

/// <summary>
/// Endpoint filter for automatic FluentValidation integration.
/// </summary>
public class ValidationFilter<T> : IEndpointFilter
{
    private readonly IValidator<T> _validator;

    /// <summary>
    /// Initializes a new instance of the ValidationFilter class.
    /// </summary>
    /// <param name="validator">The validator for type T.</param>
    public ValidationFilter(IValidator<T> validator)
    {
        _validator = validator;
    }

    /// <summary>
    /// Invokes the validation filter.
    /// </summary>
    /// <param name="context">The endpoint filter invocation context.</param>
    /// <param name="next">The next filter in the pipeline.</param>
    /// <returns>The filter result.</returns>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // Find the argument of type T in the context
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument == null)
        {
            // If no argument of type T is found, continue with the next filter
            return await next(context);
        }

        // Validate the argument
        var validationResult = await _validator.ValidateAsync(argument);
        if (!validationResult.IsValid)
        {
            // Create a validation failure result
            var validationErrors = validationResult.Errors
                .Select(error => $"{error.PropertyName}: {error.ErrorMessage}")
                .ToList();

            var result = Result<object>.ValidationFailure(validationErrors);
            
            return Results.BadRequest(new
            {
                message = result.ErrorMessage,
                errors = result.ValidationErrors
            });
        }

        // If validation passes, continue with the next filter
        return await next(context);
    }
}