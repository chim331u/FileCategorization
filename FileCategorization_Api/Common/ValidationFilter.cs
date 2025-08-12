using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FileCategorization_Api.Common;

/// <summary>
/// Generic validation filter for endpoint requests.
/// </summary>
/// <typeparam name="T">The type of request to validate.</typeparam>
public class ValidationFilter<T> : IEndpointFilter where T : class
{
    /// <summary>
    /// Invokes the validation filter.
    /// </summary>
    /// <param name="context">The endpoint filter invocation context.</param>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <returns>The result of the validation and next delegate.</returns>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator == null)
        {
            return await next(context);
        }

        var request = context.Arguments.OfType<T>().FirstOrDefault();
        if (request == null)
        {
            return await next(context);
        }

        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return Results.BadRequest(validationResult.Errors.Select(e => new { Field = e.PropertyName, Error = e.ErrorMessage }));
        }

        return await next(context);
    }
}