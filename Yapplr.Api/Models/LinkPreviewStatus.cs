namespace Yapplr.Api.Models;

public enum LinkPreviewStatus
{
    Pending = 0,
    Success = 1,
    NotFound = 2,        // 404
    Unauthorized = 3,    // 401
    Forbidden = 4,       // 403
    Timeout = 5,
    NetworkError = 6,
    InvalidUrl = 7,
    TooLarge = 8,        // Response too large
    UnsupportedContent = 9, // Not HTML content
    Error = 10           // General error
}