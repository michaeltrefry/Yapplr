using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Common;

public class StrongPasswordAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is string password)
            return ValidationUtilities.ValidatePassword(password).IsValid;
        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be at least 6 characters long and contain at least one letter and one number.";
    }
}