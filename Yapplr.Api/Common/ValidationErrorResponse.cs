namespace Yapplr.Api.Common;

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