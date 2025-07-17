using System.Net;
using System.Text.Json;
using Yapplr.Api.Exceptions;
using Serilog.Context;
using System.Security.Claims;

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
            await LogExceptionWithContextAsync(context, ex);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task LogExceptionWithContextAsync(HttpContext context, Exception ex)
    {
        using var exceptionScope = LogContext.PushProperty("ExceptionType", ex.GetType().Name);
        using var messageScope = LogContext.PushProperty("ExceptionMessage", ex.Message);
        using var stackTraceScope = LogContext.PushProperty("StackTrace", ex.StackTrace);
        using var requestPathScope = LogContext.PushProperty("RequestPath", context.Request.Path.Value);
        using var httpMethodScope = LogContext.PushProperty("HttpMethod", context.Request.Method);
        using var queryStringScope = LogContext.PushProperty("QueryString", context.Request.QueryString.Value);

        // Add user context if available
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = context.User.FindFirst(ClaimTypes.Name)?.Value;

            using var userIdScope = !string.IsNullOrEmpty(userId) ? LogContext.PushProperty("UserId", userId) : null;
            using var usernameScope = !string.IsNullOrEmpty(username) ? LogContext.PushProperty("Username", username) : null;

            _logger.LogError(ex, "Unhandled exception in {HttpMethod} {RequestPath} for user {UserId} ({Username}): {ExceptionType} - {ExceptionMessage}",
                context.Request.Method, context.Request.Path, userId, username, ex.GetType().Name, ex.Message);
        }
        else
        {
            _logger.LogError(ex, "Unhandled exception in {HttpMethod} {RequestPath}: {ExceptionType} - {ExceptionMessage}",
                context.Request.Method, context.Request.Path, ex.GetType().Name, ex.Message);
        }

        // Log additional context for specific exception types
        switch (ex)
        {
            case ArgumentException:
                _logger.LogWarning("Validation error in {RequestPath}: {ExceptionMessage}", context.Request.Path, ex.Message);
                break;
            case UnauthorizedAccessException:
                _logger.LogSecurityEvent("UnauthorizedAccess",
                    userId: context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value != null ?
                           int.Parse(context.User.FindFirst(ClaimTypes.NameIdentifier)!.Value) : null,
                    details: new { RequestPath = context.Request.Path.Value, Method = context.Request.Method });
                break;
            case InvalidOperationException when ex.Message.Contains("trust"):
                _logger.LogSecurityEvent("TrustScoreViolation",
                    userId: context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value != null ?
                           int.Parse(context.User.FindFirst(ClaimTypes.NameIdentifier)!.Value) : null,
                    details: new { RequestPath = context.Request.Path.Value, Reason = ex.Message });
                break;
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

            case InvalidCredentialsException ex:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.Message = ex.Message;
                errorResponse.Type = "unauthorized";
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
                errorResponse.Detail = ex.Message;
                errorResponse.Title = "Email Verification Required";
                errorResponse.Status = (int)HttpStatusCode.Forbidden;
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