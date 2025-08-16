namespace FileCategorization_Shared.Common;

/// <summary>
/// Represents the result of an operation that can either succeed or fail.
/// </summary>
/// <typeparam name="T">The type of data returned on success.</typeparam>
public record Result<T>
{
    /// <summary>
    /// Gets the value returned by the operation.
    /// </summary>
    public T? Value { get; init; }
    
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; init; }
    
    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;
    
    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }
    
    /// <summary>
    /// Gets the exception if the operation failed with an exception.
    /// </summary>
    public Exception? Exception { get; init; }
    
    private Result() { }
    
    /// <summary>
    /// Creates a successful result with data.
    /// </summary>
    /// <param name="value">The data to return.</param>
    /// <returns>A successful result.</returns>
    public static Result<T> Success(T value) => new()
    {
        Value = value,
        IsSuccess = true
    };
    
    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Failure(string error) => new()
    {
        Error = error,
        IsSuccess = false
    };
    
    /// <summary>
    /// Creates a failed result with an error message and exception.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Failure(string error, Exception exception) => new()
    {
        Error = error,
        Exception = exception,
        IsSuccess = false
    };
    
    /// <summary>
    /// Creates a failed result with an exception.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Failure(Exception exception) => new()
    {
        Error = exception.Message,
        Exception = exception,
        IsSuccess = false
    };
    
    /// <summary>
    /// Creates a failed result from an exception (backward compatibility with API version).
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> FromException(Exception exception) => Failure(exception);
    
    /// <summary>
    /// Matches the result and returns a value based on success or failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="onSuccess">Function to execute on success.</param>
    /// <param name="onFailure">Function to execute on failure.</param>
    /// <returns>The result of the matching function.</returns>
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<string, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value!) : onFailure(Error!);
    }
    
    /// <summary>
    /// Asynchronously matches the result and returns a value based on success or failure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="onSuccess">Async function to execute on success.</param>
    /// <param name="onFailure">Async function to execute on failure.</param>
    /// <returns>The result of the matching function.</returns>
    public async Task<TResult> MatchAsync<TResult>(
        Func<T, Task<TResult>> onSuccess,
        Func<string, Task<TResult>> onFailure)
    {
        return IsSuccess ? await onSuccess(Value!) : await onFailure(Error!);
    }
}

/// <summary>
/// Static factory methods for creating Result instances.
/// </summary>
public static class Result
{
    /// <summary>
    /// Creates a successful result with data.
    /// </summary>
    /// <typeparam name="T">The type of data.</typeparam>
    /// <param name="value">The data to return.</param>
    /// <returns>A successful result.</returns>
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    
    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <typeparam name="T">The type of data.</typeparam>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);
    
    /// <summary>
    /// Creates a failed result with an error message and exception.
    /// </summary>
    /// <typeparam name="T">The type of data.</typeparam>
    /// <param name="error">The error message.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Failure<T>(string error, Exception exception) => Result<T>.Failure(error, exception);
    
    /// <summary>
    /// Creates a failed result with an exception.
    /// </summary>
    /// <typeparam name="T">The type of data.</typeparam>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Failure<T>(Exception exception) => Result<T>.Failure(exception);
}