namespace FileCategorization_Api.Core.Common;

/// <summary>
/// Represents the result of an operation with success/failure state and optional data/error information.
/// </summary>
/// <typeparam name="T">The type of data returned on success.</typeparam>
public class Result<T>
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the data returned by a successful operation.
    /// </summary>
    public T? Data { get; private set; }

    /// <summary>
    /// Gets the error message from a failed operation.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public List<string> ValidationErrors { get; private set; } = new();

    /// <summary>
    /// Gets the exception that caused the failure, if any.
    /// </summary>
    public Exception? SourceException { get; private set; }

    private Result() { }

    /// <summary>
    /// Creates a successful result with data.
    /// </summary>
    /// <param name="data">The data to return.</param>
    /// <returns>A successful result containing the data.</returns>
    public static Result<T> Success(T data) => new()
    {
        IsSuccess = true,
        Data = data
    };

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed result with the error message.</returns>
    public static Result<T> Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };

    /// <summary>
    /// Creates a failed result with validation errors.
    /// </summary>
    /// <param name="validationErrors">The validation errors.</param>
    /// <returns>A failed result with validation errors.</returns>
    public static Result<T> ValidationFailure(List<string> validationErrors) => new()
    {
        IsSuccess = false,
        ErrorMessage = "Validation failed",
        ValidationErrors = validationErrors
    };

    /// <summary>
    /// Creates a failed result with an exception.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A failed result with the exception information.</returns>
    public static Result<T> Exception(Exception exception) => new()
    {
        IsSuccess = false,
        ErrorMessage = exception.Message,
        SourceException = exception
    };

    /// <summary>
    /// Maps the result to a different type if successful.
    /// </summary>
    /// <typeparam name="TResult">The target type.</typeparam>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A new result with the mapped data or the same failure.</returns>
    public Result<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        if (IsFailure)
        {
            return Result<TResult>.Failure(ErrorMessage!);
        }

        try
        {
            var mappedData = mapper(Data!);
            return Result<TResult>.Success(mappedData);
        }
        catch (Exception ex)
        {
            return Result<TResult>.Exception(ex);
        }
    }
}

/// <summary>
/// Represents the result of an operation without return data.
/// </summary>
public class Result
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error message from a failed operation.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public List<string> ValidationErrors { get; private set; } = new();

    /// <summary>
    /// Gets the exception that caused the failure, if any.
    /// </summary>
    public Exception? SourceException { get; private set; }

    protected Result() { }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result Success() => new() { IsSuccess = true };

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed result with the error message.</returns>
    public static Result Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };

    /// <summary>
    /// Creates a failed result with validation errors.
    /// </summary>
    /// <param name="validationErrors">The validation errors.</param>
    /// <returns>A failed result with validation errors.</returns>
    public static Result ValidationFailure(List<string> validationErrors) => new()
    {
        IsSuccess = false,
        ErrorMessage = "Validation failed",
        ValidationErrors = validationErrors
    };

    /// <summary>
    /// Creates a failed result with an exception.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A failed result with the exception information.</returns>
    public static Result Exception(Exception exception) => new()
    {
        IsSuccess = false,
        ErrorMessage = exception.Message,
        SourceException = exception
    };
}