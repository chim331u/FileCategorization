namespace FileCategorization_Web.Data.Common;

public record Result<T>
{
    public T? Value { get; init; }
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }
    public Exception? Exception { get; init; }
    
    private Result() { }
    
    public static Result<T> Success(T value) => new()
    {
        Value = value,
        IsSuccess = true
    };
    
    public static Result<T> Failure(string error) => new()
    {
        Error = error,
        IsSuccess = false
    };
    
    public static Result<T> Failure(string error, Exception exception) => new()
    {
        Error = error,
        Exception = exception,
        IsSuccess = false
    };
    
    public static Result<T> Failure(Exception exception) => new()
    {
        Error = exception.Message,
        Exception = exception,
        IsSuccess = false
    };
    
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<string, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value!) : onFailure(Error!);
    }
    
    public async Task<TResult> MatchAsync<TResult>(
        Func<T, Task<TResult>> onSuccess,
        Func<string, Task<TResult>> onFailure)
    {
        return IsSuccess ? await onSuccess(Value!) : await onFailure(Error!);
    }
}

public static class Result
{
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);
    public static Result<T> Failure<T>(string error, Exception exception) => Result<T>.Failure(error, exception);
    public static Result<T> Failure<T>(Exception exception) => Result<T>.Failure(exception);
}