using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Common;

public class ContentLengthAttribute : ValidationAttribute
{
    private readonly int _maxLength;
    private readonly bool _allowEmpty;

    public ContentLengthAttribute(int maxLength, bool allowEmpty = false)
    {
        _maxLength = maxLength;
        _allowEmpty = allowEmpty;
    }

    public override bool IsValid(object? value)
    {
        if (value is string content)
            return ValidationUtilities.ValidateContent(content, _maxLength, _allowEmpty).IsValid;
        return _allowEmpty;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be {_maxLength} characters or less.";
    }
}