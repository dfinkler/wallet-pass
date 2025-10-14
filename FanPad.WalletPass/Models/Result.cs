namespace FanPad.WalletPass.Models;

/// <summary>
/// Result type for functional error handling (like TypeScript discriminated unions)
/// Avoids throwing exceptions for business logic errors
/// </summary>
public record Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }

    // Factory methods (like TypeScript helper functions)
    public static Result<T> Success(T value) => new()
    {
        IsSuccess = true,
        Value = value
    };

    public static Result<T> Failure(string error) => new()
    {
        IsSuccess = false,
        Error = error
    };

    // Functional helpers (like Promise.then)
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
    {
        if (!IsSuccess || Value is null)
            return Result<TOut>.Failure(Error ?? "Unknown error");
        
        try
        {
            return Result<TOut>.Success(mapper(Value));
        }
        catch (Exception ex)
        {
            return Result<TOut>.Failure(ex.Message);
        }
    }

    public async Task<Result<TOut>> MapAsync<TOut>(Func<T, Task<TOut>> mapper)
    {
        if (!IsSuccess || Value is null)
            return Result<TOut>.Failure(Error ?? "Unknown error");
        
        try
        {
            var result = await mapper(Value);
            return Result<TOut>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<TOut>.Failure(ex.Message);
        }
    }
}

/// <summary>
/// Extension methods for Result type (fluent API)
/// </summary>
public static class ResultExtensions
{
    // Convert Result to IResult (ASP.NET response)
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error });
    }
}

