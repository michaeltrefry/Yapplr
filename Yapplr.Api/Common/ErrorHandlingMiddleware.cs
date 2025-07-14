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