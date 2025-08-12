using System.Net;
using FileCategorization_Api.Domain.Entities;
using Microsoft.AspNetCore.Diagnostics;

namespace FileCategorization_Api.Common;

/// <summary>
/// Represents a global exception handler for handling and logging exceptions in the application.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger used to log exception details.</param>
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles exceptions asynchronously and writes an appropriate error response.
    /// </summary>
    /// <param name="httpContext">The HTTP context for the current request.</param>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation, 
    /// with a boolean result indicating whether the exception was handled.
    /// </returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Log the exception details
        _logger.LogError(exception, "An error occurred while processing your request");

        var errorResponse = new ErrorResponse
        {
            Message = exception.Message,
            Title = exception.GetType().Name
        };

        // Determine the status code based on the type of exception
        switch (exception)
        {
            case BadHttpRequestException:
                errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case FilesDetailDoesNotExistException:
                errorResponse.StatusCode = (int)HttpStatusCode.NotFound;
                break;

            default:
                errorResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                break;
        }

        // Set the response status code
        httpContext.Response.StatusCode = errorResponse.StatusCode;

        // Write the error response as JSON
        await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);

        // Return true to indicate that the exception was handled
        return true;
    }
}