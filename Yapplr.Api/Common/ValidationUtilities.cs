using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Yapplr.Api.Common;

/// <summary>
/// Common validation utilities and custom validation attributes
/// </summary>
public static class ValidationUtilities
{
    /// <summary>
    /// Validate email format
    /// </summary>
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var emailAttribute = new EmailAddressAttribute();
            return emailAttribute.IsValid(email);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validate username format (alphanumeric, underscore, hyphen)
    /// </summary>
    public static bool IsValidUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return false;

        // Username should be 3-50 characters, alphanumeric plus underscore and hyphen
        var regex = new Regex(@"^[a-zA-Z0-9_-]{3,50}$");
        return regex.IsMatch(username);
    }

    /// <summary>
    /// Validate password strength
    /// </summary>
    public static ValidationResult ValidatePassword(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Password is required");
            return new ValidationResult(errors);
        }

        if (password.Length < 6)
            errors.Add("Password must be at least 6 characters long");

        if (password.Length > 128)
            errors.Add("Password must be less than 128 characters long");

        // Check for at least one letter and one number
        if (!password.Any(char.IsLetter))
            errors.Add("Password must contain at least one letter");

        if (!password.Any(char.IsDigit))
            errors.Add("Password must contain at least one number");

        return new ValidationResult(errors);
    }

    /// <summary>
    /// Validate content length and format
    /// </summary>
    public static ValidationResult ValidateContent(string content, int maxLength = 256, bool allowEmpty = false)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(content))
        {
            if (!allowEmpty)
                errors.Add("Content is required");
            return new ValidationResult(errors);
        }

        if (content.Length > maxLength)
            errors.Add($"Content must be {maxLength} characters or less");

        // Check for potentially harmful content patterns
        if (ContainsSuspiciousPatterns(content))
            errors.Add("Content contains potentially harmful patterns");

        return new ValidationResult(errors);
    }

    /// <summary>
    /// Check for suspicious patterns in content
    /// </summary>
    private static bool ContainsSuspiciousPatterns(string content)
    {
        // Basic checks for script injection, etc.
        var suspiciousPatterns = new[]
        {
            @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>",
            @"javascript:",
            @"vbscript:",
            @"onload\s*=",
            @"onerror\s*=",
            @"onclick\s*="
        };

        return suspiciousPatterns.Any(pattern => 
            Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// Validate file upload
    /// </summary>
    public static ValidationResult ValidateFileUpload(IFormFile file, string[] allowedExtensions, long maxSizeBytes)
    {
        var errors = new List<string>();

        if (file == null)
        {
            errors.Add("File is required");
            return new ValidationResult(errors);
        }

        if (file.Length == 0)
            errors.Add("File is empty");

        if (file.Length > maxSizeBytes)
            errors.Add($"File size must be less than {maxSizeBytes / (1024 * 1024)} MB");

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
            errors.Add($"File type not allowed. Allowed types: {string.Join(", ", allowedExtensions)}");

        return new ValidationResult(errors);
    }

    /// <summary>
    /// Validate pagination parameters
    /// </summary>
    public static ValidationResult ValidatePagination(int page, int pageSize, int maxPageSize = 100)
    {
        var errors = new List<string>();

        if (page < 1)
            errors.Add("Page must be greater than 0");

        if (pageSize < 1)
            errors.Add("Page size must be greater than 0");

        if (pageSize > maxPageSize)
            errors.Add($"Page size must be {maxPageSize} or less");

        return new ValidationResult(errors);
    }
}