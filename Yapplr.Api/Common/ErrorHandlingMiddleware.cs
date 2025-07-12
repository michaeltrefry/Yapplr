using System.Net;
using System.Text.Json;
using Yapplr.Api.Exceptions;

namespace Yapplr.Api.Common;

/// <summary>
/// Global error handling middleware for consistent API responses
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ErrorResponse();

        switch (exception)
        {
            case ArgumentException ex:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = ex.Message;
                errorResponse.Type = "validation_error";
                break;

            case UnauthorizedAccessException ex:
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                errorResponse.Message = ex.Message;
                errorResponse.Type = "access_denied";
                break;

            case EmailNotVerifiedException ex:
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                errorResponse.Message = ex.Message;
                errorResponse.Type = "email_verification_required";
                break;

            case KeyNotFoundException ex:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Message = "Resource not found";
                errorResponse.Type = "not_found";
                break;

            case InvalidOperationException ex when ex.Message.Contains("already exists"):
                response.StatusCode = (int)HttpStatusCode.Conflict;
                errorResponse.Message = ex.Message;
                errorResponse.Type = "conflict";
                break;

            case TimeoutException ex:
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                errorResponse.Message = "Request timeout";
                errorResponse.Type = "timeout";
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Message = "An internal server error occurred";
                errorResponse.Type = "internal_error";
                break;
        }

        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(jsonResponse);
    }
}

/// <summary>
/// Standard error response format
/// </summary>
public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Details { get; set; }
    public List<string>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Extension method to register the error handling middleware
/// </summary>
public static class ErrorHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorHandlingMiddleware>();
    }
}

/// <summary>
/// Validation error response for model validation failures
/// </summary>
public class ValidationErrorResponse : ErrorResponse
{
    public Dictionary<string, List<string>> ValidationErrors { get; set; } = new();

    public ValidationErrorResponse()
    {
        Type = "validation_error";
        Message = "One or more validation errors occurred";
    }
}

/// <summary>
/// Rate limiting error response
/// </summary>
public class RateLimitErrorResponse : ErrorResponse
{
    public int RetryAfterSeconds { get; set; }
    public string LimitType { get; set; } = string.Empty;

    public RateLimitErrorResponse(int retryAfterSeconds, string limitType)
    {
        Type = "rate_limit_exceeded";
        Message = $"Rate limit exceeded for {limitType}";
        RetryAfterSeconds = retryAfterSeconds;
        LimitType = limitType;
    }
}

/// <summary>
/// Helper methods for creating consistent error responses
/// </summary>
public static class ErrorResponseHelper
{
    public static ErrorResponse CreateValidationError(string message, List<string>? errors = null)
    {
        return new ErrorResponse
        {
            Message = message,
            Type = "validation_error",
            Errors = errors
        };
    }

    public static ErrorResponse CreateNotFoundError(string resource = "Resource")
    {
        return new ErrorResponse
        {
            Message = $"{resource} not found",
            Type = "not_found"
        };
    }

    public static ErrorResponse CreateUnauthorizedError(string message = "Unauthorized access")
    {
        return new ErrorResponse
        {
            Message = message,
            Type = "unauthorized"
        };
    }

    public static ErrorResponse CreateConflictError(string message)
    {
        return new ErrorResponse
        {
            Message = message,
            Type = "conflict"
        };
    }

    public static ValidationErrorResponse CreateModelValidationError(Dictionary<string, List<string>> validationErrors)
    {
        return new ValidationErrorResponse
        {
            ValidationErrors = validationErrors
        };
    }

    public static RateLimitErrorResponse CreateRateLimitError(int retryAfterSeconds, string limitType)
    {
        return new RateLimitErrorResponse(retryAfterSeconds, limitType);
    }
}
