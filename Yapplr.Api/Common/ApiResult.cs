namespace Yapplr.Api.Common;

/// <summary>
/// Standard API result wrapper for consistent responses
/// </summary>
public class ApiResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResult<T> SuccessResult(T data, string? message = null)
    {
        return new ApiResult<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static ApiResult<T> FailureResult(string message, List<string>? errors = null)
    {
        return new ApiResult<T>
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }

    public static ApiResult<T> FailureResult(List<string> errors)
    {
        return new ApiResult<T>
        {
            Success = false,
            Errors = errors,
            Message = "Validation failed"
        };
    }
}

/// <summary>
/// Non-generic API result for operations that don't return data
/// </summary>
public class ApiResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResult SuccessResult(string? message = null)
    {
        return new ApiResult
        {
            Success = true,
            Message = message
        };
    }

    public static ApiResult FailureResult(string message, List<string>? errors = null)
    {
        return new ApiResult
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }

    public static ApiResult FailureResult(List<string> errors)
    {
        return new ApiResult
        {
            Success = false,
            Errors = errors,
            Message = "Validation failed"
        };
    }
}

/// <summary>
/// Paginated result wrapper
/// </summary>
public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;

    public static PaginatedResult<T> Create(List<T> items, int page, int pageSize, int totalCount)
    {
        return new PaginatedResult<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}

/// <summary>
/// Service operation result for internal service communication
/// </summary>
public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }

    public static ServiceResult<T> Success(T data)
    {
        return new ServiceResult<T>
        {
            IsSuccess = true,
            Data = data
        };
    }

    public static ServiceResult<T> Failure(string errorMessage, Exception? exception = null)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Exception = exception
        };
    }
}

/// <summary>
/// Non-generic service result
/// </summary>
public class ServiceResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }

    public static ServiceResult Success()
    {
        return new ServiceResult { IsSuccess = true };
    }

    public static ServiceResult Failure(string errorMessage, Exception? exception = null)
    {
        return new ServiceResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Exception = exception
        };
    }
}
