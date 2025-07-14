using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Common;

/// <summary>
/// Custom validation attributes
/// </summary>
public class ValidUsernameAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is string username)
            return ValidationUtilities.IsValidUsername(username);
        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be 3-50 characters long and contain only letters, numbers, underscores, and hyphens.";
    }
}