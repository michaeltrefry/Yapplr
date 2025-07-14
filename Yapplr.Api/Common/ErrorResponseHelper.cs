namespace Yapplr.Api.Common;

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