namespace Yapplr.Api.Common;

/// <summary>
/// Validation result container
/// </summary>
public class ValidationResult
{
    public bool IsValid => !Errors.Any();
    public List<string> Errors { get; }

    public ValidationResult(List<string> errors)
    {
        Errors = errors ?? new List<string>();
    }

    public ValidationResult()
    {
        Errors = new List<string>();
    }

    public static ValidationResult Success() => new();
    public static ValidationResult Failure(string error) => new(new List<string> { error });
    public static ValidationResult Failure(List<string> errors) => new(errors);
}