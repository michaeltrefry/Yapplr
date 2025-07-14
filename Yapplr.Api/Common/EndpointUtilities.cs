using System.Security.Claims;
using Yapplr.Api.Extensions;
using Yapplr.Api.Exceptions;

namespace Yapplr.Api.Common;

/// <summary>
/// Common utilities for API endpoints to reduce code duplication
/// </summary>
public static class EndpointUtilities
{
    /// <summary>
    /// Standard error response for bad requests
    /// </summary>
    public static IResult BadRequest(string message) => 
        Results.BadRequest(new { message });

    /// <summary>
    /// Standard error response for not found
    /// </summary>
    public static IResult NotFound(string message = "Resource not found") => 
        Results.NotFound(new { message });

    /// <summary>
    /// Standard error response for unauthorized
    /// </summary>
    public static IResult Unauthorized(string message = "Unauthorized") => 
        Results.Unauthorized();

    /// <summary>
    /// Standard error response for forbidden
    /// </summary>
    public static IResult Forbidden(string message = "Forbidden") => 
        Results.Problem(
            detail: message,
            statusCode: 403,
            title: "Forbidden"
        );

    /// <summary>
    /// Standard success response with data
    /// </summary>
    public static IResult Ok<T>(T data) => Results.Ok(data);

    /// <summary>
    /// Standard created response with location and data
    /// </summary>
    public static IResult Created<T>(string location, T data) => 
        Results.Created(location, data);

    /// <summary>
    /// Handle common async operation patterns
    /// </summary>
    public static async Task<IResult> HandleAsync<T>(
        Func<Task<T?>> operation,
        string? createdLocation = null) where T : class
    {
        try
        {
            var result = await operation();
            
            if (result == null)
                return NotFound();

            if (!string.IsNullOrEmpty(createdLocation))
                return Created(createdLocation, result);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbidden(ex.Message);
        }
        catch (EmailNotVerifiedException ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: 403,
                title: "Email Verification Required",
                type: "email_verification_required"
            );
        }
        catch (InvalidCredentialsException ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: 401,
                title: "Unauthorized"
            );
        }
        catch (Exception ex)
        {
            // Log the exception for debugging
            Console.WriteLine($"EndpointUtilities.HandleAsync caught exception: {ex.GetType().Name} - {ex.Message}");
            return Results.Problem(
                detail: "An error occurred while processing the request",
                statusCode: 500,
                title: "Internal Server Error"
            );
        }
    }

    /// <summary>
    /// Handle operations that return boolean success
    /// </summary>
    public static async Task<IResult> HandleBooleanAsync(
        Func<Task<bool>> operation,
        string successMessage = "Operation completed successfully",
        string failureMessage = "Operation failed")
    {
        try
        {
            var success = await operation();
            
            if (success)
                return Ok(new { message = successMessage });
            
            return BadRequest(failureMessage);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbidden(ex.Message);
        }
        catch (EmailNotVerifiedException ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: 403,
                title: "Email Verification Required",
                type: "email_verification_required"
            );
        }
        catch (InvalidCredentialsException ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: 401,
                title: "Unauthorized"
            );
        }
        catch (Exception)
        {
            return Results.Problem(
                detail: "An error occurred while processing the request",
                statusCode: 500,
                title: "Internal Server Error"
            );
        }
    }

    /// <summary>
    /// Validate that current user can access resource owned by another user
    /// </summary>
    public static bool CanAccessUserResource(ClaimsPrincipal user, int resourceOwnerId)
    {
        var currentUserId = user.GetUserIdOrNull();
        return currentUserId.HasValue && currentUserId.Value == resourceOwnerId;
    }

    /// <summary>
    /// Check if user is self or has admin/moderator role
    /// </summary>
    public static bool CanAccessUserResourceOrIsAdmin(ClaimsPrincipal user, int resourceOwnerId)
    {
        var currentUserId = user.GetUserIdOrNull();
        if (currentUserId.HasValue && currentUserId.Value == resourceOwnerId)
            return true;

        return user.HasRole("Admin") || user.HasRole("Moderator");
    }

    /// <summary>
    /// Extract pagination parameters with validation
    /// </summary>
    public static (int page, int pageSize) GetPaginationParams(int page = 1, int pageSize = 25)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100); // Limit page size to prevent abuse
        return (page, pageSize);
    }

    /// <summary>
    /// Convert ServiceResult to IResult
    /// </summary>
    public static IResult ToApiResult<T>(ServiceResult<T> serviceResult) where T : class
    {
        if (serviceResult.IsSuccess)
        {
            return Ok(serviceResult.Data!);
        }

        if (serviceResult.Exception is ArgumentException)
        {
            return BadRequest(serviceResult.ErrorMessage ?? "Invalid request");
        }

        if (serviceResult.Exception is UnauthorizedAccessException)
        {
            return Forbidden(serviceResult.ErrorMessage ?? "Access denied");
        }

        return Results.Problem(
            detail: serviceResult.ErrorMessage ?? "An error occurred",
            statusCode: 500,
            title: "Internal Server Error"
        );
    }

    /// <summary>
    /// Convert ServiceResult to IResult for non-generic results
    /// </summary>
    public static IResult ToApiResult(ServiceResult serviceResult)
    {
        if (serviceResult.IsSuccess)
        {
            return Ok(new { message = "Operation completed successfully" });
        }

        if (serviceResult.Exception is ArgumentException)
        {
            return BadRequest(serviceResult.ErrorMessage ?? "Invalid request");
        }

        if (serviceResult.Exception is UnauthorizedAccessException)
        {
            return Forbidden(serviceResult.ErrorMessage ?? "Access denied");
        }

        return Results.Problem(
            detail: serviceResult.ErrorMessage ?? "An error occurred",
            statusCode: 500,
            title: "Internal Server Error"
        );
    }

    /// <summary>
    /// Handle paginated service results
    /// </summary>
    public static IResult ToPaginatedApiResult<T>(ServiceResult<PaginatedResult<T>> serviceResult) where T : class
    {
        if (serviceResult.IsSuccess)
        {
            return Ok(serviceResult.Data!);
        }

        return ToApiResult(ServiceResult.Failure(serviceResult.ErrorMessage!, serviceResult.Exception));
    }

    /// <summary>
    /// Create a standardized created response with location
    /// </summary>
    public static IResult CreatedWithLocation<T>(string resourceName, int id, T data) where T : class
    {
        return Created($"/api/{resourceName}/{id}", data);
    }

    /// <summary>
    /// Handle file upload validation and processing
    /// </summary>
    public static async Task<IResult> HandleFileUploadAsync<T>(
        IFormFile file,
        string[] allowedExtensions,
        long maxSizeBytes,
        Func<IFormFile, Task<ServiceResult<T>>> processor) where T : class
    {
        var validation = ValidationUtilities.ValidateFileUpload(file, allowedExtensions, maxSizeBytes);
        if (!validation.IsValid)
        {
            return BadRequest(string.Join(", ", validation.Errors));
        }

        try
        {
            var result = await processor(file);
            return ToApiResult(result);
        }
        catch (Exception)
        {
            return Results.Problem(
                detail: "An error occurred while processing the file",
                statusCode: 500,
                title: "File Processing Error"
            );
        }
    }
}
